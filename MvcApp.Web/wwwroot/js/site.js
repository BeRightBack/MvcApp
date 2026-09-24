document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("header-toggle");
    const navBar = document.getElementById("nav-bar");
    const mainContent = document.getElementById("main-content");
    const topNavbar = document.querySelector(".navbar");
    const fixedNavbar = document.querySelector(".navbar.fixed-top");

    if (toggleButton) {
        toggleButton.addEventListener("click", function () {
            if (navBar) navBar.classList.toggle("show");
            if (mainContent) mainContent.classList.toggle("shifted");
            if (topNavbar) topNavbar.classList.toggle("shifted");
        });
    }

    if (fixedNavbar) {
        var onScroll = function () {
            if (window.scrollY > 60) {
                fixedNavbar.classList.add("scrolled");
            } else {
                fixedNavbar.classList.remove("scrolled");
            }
        };
        window.addEventListener("scroll", onScroll, { passive: true });
        onScroll();
    }
});
