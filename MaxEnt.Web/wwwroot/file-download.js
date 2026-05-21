// Keeps the screen awake while an experiment is running via the Wake Lock API.
let _wakeLock = null;

export async function acquireWakeLock() {
    if (!('wakeLock' in navigator)) return;
    try {
        _wakeLock = await navigator.wakeLock.request('screen');
    } catch (_) { /* permission denied or unsupported – silently ignore */ }
}

export async function releaseWakeLock() {
    if (_wakeLock) {
        try { await _wakeLock.release(); } catch (_) { }
        _wakeLock = null;
    }
}

export function getBrowserUtcOffsetMinutes() {
    return -new Date().getTimezoneOffset();
}

export async function fetchText(url) {
    const r = await fetch(url);
    if (!r.ok) throw new Error(`Failed to fetch ${url}: ${r.status}`);
    return await r.text();
}

export async function downloadFileFromStream(fileName, contentStreamReference) {
    const buffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([buffer]);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
}
