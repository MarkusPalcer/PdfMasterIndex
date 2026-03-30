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
    const $overlayContent = $('.overlay-content');
    const $overlayPrev = $('#overlay-prev');
    const $overlayNext = $('#overlay-next');
    const $canvas = $('#pdf-canvas');
    const canvas = $canvas[0];
    const renderScale = 3.0; // Render at triple size for sharpness

    let currentScale = 1;
    let minScale = 1;
    let translateX = 0;
    let translateY = 0;
    let isDragging = false;
    let startX, startY;
    let lastPinchDistance = null;

    function applyTransform() {
        $canvas.css('transform', `translate(${translateX}px, ${translateY}px) scale(${currentScale})`);
    }

    function resetZoom(page) {
        const viewport = page.getViewport({ scale: 1 });
        const containerWidth = $overlayContent.width();
        const containerHeight = $overlayContent.height();
        
        const scaleX = containerWidth / (viewport.width * renderScale);
        const scaleY = containerHeight / (viewport.height * renderScale);
        minScale = Math.min(scaleX, scaleY);
        currentScale = minScale;
        translateX = 0;
        translateY = 0;
        applyTransform();
    }

    $overlayContent.on('wheel', (e) => {
        e.preventDefault();
        const delta = e.originalEvent.deltaY;
        const zoomStep = 1.1;
        const oldScale = currentScale;

        if (delta < 0) {
            currentScale *= zoomStep;
        } else {
            currentScale /= zoomStep;
        }

        currentScale = Math.max(minScale, Math.min(currentScale, 10));
        applyTransform();
    });

    $canvas.on('mousedown touchstart', (e) => {
        isDragging = true;
        const clientX = e.type === 'touchstart' ? e.originalEvent.touches[0].clientX : e.clientX;
        const clientY = e.type === 'touchstart' ? e.originalEvent.touches[0].clientY : e.clientY;
        startX = clientX - translateX;
        startY = clientY - translateY;
        
        if (e.type === 'touchstart' && e.originalEvent.touches.length === 2) {
            lastPinchDistance = Math.hypot(
                e.originalEvent.touches[0].clientX - e.originalEvent.touches[1].clientX,
                e.originalEvent.touches[0].clientY - e.originalEvent.touches[1].clientY
            );
        }
    }).on('click', (e) => {
        // Prevent overlay closing when clicking the canvas
        e.stopPropagation();
    });

    $(window).on('mousemove touchmove', (e) => {
        if (!isDragging) return;

        if (e.type === 'touchmove' && e.originalEvent.touches.length === 2) {
            const dist = Math.hypot(
                e.originalEvent.touches[0].clientX - e.originalEvent.touches[1].clientX,
                e.originalEvent.touches[0].clientY - e.originalEvent.touches[1].clientY
            );
            if (lastPinchDistance) {
                const zoomStep = dist / lastPinchDistance;
                currentScale = Math.max(minScale, Math.min(currentScale * zoomStep, 10));
                applyTransform();
            }
            lastPinchDistance = dist;
            return;
        }

        const clientX = e.type === 'touchmove' ? e.originalEvent.touches[0].clientX : e.clientX;
        const clientY = e.type === 'touchmove' ? e.originalEvent.touches[0].clientY : e.clientY;
        
        // Prevent default only when dragging to avoid page scroll on touch
        if (e.cancelable) e.preventDefault();
        
        translateX = clientX - startX;
        translateY = clientY - startY;
        applyTransform();
    });

    $(window).on('mouseup touchend', () => {
        isDragging = false;
        lastPinchDistance = null;
    });

    window.showDocumentOverlay = function(item) {
        pdfjsLib.getDocument(item.linkPath).promise.then(function (pdfDoc) {
            const $pageList = $('.overlay-pagelist');
            $pageList.empty();
            let currentPageNumber = item.pages[0];

            function renderPage(pageNum) {
                pdfDoc.getPage(pageNum).then(newPage => {
                    const viewport = newPage.getViewport({ scale: renderScale });
                    canvas.width = viewport.width;
                    canvas.height = viewport.height;
                    const ctx = canvas.getContext("2d");
                    const renderContext = {
                        canvasContext: ctx,
                        viewport: viewport,
                    };
                    newPage.render(renderContext);
                    
                    $pageList.find('li').removeClass('active');
                    $pageList.find('li').each(function() {
                        if (parseInt($(this).text()) === pageNum) {
                            $(this).addClass('active');
                        }
                    });
                    resetZoom(newPage);
                    currentPageNumber = pageNum;
                });
            }

            item.pages.forEach((pageNumber, index) => {
                const $li = $('<li>').text(pageNumber).on('click', () => {
                    renderPage(pageNumber);
                });
                
                if (index === 0) {
                    $li.addClass('active');
                }
                
                $pageList.append($li);
            });

            $overlayPrev.off('click').on('click', (e) => {
                e.stopPropagation();
                if (currentPageNumber > 1) {
                    renderPage(currentPageNumber - 1);
                }
            });

            $overlayNext.off('click').on('click', (e) => {
                e.stopPropagation();
                if (currentPageNumber < pdfDoc.numPages) {
                    renderPage(currentPageNumber + 1);
                }
            });

            renderPage(currentPageNumber);
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

    $overlayContent.on('click', (e) => {
        if (e.target === e.currentTarget) {
            hideOverlay();
        }
    });
});
