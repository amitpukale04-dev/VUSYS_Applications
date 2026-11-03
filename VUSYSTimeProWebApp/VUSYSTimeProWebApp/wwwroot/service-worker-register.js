if ('serviceWorker' in navigator) {
	navigator.serviceWorker.register('/service-worker.js', { scope: '/' })
		.then((registration) => {
			console.log("Service Worker Registered");

			// If there's an update, wait until it's installed
			registration.onupdatefound = () => {
				const newWorker = registration.installing;
				newWorker.onstatechange = () => {
					if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
						// New content is available; trigger immediate activation
						newWorker.postMessage({ action: 'skipWaiting' });
					}
				};
			}
		})
		.catch(err => console.error("Service Worker Registration Failed", err));

	// Reload the page once the new SW activates and takes control
	navigator.serviceWorker.addEventListener('controllerchange', () => {
		console.log("New service worker activated, reloading page...");
		window.location.reload();
	});
}
