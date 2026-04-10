$(async () => {
    const $newPath = $('#new-path');
    const $newName = $('#new-name');
    const $newTagsContainer = $('#new-tags-container');
    const $newTagEntryInput = $('#new-tag-entry');
    const $saveNewButton = $('#save-new-scan-folder');
    const $newEntryInputs = $('#new-scan-folder input, #new-scan-folder button, #new-scan-folder span');
    const $sourceFolderList = $('#source-folders-list');
    let rowTemplate;
    let scanPathConstraints = null;
    let tagConstraints = null;

    async function loadConstraints() {
        try {
            const modelNames = await $.getJSON('/api/v1/constraints');
            const scanPathModelName = modelNames.find(name => name.endsWith('.ScanPath'));
            if (scanPathModelName) {
                const constraints = await $.getJSON(`/api/v1/constraints/${encodeURIComponent(scanPathModelName)}`);
                scanPathConstraints = constraints.maxStringLengths;
            }
            const tagModelName = modelNames.find(name => name.endsWith('.Tag'));
            if (tagModelName) {
                const constraints = await $.getJSON(`/api/v1/constraints/${encodeURIComponent(tagModelName)}`);
                tagConstraints = constraints.maxStringLengths;
            }
        } catch (err) {
            console.error('Failed to load constraints:', err);
        }
    }

    function validateInput($input, propertyName, constraints = scanPathConstraints) {
        if (!constraints || !constraints[propertyName]) {
            $input.removeClass('invalid').attr('title', '');
            return true;
        }

        const maxLength = constraints[propertyName];
        const val = $input.val() || '';
        if (val.length > maxLength) {
            $input.addClass('invalid').attr('title', `Maximum length is ${maxLength} characters (current: ${val.length})`);
            return false;
        } else {
            $input.removeClass('invalid').attr('title', '');
            return true;
        }
    }

    async function loadRowTemplate() {
        rowTemplate = await $.get('settings-row.html');
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

    function updateSaveButtonState() {
        const isPathValid = validateInput($newPath, 'Path');
        const isNameValid = validateInput($newName, 'Name');
        const hasPath = !!$newPath.val().trim();

        if (hasPath && isPathValid && isNameValid) {
            $saveNewButton.removeClass('disabled');
        } else {
            $saveNewButton.addClass('disabled');
        }
    }

    // Initial state for new entry
    updateSaveButtonState();

    let prevNewPath = $newPath.val();

        $newPath.on('input', () => {
            const currentPathValue = $newPath.val();
            const currentNameValue = $newName.val();

            if (!currentNameValue.trim() || currentNameValue === prevNewPath) {
                $newName.val(currentPathValue);
            }

            prevNewPath = currentPathValue;
            updateSaveButtonState();
        });

        $newName.on('input', updateSaveButtonState);

    function createTagElement(tagText, options) {
        const { isInitial = false, onAdd, onDelete, rowId } = options;
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

    // Tag management for the "new scan path" row
    $newTagEntryInput.on('input', () => {
        validateInput($newTagEntryInput, 'Value', tagConstraints);
    });

    $newTagEntryInput.on('keydown', (e) => {
        if (e.key === ',' || e.key === ';') {
            e.preventDefault();
        }
        if (e.key === 'Enter') {
            if (!validateInput($newTagEntryInput, 'Value', tagConstraints)) {
                return;
            }
            let tagText = $newTagEntryInput.val().trim();
            if (tagText.endsWith(',') || tagText.endsWith(';')) {
                tagText = tagText.slice(0, -1).trim();
            }
            if (tagText) {
                // Check if tag already exists in the container
                const exists = $newTagsContainer.find('.name-tag').toArray()
                    .some(span => {
                        const $span = $(span);
                        const $cloned = $span.clone();
                        $cloned.find('.name-tag-symbol').remove();
                        return $cloned.text().trim() === tagText;
                    });
                
                if (!exists) {
                    $newTagsContainer.append(createTagElement(tagText, { isInitial: true }));
                }
                $newTagEntryInput.val('').removeClass('invalid').attr('title', '');
            }
        }
    });

    // Fetch and populate existing scan folders
    async function loadScanPaths() {
        try {
            if (!rowTemplate) {
                await loadRowTemplate();
            }
            const scanPaths = await $.getJSON('/api/v1/scanpaths');
            // Remove existing rows except template and new row
            $sourceFolderList.find('tr:not(#new-scan-folder)').remove();

            scanPaths.forEach(path => {
                const $row = $(rowTemplate);
                const id = path.id;
                $row.attr('data-id', id);
                $row.attr('data-initial-path', path.path);
                $row.attr('data-initial-name', path.name);
                const tagsString = path.tags ? path.tags.join(', ') : '';
                $row.attr('data-initial-tags', tagsString);

                const $pathInput = $row.find('.path').val(path.path);
                const $nameInput = $row.find('.name').val(path.name);
                const $tagsElement = $row.find('.tags');

                const tagOptions = {
                    onAdd: async (tagText) => {
                        await $.ajax({
                            url: `/api/v1/scanpaths/${id}/tags/${encodeURIComponent(tagText)}`,
                            type: 'POST'
                        });
                        // Update data-initial-tags
                        const currentTags = ($row.attr('data-initial-tags') || '').split(', ').filter(t => t.length > 0);
                        if (!currentTags.includes(tagText)) {
                            currentTags.push(tagText);
                            $row.attr('data-initial-tags', currentTags.join(', '));
                        }
                    },
                    onDelete: async (tagText) => {
                        await $.ajax({
                            url: `/api/v1/scanpaths/${id}/tags/${encodeURIComponent(tagText)}`,
                            type: 'DELETE'
                        });
                        // Update data-initial-tags
                        const currentTags = ($row.attr('data-initial-tags') || '').split(', ').filter(t => t.length > 0);
                        const updatedTags = currentTags.filter(t => t !== tagText);
                        $row.attr('data-initial-tags', updatedTags.join(', '));
                    }
                };

                if (path.tags && path.tags.length > 0) {
                    path.tags.forEach(tag => {
                        $tagsElement.append(createTagElement(tag, { ...tagOptions, isInitial: true }));
                    });
                }

                const $newTagInput = $row.find('.new-tag-input');
                $newTagInput.on('input', () => {
                    validateInput($newTagInput, 'Value', tagConstraints);
                });

                $newTagInput.on('keydown', (e) => {
                    if (e.key === ',' || e.key === ';') {
                        e.preventDefault();
                    }
                    if (e.key === 'Enter') {
                        if (!validateInput($newTagInput, 'Value', tagConstraints)) {
                            return;
                        }
                        let tagText = $newTagInput.val().trim();
                        if (tagText.endsWith(',') || tagText.endsWith(';')) {
                            tagText = tagText.slice(0, -1).trim();
                        }
                        if (tagText) {
                            $tagsElement.append(createTagElement(tagText, tagOptions));
                            $newTagInput.val('').removeClass('invalid').attr('title', '');
                        }
                    }
                });

                const $saveBtn = $row.find('.save-btn').addClass('disabled');
                const $revertBtn = $row.find('.revert-btn').addClass('disabled');

                let prevPath = $pathInput.val();

                $pathInput.on('input', () => {
                    const currentPathValue = $pathInput.val();
                    const currentNameValue = $nameInput.val();

                    if (!currentNameValue.trim() || currentNameValue === prevPath) {
                        $nameInput.val(currentPathValue);
                    }

                    prevPath = currentPathValue;
                });

                function checkChanges() {
                    const isPathValid = validateInput($pathInput, 'Path');
                    const isNameValid = validateInput($nameInput, 'Name');

                    const isChanged = $pathInput.val() !== $row.attr('data-initial-path') ||
                                      $nameInput.val() !== $row.attr('data-initial-name');

                    if (isChanged && $pathInput.val().trim() && isPathValid && isNameValid) {
                        $saveBtn.removeClass('disabled');
                        $revertBtn.removeClass('disabled');
                    } else {
                        $saveBtn.addClass('disabled');
                        if (isChanged) {
                            $revertBtn.removeClass('disabled');
                        } else {
                            $revertBtn.addClass('disabled');
                        }
                    }
                }

                $row.find('input').on('input', checkChanges);

                $revertBtn.on('click', () => {
                    if ($revertBtn.hasClass('disabled')) return;
                    $pathInput.val($row.attr('data-initial-path'));
                    $nameInput.val($row.attr('data-initial-name'));
                    prevPath = $pathInput.val();
                    checkChanges();
                });

                $saveBtn.on('click', async () => {
                    if ($saveBtn.hasClass('disabled')) return;

                    const id = $row.attr('data-id');
                    const updatedData = {
                        path: $pathInput.val().trim(),
                        name: $nameInput.val().trim()
                    };

                    $row.find('input, button, span').addClass('disabled');

                    try {
                        await $.ajax({
                            url: `/api/v1/scanpaths/${id}`,
                            type: 'PUT',
                            contentType: 'application/json',
                            data: JSON.stringify(updatedData)
                        });

                        // Update initial values
                        $row.attr('data-initial-path', updatedData.path);
                        $row.attr('data-initial-name', updatedData.name);
                        prevPath = updatedData.path;
                        checkChanges();
                    } catch (err) {
                        const errorText = getErrorMessage(err);
                        alert(`Failed to update scan path: ${errorText}`);
                    } finally {
                        $row.find('input, button, span').removeClass('disabled');
                        checkChanges();
                    }
                });

                $row.find('.delete-btn').on('click', async () => {
                    if (!confirm('Are you sure you want to delete this scan path?')) return;
                    const id = $row.attr('data-id');
                    const $spinner = $row.find('.spinner');
                    const $controls = $row.find('input, .action-icon, .name-tag');

                    $row.addClass('deleting');
                    $spinner.addClass('visible');
                    $controls.addClass('disabled');

                    try {
                        await $.ajax({
                            url: `/api/v1/scanpaths/${id}`,
                            type: 'DELETE'
                        });
                        $row.remove();
                    } catch (err) {
                        const errorText = getErrorMessage(err);
                        alert(`Failed to delete scan path: ${errorText}`);
                        $row.removeClass('deleting');
                        $spinner.removeClass('visible');
                        $controls.removeClass('disabled');
                    }
                });

                // Prepend to list (before the 'new' row)
                $sourceFolderList.find('#new-scan-folder').before($row);
            });
        } catch (err) {
            console.error('Failed to load scan paths:', err);
        }
    }

    // Load initial data
    await loadConstraints();
    await loadScanPaths();

    $saveNewButton.on('click', async () => {
        if ($saveNewButton.hasClass('disabled')) return;

        const path = $newPath.val().trim();
        const name = $newName.val().trim();
        const tagsArray = $newTagsContainer.find('.name-tag').toArray()
            .map(span => {
                const $span = $(span);
                const $cloned = $span.clone();
                $cloned.find('.name-tag-symbol').remove();
                return $cloned.text().trim();
            });

        if (!path) {
            alert('Mounted folder (Path) is required.');
            return;
        }

        const data = {
            path: path,
            name: name || path,
            tags: tagsArray
        };

        $saveNewButton.addClass('disabled');
        $newEntryInputs.prop('disabled', true).addClass('disabled');

        try {
            await $.ajax({
                url: '/api/v1/scanpaths',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(data)
            });

            // Clear inputs and reload list
            $newPath.val('');
            $newName.val('');
            $newTagsContainer.empty();
            $newTagEntryInput.val('');
            prevNewPath = '';
            updateSaveButtonState();
            await loadScanPaths();
        } catch (err) {
            const errorText = getErrorMessage(err);
            alert(`Failed to create scan path: ${errorText}`);
        } finally {
            $saveNewButton.removeClass('disabled');
            $newEntryInputs.prop('disabled', false).removeClass('disabled');
            updateSaveButtonState();
        }
    });
});
