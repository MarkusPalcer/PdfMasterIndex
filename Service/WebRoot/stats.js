$(document).ready(function() {
    async function loadStats() {
        const $container = $('#stats-container');

        try {
            const stats = await $.getJSON('/stats');
            const statItems = [
                { label: 'Documents', value: stats.documents },
                { label: 'Unique words', value: stats.uniqueWords },
                { label: 'Occurrences', value: stats.occurrences }
            ];

            statItems.forEach(stat => {
                const value = Number(stat.value ?? 0).toLocaleString();
                const $item = $('<div class="stat-item"></div>');
                $item.append($('<span class="stat-label"></span>').text(stat.label));
                $item.append($('<span class="stat-value"></span>').text(value));
                $container.append($item);
            });
        } catch (error) {
            console.error('Error fetching statistics:', error);
            $container.append('<div class="error-message">Failed to load statistics.</div>');
        } finally {
            $('#stats-spinner').removeClass('visible').addClass('hidden');
        }
    }

    loadStats();
});
