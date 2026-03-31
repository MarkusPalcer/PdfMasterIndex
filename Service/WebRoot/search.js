$(() => {
    const $searchInput = $('#search-input');
    const $searchIcon = $('.search-icon');
    const $searchError = $('#search-error');
    const $searchSpinner = $('#search-spinner');
    const $searchResultsContainer = $('#search-results-container');
    const $filterIcon = $('#filter-icon');
    const $filterPopup = $('#filter-popup');
    const $scanPathsList = $('#scanpaths-list');

    let allScanPaths = [];
    let activeScanPathIds = new Set();

    async function fetchScanPaths() {
        try {
            allScanPaths = await $.getJSON('/api/v1/scanpaths');
            activeScanPathIds = new Set(allScanPaths.map(sp => sp.id));
            renderScanPaths();
        } catch (err) {
            console.error('Failed to fetch ScanPaths:', err);
        }
    }

    function renderScanPaths() {
        $scanPathsList.empty();
        allScanPaths.forEach(sp => {
            const isOn = activeScanPathIds.has(sp.id);
            const $li = $('<li>')
                .text(sp.name)
                .addClass(isOn ? 'on' : 'off')
                .on('click', (e) => {
                    e.stopPropagation();
                    if (activeScanPathIds.has(sp.id)) {
                        activeScanPathIds.delete(sp.id);
                    } else {
                        activeScanPathIds.add(sp.id);
                    }
                    renderScanPaths();
                    onSearchTriggered();
                });
            $scanPathsList.append($li);
        });
    }

    $filterIcon.on('click', (e) => {
        e.stopPropagation();
        $filterPopup.toggleClass('hidden');
    });

    $(document).on('click', (e) => {
        if (!$filterPopup.is(e.target) && $filterPopup.has(e.target).length === 0 && !$filterIcon.is(e.target)) {
            $filterPopup.addClass('hidden');
        }
    });

    fetchScanPaths();

    let searchTimeout;
    $searchInput.on('input', function () {
        const query = $(this).val();
        if (query.includes(' ')) {
            $searchInput.addClass('invalid-input');
            $searchError.show();
        } else {
            $searchInput.removeClass('invalid-input');
            $searchError.hide();
        }

        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            onSearchTriggered();
        }, 500);
    });

    let wordTemplate;
    let locationTemplate;

    async function loadTemplates() {
        wordTemplate = await $.get('search-result-word.html');
        locationTemplate = await $.get('search-result-location.html');
    }

    async function performSearch(query) {
        if (!wordTemplate || !locationTemplate) {
            await loadTemplates();
        }
        $searchResultsContainer.empty();
        $searchSpinner.addClass('visible');

        try {
            const results = await $.ajax({
                url: '/api/v1/search',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    query: query,
                    searchPaths: Array.from(activeScanPathIds)
                })
            });

            renderResults(results);
        } catch (err) {
            console.error('Search failed:', err);
            $searchResultsContainer.append($('<p>').text('Search failed. Please try again.'));
        } finally {
            $searchSpinner.removeClass('visible');
        }
    }

    function renderResults(results) {
        if (!results || results.length === 0) {
            $searchResultsContainer.append($('<p>').text('No results found.'));
            return;
        }

        const $ul = $('<ul>');
        results.forEach(result => {
            const $wordLi = $(wordTemplate);
            const $header = $wordLi.find('.collapsible-header');
            const $chevron = $wordLi.find('.chevron');
            const $wordLabel = $wordLi.find('.word-label');
            const $locationsUl = $wordLi.find('.document-locations');
            
            $wordLabel.text(result.word);

            result.locations.forEach(location => {
                const $locationLi = $(locationTemplate);
                $locationLi.find('.document-name').text(location.documentName);
                const $link = $locationLi.find('.document-link');
                $locationLi.find('.pages-list').text(location.pages.join(', '));
                
                $link.on('click', function(e) {
                    e.preventDefault();
                    if (window.showDocumentOverlay) {
                        window.showDocumentOverlay(location, result.word);
                    }
                });

                $locationsUl.append($locationLi);
            });

            $header.on('click', function() {
                $chevron.toggleClass('collapsed');
                $locationsUl.toggleClass('collapsed');
            });

            $ul.append($wordLi);
        });

        $searchResultsContainer.append($ul);
    }

    function onSearchTriggered() {
        clearTimeout(searchTimeout);
        const query = $searchInput.val();
        if (query.includes(' ')) {
            return;
        }
        if (query.trim() === '') {
            $searchResultsContainer.empty();
            return;
        }
        performSearch(query);
    }

    $searchInput.on('keypress', function (e) {
        if (e.which === 13) { // Enter key
            e.preventDefault();
            onSearchTriggered();
        }
    });

    $searchIcon.on('click', function () {
        onSearchTriggered();
    });
});
