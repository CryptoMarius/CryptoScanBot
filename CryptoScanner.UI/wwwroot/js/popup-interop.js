// Keep a popup inside the window.
//
// A context menu is placed at the mouse position, so a right-click on the last row of a grid put
// the bottom half of the menu below the edge of the screen (behind the taskbar). Avalonia moves its
// menu up until it fits; this does the same, and the same for the right edge.
window.cryptoScannerPopup = {
    keepInViewport: function (element) {
        if (!element)
            return;

        const margin = 4;
        const rect = element.getBoundingClientRect();
        let left = rect.left;
        let top = rect.top;

        if (rect.bottom > window.innerHeight - margin)
            top = Math.max(margin, window.innerHeight - rect.height - margin);
        if (rect.right > window.innerWidth - margin)
            left = Math.max(margin, window.innerWidth - rect.width - margin);

        element.style.left = left + 'px';
        element.style.top = top + 'px';
    }
};
