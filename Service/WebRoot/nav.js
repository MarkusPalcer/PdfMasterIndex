$.ajaxSetup({ cache: false });

$(async () => {
    // Inject navigation from nav.html
    try {
        const navFragment = await $.get('nav.html');
        $('body').prepend(navFragment);
        
        $('#dismiss-warning').on('click', () => {
            sessionStorage.setItem('settings-warning-dismissed', 'true');
            $('#settings-writable-warning').addClass('hidden');
        });
        
        initializeSettingsWritableStatus();
    } catch (err) {
        console.error("Failed to load navigation:", err);
    }
});

function updateWarningPanel(isWritable) {
    if (isWritable) {
        sessionStorage.removeItem('settings-warning-dismissed');
        $('#settings-writable-warning').addClass('hidden');
    } else {
        const isDismissed = sessionStorage.getItem('settings-warning-dismissed') === 'true';
        if (!isDismissed) {
            $('#settings-writable-warning').removeClass('hidden');
        } else {
            $('#settings-writable-warning').addClass('hidden');
        }
    }
}

async function initializeSettingsWritableStatus() {
    try {
        const isWritable = await $.get('/api/v1/settings/writable');
        updateWarningPanel(isWritable);
    } catch (err) {
        console.error("Failed to fetch settings writable status:", err);
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/settings-hub")
        .build();

    connection.on("SettingsWritableChanged", (isWritable) => {
        updateWarningPanel(isWritable);
    });

    try {
        await connection.start();
    } catch (err) {
        console.error("SignalR connection error:", err);
    }
}