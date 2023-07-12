// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('install', async event => {
    console.log('Installing service worker...');
    self.skipWaiting();
});
self.addEventListener('fetch', () => { });
self.addEventListener('push', event => {
    const payload = event.data.json();
    event.waitUntil(                    //'ПКО АС «ОСОДУ»'
        self.registration.showNotification('Программный комплекс оповещения', {
            body: payload.message,
            icon: 'favicon.png',
            vibrate: [100, 50, 100],
            data: { url: `${self.location.origin}/${payload.url}` }
        })
    );
});
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    // This looks to see if the current is already open and
    // focuses if it is
    event.waitUntil(clients.matchAll({
        type: "window"
    }).then((clientList) => {

        const hadWindowToFocus = clientList.some((windowClient) => windowClient.url === event.notification.data.url ? (windowClient.focus(), true) : false);

        if (!hadWindowToFocus) clients.openWindow(event.notification.data.url).then((windowClient) => windowClient ? windowClient.focus() : null);
    }));
});