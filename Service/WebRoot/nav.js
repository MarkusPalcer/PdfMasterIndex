$(async () => {
    // Inject navigation from nav.html
    try {
        const navFragment = await $.get('nav.html');
        $('body').prepend(navFragment);
    } catch (err) {
        console.error("Failed to load navigation:", err);
    }
});