$(async () => {
    // Inject scan styles
    if (!$('link[href="scan.css"]').length) {
        $('head').append('<link rel="stylesheet" href="scan.css">');
    }

    // Inject status container from status.html
    const statusFragment = await $.get('scan.html');
    $('body').append(statusFragment);

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/scan-hub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    const $currentStepText = $('#current-step-text');
    const $currentFileText = $('#current-file-text');
    const $topProgressBarFill = $('#top-progress-bar-fill');
    const $bottomProgressBarFill = $('#bottom-progress-bar-fill');
    const $topProgressSection = $('#top-progress-section');
    const $bottomProgressSection = $('#bottom-progress-section');
    const $scanLink = $('#scan-link');
    const $cancelLink = $('#cancel-link');
    const $cancelSpinner = $('#cancel-spinner');

    function updateUI(status) {
        if (status.isRunning) {
            $scanLink.removeClass('visible');
            $cancelLink.addClass('visible');
            $currentStepText.removeClass('hidden');
            $topProgressSection.removeClass('hidden');
            if (status.currentStep === 3 || status.currentStep === "ParseFiles") {
                $bottomProgressSection.removeClass('hidden');
            } else {
                $bottomProgressSection.addClass('hidden');
            }
        } else {
            $scanLink.addClass('visible');
            $cancelLink.removeClass('visible');
            $cancelSpinner.removeClass('visible');
            $cancelLink.removeClass('disabled');
            $scanLink.removeClass('disabled');
            $currentStepText.addClass('hidden');
            $topProgressSection.addClass('hidden');
            $bottomProgressSection.addClass('hidden');
        }

        $currentStepText.text(status.currentStepMessage);
        $currentFileText.text(status.currentFileMessage);

        $topProgressBarFill.css('width', `${status.currentStepProgress * 100}%`);
        $bottomProgressBarFill.css('width', `${status.currentFileProgress * 100}%`);
    }

    connection.on("ScanStatusChanged", (status) => {
        console.log("Scan status changed:", status);
        updateUI(status);
    });

    connection.onclose(async () => {
        console.log("SignalR Connection lost. Reconnecting in 5s...");
        setTimeout(start, 5000);
    });

    async function start() {
        try {
            // Initial fetch of status before subscribing to events
            const status = await $.getJSON('/api/v1/scan');
            updateUI(status);

            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.error("Initialization Error: ", err);
            setTimeout(start, 5000);
        }
    }

    $scanLink.on('click', async (e) => {
        e.preventDefault();
        if ($scanLink.hasClass('disabled')) return;

        $scanLink.addClass('disabled');

        try {
            await $.ajax({
                url: '/api/v1/scan',
                type: 'POST'
            });
        } catch (err) {
            const errorText = err.responseText || `Error ${err.status}`;
            alert(`Failed to start scan: ${errorText}`);
            $scanLink.removeClass('disabled');
        }
    });

    $cancelLink.on('click', async (e) => {
        e.preventDefault();
        if ($cancelLink.hasClass('disabled')) return;

        $cancelLink.addClass('disabled');
        $cancelSpinner.addClass('visible');

        try {
            await $.ajax({
                url: '/api/v1/scan',
                type: 'DELETE'
            });
        } catch (err) {
            const errorText = err.responseText || `Error ${err.status}`;
            alert(`Failed to cancel scan: ${errorText}`);
            $cancelLink.removeClass('disabled');
            $cancelSpinner.removeClass('visible');
        }
    });

    // Start the connection.
    start();
});
