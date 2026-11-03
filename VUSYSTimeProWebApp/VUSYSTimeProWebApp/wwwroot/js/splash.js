// Wait for the window to load and then hide the splash screen
window.addEventListener('load', () => {
    // Simulate a delay for splash screen, in case you want to keep it visible longer
    setTimeout(() => {
        document.getElementById('splash-screen').style.display = 'none';
        document.getElementById('maincontent').style.display = '';
    }, 3000); // Hide after 3 seconds (adjust the time as necessary)
});
