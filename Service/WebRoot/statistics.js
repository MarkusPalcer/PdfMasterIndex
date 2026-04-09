$(document).ready(function() {
    let scanPathTemplate, documentTemplate, detailsTemplate;

    async function loadTemplates() {
        scanPathTemplate = await $.get('statistics-scanpath-template.html');
        documentTemplate = await $.get('statistics-document-template.html');
        detailsTemplate = await $.get('statistics-details-template.html');
    }

    async function fetchStatistics() {
        try {
            const data = await $.getJSON('/api/v1/statistics');
            renderStatistics(data);
        } catch (error) {
            console.error('Error fetching statistics:', error);
            $('#statistics-container').html('<div class="error-message">Failed to load statistics.</div>');
        } finally {
            $('#loading-spinner').removeClass('visible').addClass('hidden');
        }
    }

    function renderStatistics(scanPaths) {
        const $container = $('#statistics-container');
        $container.empty();

        if (scanPaths.length === 0) {
            $container.html('<div class="info-message">No statistics available.</div>');
            return;
        }

        scanPaths.forEach(scanPath => {
            let html = scanPathTemplate
                .replace(/{{name}}/g, scanPath.name || 'Unnamed ScanPath')
                .replace(/{{documentCount}}/g, (scanPath.documents || []).length);
            
            const $scanPathItem = $(html);
            const $tagsContainer = $scanPathItem.find('.item-tags');
            
            if (scanPath.tags && scanPath.tags.length > 0) {
                scanPath.tags.forEach(tag => {
                    const $tagSpan = $('<span class="name-tag"></span>').text(tag);
                    $tagsContainer.append($tagSpan);
                });
            }

            const $documentsContent = $scanPathItem.find('.collapsible-content');

            (scanPath.documents || []).forEach(doc => {
                let docHtml = documentTemplate
                    .replace(/{{name}}/g, doc.name || 'Unnamed Document')
                    .replace(/{{pageCount}}/g, doc.pages || 0);
                
                const $docItem = $(docHtml);
                const $detailsContent = $docItem.find('.collapsible-content');

                let detailsHtml = detailsTemplate
                    .replace(/{{totalWords}}/g, doc.words || 0)
                    .replace(/{{distinctWords}}/g, doc.distinctWords || 0);
                
                const $detailsItem = $(detailsHtml);
                $detailsContent.append($detailsItem);
                $documentsContent.append($docItem);
            });

            $container.append($scanPathItem);
        });

        // Event listener for collapsing/expanding
        $('.collapsible-header').off('click').on('click', function() {
            const $item = $(this).closest('.collapsible-item');
            const $content = $item.find('> .collapsible-content');
            
            $item.toggleClass('expanded');
            $content.toggleClass('hidden');
        });
    }

    loadTemplates().then(fetchStatistics);
});
