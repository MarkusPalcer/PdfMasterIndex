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

    function loadLogs() {
        $.ajax({
            url: '/api/v1/log',
            method: 'GET',
            success: function (logs) {
                const logBody = $('#log-body');
                logBody.empty();

                // logs are sorted by latest first based on requirement "latest log up top"
                // Assuming API returns them in some order, we should ensure latest is top.
                // If API returns oldest first, we reverse. 
                // Let's check LoggingController: historyService.GetRecentLogs()
                
                // Sort by timestamp descending to be sure
                logs.sort((a, b) => new Date(b.timestampUtc) - new Date(a.timestampUtc));

                logs.forEach(log => {
                    const row = $('<tr>');
                    
                    const timestamp = new Date(log.timestampUtc).toLocaleString();
                    const severity = severityNames[log.severity] || 'Unknown';
                    
                    row.append($('<td class="timestamp-col">').text(timestamp));
                    row.append($('<td class="severity-col">').addClass('severity-' + log.severity).text(severity));
                    row.append($('<td class="category-col">').text(log.category));
                    row.append($('<td>').text(log.message));
                    
                    logBody.append(row);
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

    loadLogs();
});