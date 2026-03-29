$(() => {
    const $searchInput = $('#search-input');
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
            const $wordLi = $('<li>').append($('<strong>').text(result.word));
            const $locationsUl = $('<ul>');

            result.locations.forEach(location => {
                const $locationLi = $('<li>');
                $locationLi.append(document.createTextNode(location.documentName));

                const $link = $('<a>')
                    .addClass('document-link')
                    .attr('href', 'file:///' + location.linkPath.replace(/\\/g, '/'))
                    .attr('title', 'Open document')
                    .html('&#128279;'); // Link icon (🔗)

                $locationLi.append($link);
                $locationLi.append($('<span>').addClass('pages-list').text('Pages: ' + location.pages.join(', ')));
                $locationsUl.append($locationLi);
            });

            $wordLi.append($locationsUl);
            $ul.append($wordLi);
        });

        $searchResultsContainer.append($ul);
    }

    $searchInput.on('keypress', function (e) {
        if (e.which === 13) { // Enter key
            const query = $(this).val();
            if (query.includes(' ')) {
                e.preventDefault();
                return;
            }
            if (query.trim() === '') {
                $searchResultsContainer.empty();
                return;
            }
            performSearch(query);
        }
    });
});
