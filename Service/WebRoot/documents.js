$(document).ready(function() {
    let scanPathTemplate, documentTemplate;

    async function loadTemplates() {
        scanPathTemplate = await $.get('documents-scanpath-template.html');
        documentTemplate = await $.get('documents-document-template.html');
    }

    function getErrorMessage(err) {
        if (err.status === 0) {
            return "Unable to connect to service. Check your connection.";
        } else if (err.statusText === 'timeout') {
            return "The request timed out. Please try again.";
        } else {
            return err.responseText || `Error ${err.status}: ${err.statusText}`;
        }
    }

    function createTagElement(tagText, options) {
        const { isInitial = false, onAdd, onDelete } = options;
        const $tagSpan = $('<span class="name-tag"></span>').text(tagText);
        const $symbol = $('<span class="name-tag-symbol"></span>');

        const removeTagHandler = async () => {
            if (onDelete) {
                $tagSpan.addClass('disabled').removeClass('error').attr('title', '');
                $symbol.off('click');
                try {
                    await onDelete(tagText);
                    $tagSpan.remove();
                } catch (err) {
                    const errorText = getErrorMessage(err);
                    $tagSpan.removeClass('disabled').addClass('error').attr('title', errorText);
                    $symbol.on('click', removeTagHandler);
                }
            } else {
                $tagSpan.remove();
            }
        };

        const addTagHandler = async () => {
            if (onAdd) {
                $tagSpan.addClass('disabled').removeClass('error').attr('title', '');
                $symbol.addClass('retry').html('&#8634;').off('click');
                try {
                    await onAdd(tagText);
                    $tagSpan.removeClass('disabled');
                    $symbol.removeClass('retry').html('&times;').on('click', removeTagHandler);
                } catch (err) {
                    const errorText = getErrorMessage(err);
                    $tagSpan.removeClass('disabled').addClass('error').attr('title', errorText);
                    $symbol.addClass('retry').html('&#8634;').on('click', addTagHandler);
                }
            } else {
                $tagSpan.removeClass('disabled');
                $symbol.removeClass('retry').html('&times;').on('click', removeTagHandler);
            }
        };

        if (isInitial || !onAdd) {
            $symbol.html('&times;').on('click', removeTagHandler);
            $tagSpan.append($symbol);
        } else {
            $tagSpan.addClass('disabled');
            $symbol.addClass('retry').html('&#8634;');
            $tagSpan.append($symbol);
            addTagHandler();
        }

        return $tagSpan;
    }

    function renderScanPaths(scanPaths) {
        const $container = $('#documents-container');
        $container.empty();

        if (scanPaths.length === 0) {
            $container.html('<div class="info-message">No scan paths available.</div>');
            return;
        }

        scanPaths.forEach(scanPath => {
            let html = scanPathTemplate
                .replace(/{{name}}/g, scanPath.name || scanPath.path || 'Unnamed ScanPath')
                .replace(/{{documentCount}}/g, '...');
            
            const $scanPathItem = $(html);
            $scanPathItem.attr('data-id', scanPath.id);
            const $tagsContainer = $scanPathItem.find('.item-tags');
            
            if (scanPath.tags && scanPath.tags.length > 0) {
                scanPath.tags.forEach(tag => {
                    const $tagSpan = $('<span class="name-tag"></span>').text(tag);
                    $tagsContainer.append($tagSpan);
                });
            }

            const $header = $scanPathItem.find('.collapsible-header');
            $header.on('click', async function() {
                const $item = $(this).closest('.collapsible-item');
                const $content = $item.find('> .collapsible-content');
                
                $item.toggleClass('expanded');
                $content.toggleClass('hidden');

                if ($item.hasClass('expanded') && !$item.data('loaded')) {
                    await loadDocuments(scanPath.id, $content, $scanPathItem);
                    $item.data('loaded', true);
                }
            });

            $container.append($scanPathItem);
        });
    }

    async function loadDocuments(scanPathId, $container, $scanPathItem) {
        $container.html('<div class="loading-small">Loading documents...</div>');
        try {
            const documents = await $.getJSON(`/api/v1/scanpaths/${scanPathId}/documents`);
            $container.empty();
            
            // Update document count in header
            $scanPathItem.find('.item-info').text(`(${documents.length} documents)`);

            if (documents.length === 0) {
                $container.html('<div class="info-message">No documents found.</div>');
                return;
            }

            documents.forEach(doc => {
                let docHtml = documentTemplate
                    .replace(/{{name}}/g, doc.name || 'Unnamed Document')
                    .replace(/{{pageCount}}/g, doc.pages || 0)
                    .replace(/{{wordCount}}/g, doc.words || 0);
                
                const $docItem = $(docHtml);
                $docItem.attr('data-id', doc.id);
                
                const $docTagsContainer = $docItem.find('.item-tags');
                const $newTagInput = $docItem.find('.new-tag-input');
                
                const tagOptions = {
                    onAdd: async (tagText) => {
                        await $.ajax({
                            url: `/api/v1/documents/${doc.id}/tags/${encodeURIComponent(tagText)}`,
                            type: 'POST'
                        });
                    },
                    onDelete: async (tagText) => {
                        await $.ajax({
                            url: `/api/v1/documents/${doc.id}/tags/${encodeURIComponent(tagText)}`,
                            type: 'DELETE'
                        });
                    }
                };

                if (doc.tags && doc.tags.length > 0) {
                    doc.tags.forEach(tag => {
                        $newTagInput.before(createTagElement(tag, { ...tagOptions, isInitial: true }));
                    });
                }

                $newTagInput.on('keydown', (e) => {
                    if (e.key === ',' || e.key === ';') {
                        e.preventDefault();
                    }
                    if (e.key === 'Enter') {
                        let tagText = $newTagInput.val().trim();
                        if (tagText.endsWith(',') || tagText.endsWith(';')) {
                            tagText = tagText.slice(0, -1).trim();
                        }
                        if (tagText) {
                            $newTagInput.before(createTagElement(tagText, tagOptions));
                            $newTagInput.val('');
                        }
                    }
                });

                $container.append($docItem);
            });
        } catch (error) {
            console.error('Error fetching documents:', error);
            $container.html('<div class="error-message">Failed to load documents.</div>');
        }
    }

    async function fetchScanPaths() {
        try {
            const data = await $.getJSON('/api/v1/scanpaths');
            renderScanPaths(data);
        } catch (error) {
            console.error('Error fetching scan paths:', error);
            $('#documents-container').html('<div class="error-message">Failed to load scan paths.</div>');
        } finally {
            $('#loading-spinner').removeClass('visible').addClass('hidden');
        }
    }

    loadTemplates().then(fetchScanPaths);
});
