(function () {
    const cfg = window.VideoRoomConfig;
    if (!cfg) return;

    const roomId = cfg.roomId;
    const maxSpots = cfg.maxSpots;
    const hubUrl = cfg.hubUrl || '/videohub';

    let connection = null;
    let localStream = null;
    let camEnabled = true;
    let micEnabled = true;
    let mySlot = -1;
    let screenStream = null;

    const peers = {};          // targetConnectionId -> { pc, connId, slotIndex, polite, makingOffer, ignoreOffer }
    const occupantBySlot = {}; // slotIndex -> { connectionId, userId, username }
    const slotByConnection = {}; // connectionId -> slotIndex
    const viewerByConnection = {}; // connectionId -> { connectionId, userId, username }

    const rtcConfig = {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' },
            { urls: 'stun:stun2.l.google.com:19302' }
        ]
    };

    const $ = (id) => document.getElementById(id);

    function slotEl(i) { return $('slot-' + i); }
    function videoEl(i) { return $('video-' + i); }

    function esc(s) {
        const d = document.createElement('div');
        d.textContent = s == null ? '' : String(s);
        return d.innerHTML;
    }

    function renderSlot(i, occupant) {
        const slot = slotEl(i);
        if (!slot) return;
        const video = videoEl(i);
        const placeholder = slot.querySelector('.slot-placeholder');
        const claim = slot.querySelector('.claim-btn');
        const bar = slot.querySelector('.occupant-bar');
        const name = bar.querySelector('.occupant-name');

        if (occupant) {
            slot.classList.add('slot-occupied');
            if (occupant.connectionId === connection.connectionId) slot.classList.add('slot-mine');
            name.textContent = occupant.username;
            bar.classList.remove('d-none');
            claim.classList.add('d-none');
            placeholder.style.display = 'none';
        } else {
            slot.classList.remove('slot-occupied', 'slot-mine');
            bar.classList.add('d-none');
            claim.classList.remove('d-none');
            placeholder.style.display = 'flex';
            if (video) {
                video.srcObject = null;
                video.classList.remove('vc-playing');
            }
        }
    }

    function updateInRoom() {
        const occupied = Object.keys(occupantBySlot).length;
        const viewers = Object.keys(viewerByConnection).length;
        const el = $('inRoomCount');
        if (el) el.textContent = (occupied + viewers) + ' in room';
        updateParticipantList();
    }

    function updateParticipantList() {
        const list = $('participantList');
        if (!list) return;
        const items = [];
        const isMe = (connId) => connection && connId === connection.connectionId;
        for (let i = 0; i < maxSpots; i++) {
            const occ = occupantBySlot[i];
            if (occ) {
                const suffix = isMe(occ.connectionId) ? ' (You)' : '';
                items.push('<li class="list-group-item d-flex justify-content-between align-items-center"><span>' + esc(occ.username) + suffix + '</span><span class="badge bg-danger">LIVE</span></li>');
            }
        }
        for (const connId of Object.keys(viewerByConnection)) {
            const v = viewerByConnection[connId];
            const suffix = isMe(connId) ? ' (You)' : '';
            items.push('<li class="list-group-item d-flex justify-content-between align-items-center"><span>' + esc(v.username) + suffix + '</span><span class="badge bg-secondary">Viewing</span></li>');
        }
        if (items.length === 0) items.push('<li class="list-group-item text-muted">No one is in the room yet. Chat is open to everyone.</li>');
        list.innerHTML = items.join('');
    }

    function appendChat(senderId, senderName, content, timestamp) {
        const list = $('chatList');
        if (!list) return;
        const isMine = senderId === cfg.currentUserId;
        const date = new Date(timestamp);
        const timeStr = date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
        const div = document.createElement('div');
        div.className = 'd-flex ' + (isMine ? 'justify-content-end' : 'justify-content-start');
        div.innerHTML = '<div class="p-2 rounded-3 ' + (isMine ? 'bg-primary text-white' : 'bg-light') + '" style="max-width:85%;word-wrap:break-word">' +
            '<small class="d-block ' + (isMine ? 'text-white-50' : 'text-muted') + '" style="font-size:0.7rem"><strong>' + esc(senderName) + '</strong> &middot; ' + timeStr + '</small>' +
            esc(content) + '</div>';
        list.appendChild(div);
        list.scrollTop = list.scrollHeight;
    }

    // ----- WebRTC mesh (perfect negotiation) -----

    function ensurePeer(occupant, slotIndex) {
        const targetConnectionId = occupant.connectionId;
        if (!connection || targetConnectionId === connection.connectionId) return null;
        if (peers[targetConnectionId]) {
            if (slotIndex != null) peers[targetConnectionId].slotIndex = slotIndex;
            else if (occupant.slotIndex !== undefined) peers[targetConnectionId].slotIndex = occupant.slotIndex;
            return peers[targetConnectionId];
        }

        const polite = cfg.currentUserId >= occupant.userId;
        const pc = new RTCPeerConnection(rtcConfig);
        const entry = {
            pc,
            connId: targetConnectionId,
            slotIndex: slotIndex != null ? slotIndex : occupant.slotIndex,
            polite,
            makingOffer: false,
            ignoreOffer: false
        };
        peers[targetConnectionId] = entry;

        pc.onicecandidate = (event) => {
            if (event.candidate) {
                connection.invoke('SendIce', targetConnectionId, JSON.stringify(event.candidate)).catch(() => {});
            }
        };

        pc.ontrack = (event) => {
            const e = peers[targetConnectionId];
            if (!e || e.slotIndex == null) return;
            const video = videoEl(e.slotIndex);
            if (video) {
                video.srcObject = event.streams[0];
                video.classList.add('vc-playing');
            }
        };

        if (localStream) {
            localStream.getTracks().forEach((t) => pc.addTrack(t, localStream));
        }

        return entry;
    }

    async function negotiate(entry) {
        try {
            entry.makingOffer = true;
            await entry.pc.setLocalDescription();
            await connection.invoke('SendOffer', entry.connId, JSON.stringify(entry.pc.localDescription));
        } catch (err) {
            console.error('negotiate error', err);
        } finally {
            entry.makingOffer = false;
        }
    }

    async function onReceivedOffer(from, sdp) {
        let entry = peers[from];
        if (!entry) {
            const occ = slotByConnection[from] != null ? occupantBySlot[slotByConnection[from]] : null;
            entry = ensurePeer(occ || { connectionId: from, userId: from, username: from }, slotByConnection[from]);
            if (!entry) return;
        }
        const offerCollision = entry.makingOffer || entry.pc.signalingState !== 'stable';
        entry.ignoreOffer = !entry.polite && offerCollision;
        if (entry.ignoreOffer) return;
        await entry.pc.setRemoteDescription(new RTCSessionDescription(sdp));
        await entry.pc.setLocalDescription();
        await connection.invoke('SendAnswer', from, JSON.stringify(entry.pc.localDescription));
    }

    async function onReceivedAnswer(from, sdp) {
        const entry = peers[from];
        if (!entry) return;
        await entry.pc.setRemoteDescription(new RTCSessionDescription(sdp));
    }

    async function onReceivedIce(from, candidate) {
        const entry = peers[from];
        if (!entry) return;
        try {
            await entry.pc.addIceCandidate(new RTCIceCandidate(candidate));
        } catch (err) {
            if (!entry.ignoreOffer) console.error('addIceCandidate error', err);
        }
    }

    function addLocalTracksToPeers() {
        if (!localStream) return;
        Object.values(peers).forEach((entry) => {
            localStream.getTracks().forEach((t) => {
                const hasKind = entry.pc.getSenders().some((s) => s.track && s.track.kind === t.kind);
                if (!hasKind) entry.pc.addTrack(t, localStream);
            });
        });
    }

    function connectToAllExisting() {
        for (let i = 0; i < maxSpots; i++) {
            const occ = occupantBySlot[i];
            if (!occ || occ.connectionId === connection.connectionId) continue;
            const entry = ensurePeer(occ, i);
            if (entry && localStream && !entry.polite && entry.pc.signalingState === 'stable') {
                negotiate(entry);
            }
        }
    }

    function trackOccupant(slotIndex, occupant) {
        occupantBySlot[slotIndex] = occupant;
        slotByConnection[occupant.connectionId] = slotIndex;
        renderSlot(slotIndex, occupant);
        if (mySlot >= 0 && occupant.connectionId !== connection.connectionId) {
            const entry = ensurePeer(occupant, slotIndex);
            if (entry && localStream && !entry.polite && entry.pc.signalingState === 'stable') {
                negotiate(entry);
            }
        }
        updateInRoom();
    }

    function vacateSlot(slotIndex) {
        const occ = occupantBySlot[slotIndex];
        if (occ && occ.connectionId !== connection.connectionId) {
            const entry = peers[occ.connectionId];
            if (entry) {
                try { entry.pc.close(); } catch (e) { /* noop */ }
                delete peers[occ.connectionId];
            }
            delete slotByConnection[occ.connectionId];
        }
        delete occupantBySlot[slotIndex];
        renderSlot(slotIndex, null);
        updateInRoom();
    }

    // ----- Camera / slot control -----

    async function claimSlot(index) {
        if (mySlot >= 0) return;
        if (occupantBySlot[index]) return;

        const btn = slotEl(index)?.querySelector('.claim-btn');
        if (btn) { btn.disabled = true; btn.textContent = 'Starting…'; }

        try {
            if (!localStream) {
                localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
            }
            mySlot = index;
            occupantBySlot[index] = { connectionId: connection.connectionId, userId: cfg.currentUserId, username: cfg.currentUsername };
            slotByConnection[connection.connectionId] = index;
            renderSlot(index, { connectionId: connection.connectionId, userId: cfg.currentUserId, username: cfg.currentUsername });
            const local = videoEl(index);
            if (local) {
                local.srcObject = localStream;
                local.classList.add('vc-playing');
                local.muted = true;
            }
            updateInRoom();
            addLocalTracksToPeers();
            await connection.invoke('TakeSlot', String(roomId), index);
            connectToAllExisting();
            enableControls(true);
            $('liveStatus').textContent = 'You are live on slot ' + (index + 1) + '.';
        } catch (err) {
            console.error('claim slot failed', err);
            const b = slotEl(index)?.querySelector('.claim-btn');
            if (b) { b.disabled = false; b.textContent = 'Go Live Here'; }
            $('liveStatus').textContent = 'Could not access camera/microphone: ' + (err && err.name ? err.name : err);
        }
    }

    async function leaveVideo() {
        if (mySlot >= 0 && connection) {
            try { await connection.invoke('LeaveSlot', String(roomId)); } catch (e) { /* noop */ }
        }
        closeAllPeers();
        stopLocalStream();
        mySlot = -1;
        enableControls(false);
        $('liveStatus').textContent = 'You are viewing. Click a spot to go live.';
        updateInRoom();
    }

    function stopLocalStream() {
        if (localStream) {
            localStream.getTracks().forEach((t) => t.stop());
            localStream = null;
        }
        if (screenStream) {
            screenStream.getTracks().forEach((t) => t.stop());
            screenStream = null;
        }
    }

    function closeAllPeers() {
        Object.values(peers).forEach((entry) => {
            try { entry.pc.close(); } catch (e) { /* noop */ }
        });
        for (const k of Object.keys(peers)) delete peers[k];
    }

    function enableControls(on) {
        const btnMic = $('btnMic');
        const btnCam = $('btnCam');
        const btnScreen = $('btnScreen');
        const btnLeave = $('btnLeave');
        btnMic.disabled = !on;
        btnCam.disabled = !on;
        btnScreen.disabled = !on;
        btnLeave.classList.toggle('d-none', !on);
    }

    function toggleMic() {
        if (!localStream) return;
        micEnabled = !micEnabled;
        localStream.getAudioTracks().forEach((t) => (t.enabled = micEnabled));
        $('btnMic').querySelector('.mic-on').classList.toggle('d-none', !micEnabled);
        $('btnMic').querySelector('.mic-off').classList.toggle('d-none', micEnabled);
    }

    function toggleCam() {
        if (!localStream) return;
        camEnabled = !camEnabled;
        localStream.getVideoTracks().forEach((t) => (t.enabled = camEnabled));
        $('btnCam').querySelector('.cam-on').classList.toggle('d-none', !camEnabled);
        $('btnCam').querySelector('.cam-off').classList.toggle('d-none', camEnabled);
    }

    async function shareScreen() {
        if (screenStream) {
            screenStream.getTracks().forEach((t) => t.stop());
            screenStream = null;
            replaceVideoTrack(getCameraVideoTrack());
            $('btnScreen').textContent = 'Share Screen';
            return;
        }
        try {
            screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true });
            screenStream.getVideoTracks()[0].addEventListener('ended', () => {
                screenStream = null;
                replaceVideoTrack(getCameraVideoTrack());
                $('btnScreen').textContent = 'Share Screen';
            });
            replaceVideoTrack(screenStream.getVideoTracks()[0]);
            $('btnScreen').textContent = 'Stop Screen';
        } catch (err) {
            console.error('screen share failed', err);
        }
    }

    function getCameraVideoTrack() {
        if (!localStream) return null;
        return localStream.getVideoTracks()[0] || null;
    }

    function replaceVideoTrack(track) {
        Object.values(peers).forEach((entry) => {
            const sender = entry.pc.getSenders().find((s) => s.track && s.track.kind === 'video');
            if (sender) sender.replaceTrack(track).catch(() => {});
        });
        if (mySlot >= 0) {
            const local = videoEl(mySlot);
            if (local && localStream) local.srcObject = localStream;
        }
    }

    // ----- Signaling events -----

    function viewerByConnectionRefill(viewers) {
        for (const k of Object.keys(viewerByConnection)) delete viewerByConnection[k];
        (viewers || []).forEach((v) => { viewerByConnection[v.connectionId] = v; });
    }

    function bindHubEvents() {
        connection.on('RoomState', (state) => {
            const slots = state.slots || [];
            for (let i = 0; i < maxSpots; i++) {
                const occ = slots[i];
                if (occ) trackOccupant(i, occ);
                else vacateSlot(i);
            }
            viewerByConnectionRefill(state.viewers || []);
            updateInRoom();
        });

        connection.on('SlotOccupied', (slotIndex, occupant) => {
            trackOccupant(slotIndex, occupant);
        });

        connection.on('SlotVacated', (slotIndex) => {
            vacateSlot(slotIndex);
        });

        connection.on('ParticipantJoined', (participant) => {
            if (!occupantBySlot[slotByConnection[participant.connectionId]]) {
                viewerByConnection[participant.connectionId] = participant;
            }
            updateInRoom();
        });

        connection.on('ParticipantLeft', (connectionId) => {
            delete viewerByConnection[connectionId];
            updateInRoom();
        });

        connection.on('ViewerCountChanged', (count) => {
            const el = $('viewerCount');
            if (el) el.textContent = count + ' watching';
        });

        connection.on('ReceiveVideoRoomMessage', (senderId, senderName, content, timestamp) => {
            appendChat(senderId, senderName, content, timestamp);
        });

        connection.on('ReceiveOffer', (from, sdp) => onReceivedOffer(from, JSON.parse(sdp)));
        connection.on('ReceiveAnswer', (from, sdp) => onReceivedAnswer(from, JSON.parse(sdp)));
        connection.on('ReceiveIce', (from, candidate) => onReceivedIce(from, JSON.parse(candidate)));
    }

    function wireUI() {
        document.getElementById('videoGrid').addEventListener('click', (e) => {
            const btn = e.target.closest('.claim-btn');
            if (!btn) return;
            const slotDiv = btn.closest('.video-slot');
            if (!slotDiv) return;
            claimSlot(parseInt(slotDiv.dataset.slotIndex, 10));
        });

        $('btnMic').addEventListener('click', toggleMic);
        $('btnCam').addEventListener('click', toggleCam);
        $('btnScreen').addEventListener('click', shareScreen);
        $('btnLeave').addEventListener('click', leaveVideo);

        const chatForm = $('chatForm');
        const chatInput = $('chatInput');
        chatForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const content = chatInput.value.trim();
            if (!content) return;
            connection.invoke('SendVideoRoomMessage', String(roomId), content).catch((err) => console.error(err));
            chatInput.value = '';
        });

        window.addEventListener('beforeunload', () => {
            if (connection) connection.stop().catch(() => {});
        });
    }

    async function start() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();

        bindHubEvents();
        wireUI();
        enableControls(false);

        connection.onreconnected(() => {
            mySlot = -1;
            closeAllPeers();
            stopLocalStream();
            enableControls(false);
            $('liveStatus').textContent = 'Reconnected. Click a spot to go live.';
            connection.invoke('JoinRoom', String(roomId)).catch((e) => console.error('rejoin failed', e));
        });

        try {
            await connection.start();
            await connection.invoke('JoinRoom', String(roomId));
        } catch (err) {
            console.error('video room start failed', err);
        }
    }

    start();
})();
