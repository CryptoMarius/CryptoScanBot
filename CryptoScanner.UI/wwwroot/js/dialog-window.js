// Move and resize a dialog the way the Avalonia configuration window can be moved: drag it by its
// title bar, pull the corner to make it bigger. The dark overlay stays where it is, so the dialog
// is still modal - it can only be slid aside to read the grids underneath, not clicked through.
//
// The stylesheet centres a dialog with translate(-50%, -50%). Dragging replaces that with plain
// left/top pixels: moving something that is still centred on itself fights with its own transform.
window.cryptoScannerDialog = {
    // The dialog that is open, or null. Only one is ever attached - the settings dialog is the only
    // window-like dialog and it cannot be opened twice.
    _active: null,
    _drag: null,
    _resize: null,

    _minWidth: 420,
    _minHeight: 240,
    _margin: 4,

    // Keep the whole dialog inside the window, the same rule the context menus follow.
    _clampPosition: function (dialog, left, top) {
        const margin = window.cryptoScannerDialog._margin;
        const rect = dialog.getBoundingClientRect();
        const maxLeft = Math.max(margin, window.innerWidth - rect.width - margin);
        const maxTop = Math.max(margin, window.innerHeight - rect.height - margin);
        return {
            left: Math.min(Math.max(margin, left), maxLeft),
            top: Math.min(Math.max(margin, top), maxTop)
        };
    },

    // Hand the position and size back so they can be stored.
    _reportBounds: function () {
        const a = window.cryptoScannerDialog._active;
        if (!a || !a.dotNetRef)
            return;

        // The rectangle as drawn, not the inline style: max-width/max-height in the stylesheet can
        // cap what was asked for, and storing a size that is never drawn would open the dialog
        // somewhere else the next time.
        const rect = a.dialog.getBoundingClientRect();
        a.dotNetRef.invokeMethodAsync('OnDialogBoundsChanged', rect.left, rect.top, rect.width, rect.height);
    },

    // A drag ends with the mouse somewhere else than where it went down, so the click that follows
    // is delivered to a shared parent instead of to the dialog. Swallow that one click, otherwise
    // it can reach the overlay behind the dialog - which cancels the settings and throws away every
    // edit made in this session.
    _swallowNextClick: function () {
        const swallow = function (ev) {
            ev.stopPropagation();
            ev.preventDefault();
        };
        document.addEventListener('click', swallow, { capture: true, once: true });
        // "once" removes it as soon as a click arrives. Nothing guarantees one ever does (releasing
        // outside the window), so disarm it after a moment as well.
        setTimeout(function () {
            document.removeEventListener('click', swallow, { capture: true });
        }, 250);
    },

    // x/y/width/height are the stored position and size; a width of zero means nothing was stored
    // yet, and the dialog opens centred at the size the stylesheet gives it.
    attach: function (dialog, header, grip, dotNetRef, x, y, width, height) {
        if (!dialog || !header)
            return;

        window.cryptoScannerDialog.detach();

        const restored = width > 0 && height > 0;

        // A size is only written once the user has pulled the corner. Until then nothing is set
        // here, so the dialog keeps sizing itself to the tab that is open, exactly as before.
        if (restored) {
            dialog.style.width = width + 'px';
            dialog.style.height = height + 'px';
        }

        // Turn the CSS centring into concrete pixels, otherwise the first drag jumps by half the
        // dialog. Measured before the transform is dropped - translate does not change the size.
        const rect = dialog.getBoundingClientRect();
        const spot = window.cryptoScannerDialog._clampPosition(dialog, restored ? x : rect.left, restored ? y : rect.top);
        dialog.style.transform = 'none';
        dialog.style.left = spot.left + 'px';
        dialog.style.top = spot.top + 'px';

        const state = { dialog: dialog, header: header, grip: grip, dotNetRef: dotNetRef };

        state.onHeaderDown = function (e) {
            if (e.button !== 0)
                return;
            // Never start a drag on something that was meant to be clicked.
            if (e.target.closest && e.target.closest('button, input, select, textarea, a'))
                return;

            e.preventDefault();
            const r = dialog.getBoundingClientRect();
            window.cryptoScannerDialog._drag = {
                offsetX: e.clientX - r.left,
                offsetY: e.clientY - r.top,
                moved: false
            };
            document.addEventListener('mousemove', window.cryptoScannerDialog._onDragMove);
            document.addEventListener('mouseup', window.cryptoScannerDialog._onDragUp);
            document.body.style.userSelect = 'none';
        };

        state.onGripDown = function (e) {
            if (e.button !== 0)
                return;

            e.preventDefault();
            e.stopPropagation();
            const r = dialog.getBoundingClientRect();
            window.cryptoScannerDialog._resize = {
                startX: e.clientX,
                startY: e.clientY,
                startWidth: r.width,
                startHeight: r.height,
                left: r.left,
                top: r.top
            };
            document.addEventListener('mousemove', window.cryptoScannerDialog._onResizeMove);
            document.addEventListener('mouseup', window.cryptoScannerDialog._onResizeUp);
            document.body.style.userSelect = 'none';
        };

        // The scanner window itself can be made smaller while the dialog is open; without this the
        // dialog would be left hanging outside it.
        state.onWindowResize = function () {
            const r = dialog.getBoundingClientRect();
            const s = window.cryptoScannerDialog._clampPosition(dialog, r.left, r.top);
            dialog.style.left = s.left + 'px';
            dialog.style.top = s.top + 'px';
        };

        header.addEventListener('mousedown', state.onHeaderDown);
        if (grip)
            grip.addEventListener('mousedown', state.onGripDown);
        window.addEventListener('resize', state.onWindowResize);

        window.cryptoScannerDialog._active = state;
    },

    detach: function () {
        const a = window.cryptoScannerDialog._active;
        if (!a)
            return;

        a.header.removeEventListener('mousedown', a.onHeaderDown);
        if (a.grip)
            a.grip.removeEventListener('mousedown', a.onGripDown);
        window.removeEventListener('resize', a.onWindowResize);

        window.cryptoScannerDialog._stopDrag();
        window.cryptoScannerDialog._stopResize();
        window.cryptoScannerDialog._active = null;
    },

    _stopDrag: function () {
        if (!window.cryptoScannerDialog._drag)
            return;

        document.removeEventListener('mousemove', window.cryptoScannerDialog._onDragMove);
        document.removeEventListener('mouseup', window.cryptoScannerDialog._onDragUp);
        document.body.style.userSelect = '';
        window.cryptoScannerDialog._drag = null;
    },

    _stopResize: function () {
        if (!window.cryptoScannerDialog._resize)
            return;

        document.removeEventListener('mousemove', window.cryptoScannerDialog._onResizeMove);
        document.removeEventListener('mouseup', window.cryptoScannerDialog._onResizeUp);
        document.body.style.userSelect = '';
        window.cryptoScannerDialog._resize = null;
    },

    _onDragMove: function (e) {
        const d = window.cryptoScannerDialog._drag;
        const a = window.cryptoScannerDialog._active;
        if (!d || !a)
            return;

        d.moved = true;
        const spot = window.cryptoScannerDialog._clampPosition(a.dialog, e.clientX - d.offsetX, e.clientY - d.offsetY);
        a.dialog.style.left = spot.left + 'px';
        a.dialog.style.top = spot.top + 'px';
    },

    _onDragUp: function () {
        const d = window.cryptoScannerDialog._drag;
        if (!d)
            return;

        const moved = d.moved;
        window.cryptoScannerDialog._stopDrag();
        if (moved) {
            window.cryptoScannerDialog._swallowNextClick();
            window.cryptoScannerDialog._reportBounds();
        }
    },

    _onResizeMove: function (e) {
        const r = window.cryptoScannerDialog._resize;
        const a = window.cryptoScannerDialog._active;
        if (!r || !a)
            return;

        const margin = window.cryptoScannerDialog._margin;
        let width = Math.max(window.cryptoScannerDialog._minWidth, r.startWidth + (e.clientX - r.startX));
        let height = Math.max(window.cryptoScannerDialog._minHeight, r.startHeight + (e.clientY - r.startY));

        // The dialog grows to the right and down, so the corner it hangs on decides how far it may
        // grow before it leaves the window.
        width = Math.min(width, window.innerWidth - r.left - margin);
        height = Math.min(height, window.innerHeight - r.top - margin);

        a.dialog.style.width = width + 'px';
        a.dialog.style.height = height + 'px';
    },

    _onResizeUp: function () {
        if (!window.cryptoScannerDialog._resize)
            return;

        window.cryptoScannerDialog._stopResize();
        window.cryptoScannerDialog._swallowNextClick();
        window.cryptoScannerDialog._reportBounds();
    }
};
