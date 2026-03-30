$(async () => {
    // Inject display styles
    if (!$('link[href="display.css"]').length) {
        $('head').append('<link rel="stylesheet" href="display.css">');
    }

    // Inject overlay container from display.html
    const overlayFragment = await $.get('display.html');
    $('body').append(overlayFragment);

    const $overlay = $('#document-overlay');
    const $overlayObject = $('#overlay-object');
    const $overlayClose = $('#overlay-close');
    const $overlayBackground = $('.overlay-background');

    window.showDocumentOverlay = function(item) {
        $overlay.removeClass('hidden');
        $('body').css('overflow', 'hidden'); // Prevent scroll
    };

    function hideOverlay() {
        $overlay.addClass('hidden');
        $overlayObject.attr('data', '');
        $('body').css('overflow', ''); // Restore scroll
    }

    $overlayClose.on('click', (e) => {
        e.preventDefault();
        hideOverlay();
    });

    $overlayBackground.on('click', () => {
        hideOverlay();
    });
});
