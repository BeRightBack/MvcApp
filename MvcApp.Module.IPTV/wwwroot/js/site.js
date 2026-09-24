document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("header-toggle");
    const navBar = document.getElementById("nav-bar");
    const mainContent = document.getElementById("main-content");
    const topNavbar = document.querySelector(".navbar");

    toggleButton.addEventListener("click", function () {
        navBar.classList.toggle("show");
        mainContent.classList.toggle("shifted");
        topNavbar.classList.toggle("shifted");
    });
});
