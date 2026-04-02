$(document).ready(function () {
    const severityNames = {
        0: 'Trace',
        1: 'Debug',
        2: 'Information',
        3: 'Warning',
        4: 'Error',
        5: 'Critical',
        6: 'None'
    };

    function createLogRow(log) {
        const row = $('<tr>');
        
        const timestamp = new Date(log.timestampUtc).toLocaleString();
        const severity = severityNames[log.severity] || 'Unknown';
        
        row.append($('<td class="timestamp-col">').text(timestamp));
        row.append($('<td class="severity-col">').addClass('severity-' + log.severity).text(severity));
        row.append($('<td class="category-col">').text(log.category));
        row.append($('<td>').text(log.message));
        
        return row;
    }

    function loadLogs() {
        $.ajax({
            url: '/api/v1/log',
            method: 'GET',
            success: function (logs) {
                const logBody = $('#log-body');
                logBody.empty();

                // Sort by timestamp descending to be sure
                logs.sort((a, b) => new Date(b.timestampUtc) - new Date(a.timestampUtc));

                logs.forEach(log => {
                    logBody.append(createLogRow(log));
                });

                $('#log-spinner').addClass('hidden');
                $('#log-table').removeClass('hidden');
            },
            error: function (error) {
                console.error('Error loading logs:', error);
                $('#log-spinner').addClass('hidden');
                const logBody = $('#log-body');
                logBody.append('<tr><td colspan="4" style="color: red; text-align: center;">Failed to load logs.</td></tr>');
                $('#log-table').removeClass('hidden');
            }
        });
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/log-hub")
        .build();

    connection.on("Log", function (log) {
        const logBody = $('#log-body');
        logBody.prepend(createLogRow(log));
        
        // Keep only the last 100 logs in the UI
        if (logBody.children().length > 100) {
            logBody.children().last().remove();
        }
    });

    connection.start().catch(function (err) {
        return console.error(err.toString());
    });

    loadLogs();
});