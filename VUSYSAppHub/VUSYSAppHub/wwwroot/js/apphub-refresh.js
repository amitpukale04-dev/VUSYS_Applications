window.appHubRefresh = {
    reload: function () {

        const btn = document.getElementById("btnRefreshAppHub");

        if (btn) {
            btn.classList.add("spin");
        }

        setTimeout(() => {
            window.location.reload();
        }, 300);
    }
};