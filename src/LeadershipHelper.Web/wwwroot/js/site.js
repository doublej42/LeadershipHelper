(() => {
	if (!("serviceWorker" in navigator)) {
		return;
	}

	window.addEventListener("load", async () => {
		try {
			await navigator.serviceWorker.register("/service-worker.js", { updateViaCache: "none" });
		} catch (error) {
			console.error("Service worker registration failed", error);
		}
	});
})();

(() => {
	const installButton = document.getElementById("installAppButton");
	if (!installButton) {
		return;
	}

	let deferredPrompt = null;

	window.addEventListener("beforeinstallprompt", (event) => {
		event.preventDefault();
		deferredPrompt = event;
		installButton.classList.remove("d-none");
	});

	installButton.addEventListener("click", async () => {
		if (!deferredPrompt) {
			return;
		}

		deferredPrompt.prompt();
		await deferredPrompt.userChoice;
		deferredPrompt = null;
		installButton.classList.add("d-none");
	});

	window.addEventListener("appinstalled", () => {
		deferredPrompt = null;
		installButton.classList.add("d-none");
	});
})();
