document.addEventListener("click", function (e) {
    if (e.target.classList && e.target.classList.contains("modal-overlay")) {
        e.target.classList.remove("open");
    }
});
