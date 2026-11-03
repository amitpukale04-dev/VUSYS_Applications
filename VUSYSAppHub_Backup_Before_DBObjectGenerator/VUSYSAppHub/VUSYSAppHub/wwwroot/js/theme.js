window.themeManager = {
    applyTheme: function (theme) {
        document.documentElement.setAttribute("data-bs-theme", theme);
        localStorage.setItem("vusys-apphub-theme", theme);
        return theme === "dark";
    },

    toggleTheme: function () {
        const currentTheme = document.documentElement.getAttribute("data-bs-theme") || "light";
        const newTheme = currentTheme === "dark" ? "light" : "dark";
        document.documentElement.setAttribute("data-bs-theme", newTheme);
        localStorage.setItem("vusys-apphub-theme", newTheme);
        return newTheme === "dark";
    },

    getCurrentThemeIsDark: function () {
        const savedTheme = localStorage.getItem("vusys-apphub-theme");
        const theme = savedTheme || "light";
        document.documentElement.setAttribute("data-bs-theme", theme);
        return theme === "dark";
    }
};