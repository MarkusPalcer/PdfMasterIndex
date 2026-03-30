$(() => {
    const $searchInput = $('#search-input');
    const $searchIcon = $('.search-icon');
    const $searchError = $('#search-error');
    const $searchSpinner = $('#search-spinner');
    const $searchResultsContainer = $('#search-results-container');

    $searchInput.on('input', function () {
        const query = $(this).val();
        if (query.includes(' ')) {
            $searchInput.addClass('invalid-input');
            $searchError.show();
        } else {
            $searchInput.removeClass('invalid-input');
            $searchError.hide();
        }
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
            const results = await $.getJSON('/api/v1/search', { query: query });

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
                $locationLi.find('.document-link').attr('href', location.linkPath);
                $locationLi.find('.pages-list').text('Pages: ' + location.pages.join(', '));
                
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
