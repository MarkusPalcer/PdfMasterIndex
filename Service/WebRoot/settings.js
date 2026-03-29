$(async () => {
    const $newInternalPath = $('#new-internal-path');
    const $newExternalPath = $('#new-external-path');
    const $newName = $('#new-name');
    const $newSelectFolderButton = $('#new-select-folder');
    const $saveNewButton = $('#save-new-scan-folder');
    const $newEntryInputs = $('#new-scan-folder input, #new-scan-folder button');

    function updateSaveButtonState() {
        if ($newInternalPath.val().trim()) {
            $saveNewButton.removeClass('disabled');
        } else {
            $saveNewButton.addClass('disabled');
        }
    }

    // Initial state
    updateSaveButtonState();

    $newInternalPath.on('input', updateSaveButtonState);

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

            // Clear inputs on success
            $newInternalPath.val('');
            $newExternalPath.val('');
            $newName.val('');

            // Optional: Reload or update the list (not required by the issue, but good practice)
            location.reload(); 
        } catch (err) {
            const errorText = err.responseText || `Error ${err.status}`;
            alert(`Failed to create scan path: ${errorText}`);
            $saveNewButton.removeClass('disabled');
            $newEntryInputs.prop('disabled', false).removeClass('disabled');
        }
    });
});