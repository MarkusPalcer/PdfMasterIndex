$(async () => {
    // Inject display styles
    if (!$('link[href="display.css"]').length) {
        $('head').append('<link rel="stylesheet" href="display.css">');
    }

    // Inject overlay container from display.html
    const overlayFragment = await $.get('display.html');
    $('body').append(overlayFragment);

    if (window.pdfjsLib) {
        window.pdfjsLib.GlobalWorkerOptions.workerSrc = 'lib/pdf.worker.min.js';
    }

    const $overlay = $('#document-overlay');
    const $overlayObject = $('#overlay-object');
    const $overlayClose = $('#overlay-close');
    const $overlayBackground = $('.overlay-background');
    const $overlayContent = $('.overlay-content');
    const $overlayPrev = $('#overlay-prev');
    const $overlayNext = $('#overlay-next');
    const $pageListWrapper = $('.overlay-pagelist-wrapper');
    const $pageList = $('.overlay-pagelist');
    const $panLeft = $('#pagelist-pan-left');
    const $panRight = $('#pagelist-pan-right');
    const $pdfLoadingSpinner = $('#pdf-loading-spinner');
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

    let listIsDragging = false;
    let listStartX;
    let listScrollLeft;

    $pageListWrapper.on('mousedown touchstart', (e) => {
        listIsDragging = true;
        $pageListWrapper.css('cursor', 'grabbing');
        const clientX = e.type === 'touchstart' ? e.originalEvent.touches[0].clientX : e.clientX;
        listStartX = clientX - $pageListWrapper.offset().left;
        listScrollLeft = $pageListWrapper.scrollLeft();
    });

    $(window).on('mousemove touchmove', (e) => {
        if (!listIsDragging) return;
        const clientX = e.type === 'touchmove' ? e.originalEvent.touches[0].clientX : e.clientX;
        const x = clientX - $pageListWrapper.offset().left;
        const walk = (x - listStartX) * 1.5;
        $pageListWrapper.scrollLeft(listScrollLeft - walk);
        updatePanButtons();
    });

    $(window).on('mouseup touchend', () => {
        if (listIsDragging) {
            listIsDragging = false;
            $pageListWrapper.css('cursor', '');
        }
    });

    function updatePanButtons() {
        const scrollLeft = $pageListWrapper.scrollLeft();
        const maxScroll = $pageListWrapper[0].scrollWidth - $pageListWrapper[0].clientWidth;

        if (scrollLeft <= 0) {
            $panLeft.addClass('hidden');
        } else {
            $panLeft.removeClass('hidden');
        }

        if (scrollLeft >= maxScroll - 1) { // -1 for subpixel rounding issues
            $panRight.addClass('hidden');
        } else {
            $panRight.removeClass('hidden');
        }
    }

    $pageListWrapper.on('scroll', updatePanButtons);

    $panLeft.on('click', () => {
        panList(-8);
    });

    $panRight.on('click', () => {
        panList(8);
    });

    function panList(direction) {
        const $items = $pageList.find('li');
        if ($items.length === 0) return;

        const itemWidth = $items.outerWidth(true);
        const scrollAmount = itemWidth * direction;
        $pageListWrapper.animate({
            scrollLeft: $pageListWrapper.scrollLeft() + scrollAmount
        }, 300, updatePanButtons);
    }

    function scrollActiveIntoView() {
        const $active = $pageList.find('li.active');
        if ($active.length) {
            const wrapperScrollLeft = $pageListWrapper.scrollLeft();
            const wrapperWidth = $pageListWrapper.width();
            const activeOffset = $active.position().left + wrapperScrollLeft;
            const activeWidth = $active.outerWidth(true);

            if (activeOffset < wrapperScrollLeft || (activeOffset + activeWidth) > (wrapperScrollLeft + wrapperWidth)) {
                $pageListWrapper.animate({
                    scrollLeft: activeOffset - (wrapperWidth / 2) + (activeWidth / 2)
                }, 300, updatePanButtons);
            }
        }
    }

    function highlightText(page, viewport, ctx, searchTerm) {
        page.getTextContent().then(textContent => {
            const items = textContent.items;
            const searchTermLower = searchTerm.toLowerCase();
            
            ctx.fillStyle = 'rgba(255, 255, 0, 0.4)'; // Semi-transparent yellow

            items.forEach(item => {
                const text = item.str.toLowerCase();
                let index = text.indexOf(searchTermLower);
                
                while (index !== -1) {
                    const tx = pdfjsLib.Util.transform(viewport.transform, item.transform);
                    
                    // We need to measure the text before the match to find the offset
                    const beforeMatch = item.str.substring(0, index);
                    const matchText = item.str.substring(index, index + searchTerm.length);
                    
                    // We can estimate the match position by calculating the ratio of the match position to the string length
                    // and multiplying it by the total item width.
                    const ratio = viewport.scale * item.width / item.str.length;
                    const offset = index * ratio;
                    const matchWidth = searchTerm.length * ratio;
                    const itemHeight = item.height * viewport.scale;

                    ctx.fillRect(tx[4] + offset, tx[5] - itemHeight, matchWidth, itemHeight);
                    
                    index = text.indexOf(searchTermLower, index + 1);
                }
            });
        });
    }

    window.showDocumentOverlay = function(item, searchTerm) {
        $pdfLoadingSpinner.addClass('visible');
        pdfjsLib.getDocument(item.linkPath).promise.then(function (pdfDoc) {
            const $pageList = $('.overlay-pagelist');
            $pageList.empty();
            let currentPageNumber = item.pages[0];

            function renderPage(pageNum) {
                $pdfLoadingSpinner.addClass('visible');
                pdfDoc.getPage(pageNum).then(newPage => {
                    const viewport = newPage.getViewport({ scale: renderScale });
                    canvas.width = viewport.width;
                    canvas.height = viewport.height;
                    const ctx = canvas.getContext("2d");
                    const renderContext = {
                        canvasContext: ctx,
                        viewport: viewport,
                    };
                    
                    const renderTask = newPage.render(renderContext);
                    renderTask.promise.then(() => {
                        if (searchTerm) {
                            highlightText(newPage, viewport, ctx, searchTerm);
                        }
                        $pdfLoadingSpinner.removeClass('visible');
                    }).catch(err => {
                        console.error("Error rendering page:", err);
                        $pdfLoadingSpinner.removeClass('visible');
                    });
                    
                    $pageList.find('li').removeClass('active');
                    $pageList.find('li').each(function() {
                        if (parseInt($(this).text()) === pageNum) {
                            $(this).addClass('active');
                        }
                    });
                    scrollActiveIntoView();
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

            setTimeout(() => {
                updatePanButtons();
                scrollActiveIntoView();
            }, 100);

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
            $pdfLoadingSpinner.removeClass('visible');
        });
        
        $overlay.removeClass('hidden');
        $('body').css('overflow', 'hidden'); // Prevent scroll
    };

    function hideOverlay() {
        $overlay.addClass('hidden');
        $overlayObject.attr('data', '');
        
        // Clear PDF canvas
        const ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        // Reset canvas size to 0 to prevent it from showing a huge empty space if it was previously zoomed
        canvas.width = 0;
        canvas.height = 0;
        
        // Clear page list
        $pageList.empty();
        
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
