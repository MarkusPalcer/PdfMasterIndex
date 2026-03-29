$(async () => {
    const $newInternalPath = $('#new-internal-path');
    const $newExternalPath = $('#new-external-path');
    const $newName = $('#new-name');
    const $saveNewButton = $('#save-new-scan-folder');
    const $newEntryInputs = $('#new-scan-folder input, #new-scan-folder button, #new-scan-folder span');
    const $sourceFolderList = $('#source-folders-list');
    const $template = $('#scan-folder-template');

    function updateSaveButtonState() {
        if ($newInternalPath.val().trim()) {
            $saveNewButton.removeClass('disabled');
        } else {
            $saveNewButton.addClass('disabled');
        }
    }

    // Initial state for new entry
    updateSaveButtonState();

    let prevNewInternalPath = $newInternalPath.val();
    let prevNewExternalPath = $newExternalPath.val();

    $newInternalPath.on('input', () => {
        const currentInternalValue = $newInternalPath.val();
        const currentExternalValue = $newExternalPath.val();

        if (!currentExternalValue.trim() || currentExternalValue === prevNewInternalPath) {
            $newExternalPath.val(currentInternalValue);
            $newExternalPath.trigger('input');
        }

        prevNewInternalPath = currentInternalValue;
        updateSaveButtonState();
    });

    $newExternalPath.on('input', () => {
        const currentExternalValue = $newExternalPath.val();
        const currentNameValue = $newName.val();

        if (!currentNameValue.trim() || currentNameValue === prevNewExternalPath) {
            $newName.val(currentExternalValue);
        }

        prevNewExternalPath = currentExternalValue;
        updateSaveButtonState();
    });

    // Fetch and populate existing scan folders
    async function loadScanPaths() {
        try {
            const scanPaths = await $.getJSON('/api/v1/scanpaths');
            // Remove existing rows except template and new row
            $sourceFolderList.find('tr:not(#scan-folder-template, #new-scan-folder)').remove();

            scanPaths.forEach(path => {
                const $row = $template.clone().removeAttr('id').removeClass('hidden');
                $row.attr('data-id', path.id);
                $row.attr('data-initial-internal', path.internalPath);
                $row.attr('data-initial-external', path.externalPath);
                $row.attr('data-initial-name', path.name);

                const $internalInput = $row.find('.internal-path').val(path.internalPath);
                const $externalInput = $row.find('.external-path').val(path.externalPath);
                const $nameInput = $row.find('.name').val(path.name);

                const $saveBtn = $row.find('.save-btn').addClass('disabled');
                const $revertBtn = $row.find('.revert-btn').addClass('disabled');

                let prevInternalPath = $internalInput.val();
                let prevExternalPath = $externalInput.val();

                $internalInput.on('input', () => {
                    const currentInternalValue = $internalInput.val();
                    const currentExternalValue = $externalInput.val();

                    if (!currentExternalValue.trim() || currentExternalValue === prevInternalPath) {
                        $externalInput.val(currentInternalValue);
                        $externalInput.trigger('input');
                    }

                    prevInternalPath = currentInternalValue;
                });

                $externalInput.on('input', () => {
                    const currentExternalValue = $externalInput.val();
                    const currentNameValue = $nameInput.val();

                    if (!currentNameValue.trim() || currentNameValue === prevExternalPath) {
                        $nameInput.val(currentExternalValue);
                    }

                    prevExternalPath = currentExternalValue;
                });

                function checkChanges() {
                    const isChanged = $internalInput.val() !== $row.attr('data-initial-internal') ||
                                      $externalInput.val() !== $row.attr('data-initial-external') ||
                                      $nameInput.val() !== $row.attr('data-initial-name');

                    if (isChanged && $internalInput.val().trim()) {
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
                    $internalInput.val($row.attr('data-initial-internal'));
                    $externalInput.val($row.attr('data-initial-external'));
                    $nameInput.val($row.attr('data-initial-name'));
                    prevInternalPath = $internalInput.val();
                    prevExternalPath = $externalInput.val();
                    checkChanges();
                });

                $saveBtn.on('click', async () => {
                    if ($saveBtn.hasClass('disabled')) return;

                    const id = $row.attr('data-id');
                    const updatedData = {
                        internalPath: $internalInput.val().trim(),
                        externalPath: $externalInput.val().trim(),
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
                        $row.attr('data-initial-internal', updatedData.internalPath);
                        $row.attr('data-initial-external', updatedData.externalPath);
                        $row.attr('data-initial-name', updatedData.name);
                        prevInternalPath = updatedData.internalPath;
                        prevExternalPath = updatedData.externalPath;
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

        const internalPath = $newInternalPath.val().trim();
        const externalPath = $newExternalPath.val().trim();
        const name = $newName.val().trim();

        if (!internalPath) {
            alert('Mounted folder (Internal Path) is required.');
            return;
        }

        const data = {
            internalPath: internalPath,
            externalPath: externalPath || internalPath,
            name: name || externalPath || internalPath
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
            $newInternalPath.val('');
            $newExternalPath.val('');
            $newName.val('');
            prevNewInternalPath = '';
            prevNewExternalPath = '';
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
