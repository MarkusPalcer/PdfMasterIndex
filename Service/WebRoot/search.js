$(() => {
    const $searchInput = $('#search-input');
    const $searchError = $('#search-error');

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

    $searchInput.on('keypress', function (e) {
        if (e.which === 13) { // Enter key
            const query = $(this).val();
            if (query.includes(' ')) {
                e.preventDefault();
                return;
            }
            console.log('Search query:', query);
        }
    });
});
