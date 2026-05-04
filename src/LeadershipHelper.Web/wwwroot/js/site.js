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
const installButtons = [
document.getElementById("installAppButton"),
document.getElementById("installAppButtonMobile"),
].filter(Boolean);
if (installButtons.length === 0) {
return;
}

let deferredPrompt = null;

window.addEventListener("beforeinstallprompt", (event) => {
event.preventDefault();
deferredPrompt = event;
installButtons.forEach(b => b.classList.remove("d-none"));
});

async function handleInstallClick() {
if (!deferredPrompt) {
return;
}

deferredPrompt.prompt();
await deferredPrompt.userChoice;
deferredPrompt = null;
installButtons.forEach(b => b.classList.add("d-none"));
}

installButtons.forEach(b => b.addEventListener("click", handleInstallClick));

window.addEventListener("appinstalled", () => {
deferredPrompt = null;
installButtons.forEach(b => b.classList.add("d-none"));
});
})();
