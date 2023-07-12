export var blazorPushNotifications = {
    requestSubscription: async (publicKey) => {
        const worker = await navigator.serviceWorker.getRegistration();
        const existingSubscription = await worker.pushManager.getSubscription();

        if (!existingSubscription) {
            const newSubscription = await subscribe(worker, publicKey);
            if (newSubscription) {
                return {
                    url: newSubscription.endpoint,
                    p256dh: arrayBufferToBase64(newSubscription.getKey('p256dh')),
                    auth: arrayBufferToBase64(newSubscription.getKey('auth'))
                };
            }
        }
    },
    checkSubscription: async () => {
        const worker = await navigator.serviceWorker.getRegistration();
        const existingSubscription = await worker.pushManager.getSubscription();

        if (!existingSubscription) {
            return null;
        }
        else {
            return existingSubscription.endpoint;
        }
    },
    reconnectSubscription: async () => {
        const worker = await navigator.serviceWorker.getRegistration();
        const existingSubscription = await worker.pushManager.getSubscription();
        if (existingSubscription)
            existingSubscription.unsubscribe();
    }
};

async function subscribe(worker, publicKey) {
    try {
        return await worker.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: publicKey
        });
    } catch (error) {
        if (error.name === 'NotAllowedError') {
            return null;
        }
        throw error;
    }
}

function arrayBufferToBase64(buffer) {
    var binary = '';
    var bytes = new Uint8Array(buffer);
    var len = bytes.byteLength;
    for (var i = 0; i < len; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
}