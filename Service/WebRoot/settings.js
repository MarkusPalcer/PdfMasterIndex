$(async () => {
    const $newPath = $('#new-path');
    const $newName = $('#new-name');
    const $saveNewButton = $('#save-new-scan-folder');
    const $newEntryInputs = $('#new-scan-folder input, #new-scan-folder button, #new-scan-folder span');
    const $sourceFolderList = $('#source-folders-list');
    let rowTemplate;

    async function loadRowTemplate() {
        rowTemplate = await $.get('settings-row.html');
    }

    function updateSaveButtonState() {
        if ($newPath.val().trim()) {
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
                $row.attr('data-id', path.id);
                $row.attr('data-initial-path', path.path);
                $row.attr('data-initial-name', path.name);

                const $pathInput = $row.find('.path').val(path.path);
                const $nameInput = $row.find('.name').val(path.name);

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
                    const isChanged = $pathInput.val() !== $row.attr('data-initial-path') ||
                                      $nameInput.val() !== $row.attr('data-initial-name');

                    if (isChanged && $pathInput.val().trim()) {
                        $saveBtn.removeClass('disabled');
                        $revertBtn.removeClass('disabled');
                    } else {
                        $saveBtn.addClass('disabled');
                        $revertBtn.addClass('disabled');
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
                        const errorText = err.responseText || `Error ${err.status}`;
                        alert(`Failed to update scan path: ${errorText}`);
                    } finally {
                        $row.find('input, button, span').removeClass('disabled');
                        checkChanges();
                    }
                });

                $row.find('.delete-btn').on('click', async () => {
                    if (!confirm('Are you sure you want to delete this scan path?')) return;
                    const id = $row.attr('data-id');
                    try {
                        await $.ajax({
                            url: `/api/v1/scanpaths/${id}`,
                            type: 'DELETE'
                        });
                        $row.remove();
                    } catch (err) {
                        const errorText = err.responseText || `Error ${err.status}`;
                        alert(`Failed to delete scan path: ${errorText}`);
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
    await loadScanPaths();

    $saveNewButton.on('click', async () => {
        if ($saveNewButton.hasClass('disabled')) return;

        const path = $newPath.val().trim();
        const name = $newName.val().trim();

        if (!path) {
            alert('Mounted folder (Path) is required.');
            return;
        }

        const data = {
            path: path,
            name: name || path
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
            prevNewPath = '';
            updateSaveButtonState();
            await loadScanPaths();
        } catch (err) {
            const errorText = err.responseText || `Error ${err.status}`;
            alert(`Failed to create scan path: ${errorText}`);
        } finally {
            $saveNewButton.removeClass('disabled');
            $newEntryInputs.prop('disabled', false).removeClass('disabled');
            updateSaveButtonState();
        }
    });
});
