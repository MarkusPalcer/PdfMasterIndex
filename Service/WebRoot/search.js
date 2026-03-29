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

    async function performSearch(query) {
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
            const $wordLi = $('<li>');
            const $header = $('<div>').addClass('collapsible-header');
            const $chevron = $('<span>').addClass('chevron collapsed').html('&#9660;'); // Downward arrow
            const $wordLabel = $('<strong>').text(result.word);
            
            $header.append($chevron).append($wordLabel);
            $wordLi.append($header);

            const $locationsUl = $('<ul>').addClass('collapsible-content collapsed');

            result.locations.forEach(location => {
                const $locationLi = $('<li>');
                $locationLi.append(document.createTextNode(location.documentName));

                const $link = $('<a>')
                    .addClass('document-link')
                    .attr('href', location.linkPath)
                    .attr('target', '_blank')
                    .attr('title', 'Open document')
                    .html('&#128279;'); // Link icon (🔗)

                $locationLi.append($link);
                $locationLi.append($('<span>').addClass('pages-list').text('Pages: ' + location.pages.join(', ')));
                $locationsUl.append($locationLi);
            });

            $wordLi.append($locationsUl);

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
