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
        const canvas = document.getElementById("pdf-canvas");
        pdfjsLib.getDocument(item.linkPath).promise.then(function (pdfDoc) {
            pdfDoc.getPage(item.pages[0]).then(function (page) {
                const $pageList = $('.overlay-pagelist');
                $pageList.empty();
                
                function renderPage(pageNum, $li) {
                    pdfDoc.getPage(pageNum).then(newPage => {
                        const viewport = newPage.getViewport({ scale: 1 });
                        canvas.width = viewport.width;
                        canvas.height = viewport.height;
                        const ctx = canvas.getContext("2d");
                        const renderContext = {
                            canvasContext: ctx,
                            viewport: viewport,
                        };
                        newPage.render(renderContext);
                        
                        $pageList.find('li').removeClass('active');
                        $li.addClass('active');
                    });
                }

                item.pages.forEach((pageNumber, index) => {
                    const $li = $('<li>').text(pageNumber).on('click', () => {
                        renderPage(pageNumber, $li);
                    });
                    
                    if (index === 0) {
                        $li.addClass('active');
                    }
                    
                    $pageList.append($li);
                });
                
                const viewport = page.getViewport({ scale: 1 });
                canvas.width = viewport.width;
                canvas.height = viewport.height;

                const ctx = canvas.getContext("2d");
                const renderContext = {
                    canvasContext: ctx,
                    viewport: viewport,
                };

                page.render(renderContext);
            });
        }).catch(function (error) {
            console.log("Error loading PDF file:", error);
        });
        
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
