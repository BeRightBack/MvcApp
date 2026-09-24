/* 1-on-1 video call client for the Messages page.
 * Uses the same /videohub SignalR hub for signaling (offer/answer/ICE) and
 * presence; audio/video flows peer-to-peer via WebRTC. */
window.VideoCall = (function () {
    let cfg = null;
    let connection = null;
    let connectionPromise = null;

    let state = 'idle';        // idle | ringing | incoming | active
    let myRole = null;         // caller | callee
    let localStream = null;
    let screenStream = null;
    let camEnabled = true;
    let micEnabled = true;
    let peerConn = null;
    let peerConnectionId = null;
    let currentRoomId = null;

    let ringTimer = null;
    let ringCtx = null;

    const $ = (id) => document.getElementById(id);

    const rtcConfig = {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' },
            { urls: 'stun:stun2.l.google.com:19302' }
        ]
    };

    function ensureConnection() {
        if (!connectionPromise) {
            connectionPromise = (async () => {
                connection = new signalR.HubConnectionBuilder()
                    .withUrl(cfg.hubUrl || '/videohub')
                    .withAutomaticReconnect()
                    .build();
                bindHubEvents();
                await connection.start();
            })();
        }
        return connectionPromise;
    }

    // ----- Overlay DOM -----

    function buildOverlay() {
        if ($('vcCallOverlay')) return;
        const div = document.createElement('div');
        div.id = 'vcCallOverlay';
        div.className = 'vc-call-overlay';
        div.style.display = 'none';
        div.innerHTML =
            '<div class="vc-remote-frame">' +
            '  <video id="vcRemoteVideo" autoplay playsinline></video>' +
            '  <div class="vc-local-frame"><video id="vcLocalVideo" autoplay playsinline muted></video></div>' +
            '</div>' +
            '<div class="vc-call-status" id="vcStatus">…</div>' +
            '<div class="vc-controls">' +
            '  <button type="button" id="vcBtnMic" class="vc-btn" title="Mute microphone"><i class="fa fa-microphone"></i></button>' +
            '  <button type="button" id="vcBtnCam" class="vc-btn" title="Toggle camera"><i class="fa fa-video"></i></button>' +
            '  <button type="button" id="vcBtnScreen" class="vc-btn" title="Share screen"><i class="fa fa-desktop"></i></button>' +
            '  <button type="button" id="vcBtnCancel" class="vc-btn vc-hangup" title="Cancel" style="display:none"><i class="fa fa-phone"></i></button>' +
            '  <button type="button" id="vcBtnHangup" class="vc-btn vc-hangup" title="End call" style="display:none"><i class="fa fa-phone"></i></button>' +
            '</div>';
        document.body.appendChild(div);

        $('vcBtnMic').addEventListener('click', toggleMic);
        $('vcBtnCam').addEventListener('click', toggleCam);
        $('vcBtnScreen').addEventListener('click', shareScreen);
        $('vcBtnCancel').addEventListener('click', onCancel);
        $('vcBtnHangup').addEventListener('click', onHangup);
    }

    function buildIncomingCard() {
        if ($('vcIncomingCard')) return;
        const div = document.createElement('div');
        div.id = 'vcIncomingCard';
        div.className = 'vc-incoming-card';
        div.style.display = 'none';
        div.innerHTML =
            '<div class="vc-avatar"><i class="fa fa-user"></i></div>' +
            '<h5 id="vcIncomingName">…</h5>' +
            '<p class="text-muted mb-0">is calling you</p>' +
            '<div class="vc-actions">' +
            '  <button type="button" id="vcAccept" class="vc-btn vc-accept" title="Accept"><i class="fa fa-phone"></i></button>' +
            '  <button type="button" id="vcDecline" class="vc-btn vc-decline" title="Decline"><i class="fa fa-phone"></i></button>' +
            '</div>';
        document.body.appendChild(div);

        $('vcAccept').addEventListener('click', onAccept);
        $('vcDecline').addEventListener('click', onDecline);
    }

    function setOverlayPhase(phase, statusText) {
        buildOverlay();
        const overlay = $('vcCallOverlay');
        overlay.style.display = 'flex';
        $('vcStatus').textContent = statusText || '';
        const active = phase === 'active';
        $('vcRemoteVideo').style.display = active ? 'block' : 'none';
        $('vcLocalVideo').style.display = active ? 'block' : 'none';
        $('vcBtnMic').style.display = active ? 'flex' : 'none';
        $('vcBtnCam').style.display = active ? 'flex' : 'none';
        $('vcBtnScreen').style.display = active ? 'flex' : 'none';
        $('vcBtnCancel').style.display = phase === 'ringing' ? 'flex' : 'none';
        $('vcBtnHangup').style.display = active ? 'flex' : 'none';
    }

    function closeOverlay() {
        buildOverlay();
        $('vcCallOverlay').style.display = 'none';
        $('vcStatus').textContent = '';
    }

    // ----- Ringing -----

    function ringStart() {
        ringStop();
        try {
            const AC = window.AudioContext || window.webkitAudioContext;
            ringCtx = new AC();
            const o = ringCtx.createOscillator();
            const g = ringCtx.createGain();
            o.type = 'sine';
            o.frequency.value = 620;
            g.gain.value = 0.12;
            o.connect(g);
            g.connect(ringCtx.destination);
            o.start();
            ringTimer = setInterval(function () {
                g.gain.value = g.gain.value > 0.05 ? 0 : 0.12;
            }, 650);
        } catch (e) { /* audio unavailable */ }
    }

    function ringStop() {
        if (ringTimer) clearInterval(ringTimer);
        ringTimer = null;
        if (ringCtx) {
            try { ringCtx.close(); } catch (e) { /* noop */ }
            ringCtx = null;
        }
    }

    // ----- Media -----

    async function getUserMedia() {
        return navigator.mediaDevices.getUserMedia({ video: true, audio: true });
    }

    function attachLocal() {
        const local = $('vcLocalVideo');
        if (local && localStream) {
            local.srcObject = localStream;
        }
    }

    function closeStreams() {
        if (localStream) {
            localStream.getTracks().forEach(function (t) { t.stop(); });
            localStream = null;
        }
        if (screenStream) {
            screenStream.getTracks().forEach(function (t) { t.stop(); });
            screenStream = null;
        }
    }

    function closePeer() {
        if (peerConn) {
            try { peerConn.close(); } catch (e) { /* noop */ }
            peerConn = null;
        }
        peerConnectionId = null;
    }

    // ----- WebRTC -----

    function createPeer() {
        const pc = new RTCPeerConnection(rtcConfig);

        pc.onicecandidate = function (event) {
            if (event.candidate && peerConnectionId && connection) {
                connection.invoke('SendIce', peerConnectionId, JSON.stringify(event.candidate)).catch(function () {});
            }
        };

        pc.ontrack = function (event) {
            const remote = $('vcRemoteVideo');
            if (remote) {
                remote.srcObject = event.streams[0];
            }
        };

        if (localStream) {
            localStream.getTracks().forEach(function (t) { pc.addTrack(t, localStream); });
        }
        return pc;
    }

    async function sendOffer() {
        await peerConn.setLocalDescription();
        await connection.invoke('SendOffer', peerConnectionId, JSON.stringify(peerConn.localDescription));
    }

    async function onReceiveOffer(from, sdp) {
        peerConnectionId = from;
        if (!peerConn) peerConn = createPeer();
        await peerConn.setRemoteDescription(new RTCSessionDescription(sdp));
        await peerConn.setLocalDescription();
        await connection.invoke('SendAnswer', from, JSON.stringify(peerConn.localDescription));
    }

    async function onReceiveAnswer(from, sdp) {
        if (!peerConn) return;
        await peerConn.setRemoteDescription(new RTCSessionDescription(sdp));
    }

    async function onReceiveIce(from, candidate) {
        if (!peerConn) return;
        try {
            await peerConn.addIceCandidate(new RTCIceCandidate(candidate));
        } catch (err) {
            console.error('addIceCandidate error', err);
        }
    }

    // ----- Controls -----

    function toggleMic() {
        if (!localStream) return;
        micEnabled = !micEnabled;
        localStream.getAudioTracks().forEach(function (t) { t.enabled = micEnabled; });
        $('vcBtnMic').classList.toggle('vc-off', !micEnabled);
        $('vcBtnMic').innerHTML = '<i class="fa fa-' + (micEnabled ? 'microphone' : 'microphone-slash') + '"></i>';
    }

    function toggleCam() {
        if (!localStream) return;
        camEnabled = !camEnabled;
        localStream.getVideoTracks().forEach(function (t) { t.enabled = camEnabled; });
        $('vcBtnCam').classList.toggle('vc-off', !camEnabled);
        $('vcBtnCam').innerHTML = '<i class="fa fa-' + (camEnabled ? 'video' : 'video-slash') + '"></i>';
    }

    function replaceVideoTrack(track) {
        if (!peerConn) return;
        const sender = peerConn.getSenders().find(function (s) { return s.track && s.track.kind === 'video'; });
        if (sender) sender.replaceTrack(track).catch(function () {});
    }

    async function shareScreen() {
        if (screenStream) {
            screenStream.getTracks().forEach(function (t) { t.stop(); });
            screenStream = null;
            const cam = localStream ? localStream.getVideoTracks()[0] : null;
            replaceVideoTrack(cam || null);
            $('vcBtnScreen').classList.remove('vc-off');
            return;
        }
        try {
            screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true });
            const track = screenStream.getVideoTracks()[0];
            replaceVideoTrack(track);
            $('vcBtnScreen').classList.add('vc-off');
            track.addEventListener('ended', function () {
                screenStream = null;
                const cam = localStream ? localStream.getVideoTracks()[0] : null;
                replaceVideoTrack(cam || null);
                $('vcBtnScreen').classList.remove('vc-off');
            });
        } catch (err) {
            console.error('screen share failed', err);
        }
    }

    // ----- Call actions -----

    function onCancel() {
        if (connection && cfg) {
            connection.invoke('HangUp', cfg.recipientId).catch(function () {});
        }
        cleanupAfterCall();
    }

    function onHangup() {
        if (connection && peerConnectionId) {
            connection.invoke('HangUp', cfg.recipientId).catch(function () {});
        }
        cleanupAfterCall();
    }

    function cleanupAfterCall() {
        ringStop();
        closeStreams();
        closePeer();
        closeOverlay();
        hideIncoming();
        state = 'idle';
        myRole = null;
        currentRoomId = null;
    }

    async function onAccept() {
        const callerId = $('vcIncomingCard').dataset.callerId;
        const callerConnectionId = $('vcIncomingCard').dataset.callerConnectionId;
        const roomId = $('vcIncomingCard').dataset.roomId;
        hideIncoming();
        ringStop();

        state = 'active';
        myRole = 'callee';
        peerConnectionId = callerConnectionId;
        currentRoomId = roomId;
        setOverlayPhase('active', 'Connected with ' + cfg.recipientUsername);

        try {
            if (!localStream) localStream = await getUserMedia();
        } catch (err) {
            $('vcStatus').textContent = 'Camera unavailable — you can still receive.';
        }
        attachLocal();
        peerConn = createPeer();

        await ensureConnection();
        connection.invoke('AcceptCall', callerId, roomId).catch(function (err) { console.error(err); });
    }

    function onDecline() {
        const callerId = $('vcIncomingCard').dataset.callerId;
        hideIncoming();
        ringStop();
        if (connection) {
            connection.invoke('DeclineCall', callerId).catch(function () {});
        }
    }

    async function startCall() {
        if (!cfg || state !== 'idle') return;
        buildOverlay();
        hideIncoming();

        state = 'ringing';
        myRole = 'caller';
        currentRoomId = 'dm:' + [cfg.currentUserId, cfg.recipientId].sort().join(':');
        setOverlayPhase('ringing', 'Calling ' + cfg.recipientUsername + '…');
        ringStart();

        await ensureConnection();
        connection.invoke('CallUser', cfg.recipientId, currentRoomId).catch(function (err) { console.error(err); });
    }

    // ----- Hub events -----

    function bindHubEvents() {
        connection.on('IncomingCall', function (payload) {
            if (state !== 'idle') {
                // Busy — politely decline.
                connection.invoke('DeclineCall', payload.callerId).catch(function () {});
                return;
            }
            state = 'incoming';
            buildIncomingCard();
            const card = $('vcIncomingCard');
            card.dataset.callerId = payload.callerId;
            card.dataset.callerConnectionId = payload.callerConnectionId;
            card.dataset.roomId = payload.roomId;
            $('vcIncomingName').textContent = payload.callerName || 'Someone';
            card.style.display = 'block';
            ringStart();
        });

        connection.on('CallRinging', function () {
            // Caller keeps the "Calling…" overlay.
        });

        connection.on('CallAccepted', async function (payload) {
            if (state !== 'ringing') return;
            state = 'active';
            myRole = 'caller';
            peerConnectionId = payload.calleeConnectionId;
            currentRoomId = payload.roomId;
            ringStop();
            setOverlayPhase('active', 'Connected with ' + cfg.recipientUsername);

            try {
                if (!localStream) localStream = await getUserMedia();
            } catch (err) {
                $('vcStatus').textContent = 'Camera unavailable — sending without video.';
            }
            attachLocal();
            peerConn = createPeer();
            try {
                await sendOffer();
            } catch (err) {
                console.error('offer error', err);
            }
        });

        connection.on('CallDeclined', function () {
            if (state !== 'ringing') return;
            ringStop();
            $('vcStatus').textContent = cfg.recipientUsername + ' declined the call.';
            $('vcBtnCancel').style.display = 'none';
            state = 'idle';
            setTimeout(closeOverlay, 1800);
        });

        connection.on('UserOffline', function () {
            if (state !== 'ringing') return;
            ringStop();
            $('vcStatus').textContent = cfg.recipientUsername + ' is offline.';
            $('vcBtnCancel').style.display = 'none';
            state = 'idle';
            setTimeout(closeOverlay, 1800);
        });

        connection.on('CallEnded', function () {
            ringStop();
            cleanupAfterCall();
        });

        connection.on('ReceiveOffer', function (from, sdp) {
            onReceiveOffer(from, JSON.parse(sdp)).catch(function (err) { console.error(err); });
        });

        connection.on('ReceiveAnswer', function (from, sdp) {
            onReceiveAnswer(from, JSON.parse(sdp)).catch(function (err) { console.error(err); });
        });

        connection.on('ReceiveIce', function (from, candidate) {
            onReceiveIce(from, JSON.parse(candidate)).catch(function (err) { console.error(err); });
        });
    }

    function hideIncoming() {
        buildIncomingCard();
        $('vcIncomingCard').style.display = 'none';
    }

    return {
        initialize: function (options) {
            cfg = options;
            buildOverlay();
            buildIncomingCard();
            if (cfg.connectOnLoad) {
                ensureConnection().catch(function (err) { console.error('video hub connect failed', err); });
            }
        },
        startCall: function () {
            startCall();
        },
        isActive: function () {
            return state === 'active' || state === 'ringing';
        }
    };
})();
