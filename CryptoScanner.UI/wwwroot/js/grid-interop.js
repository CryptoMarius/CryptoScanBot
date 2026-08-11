window.GridInterop = {
    _activeResize: null,

    // Bring a grid row into view without yanking the whole list around: only scrolls when the row
    // actually sits outside the visible area, and then no further than needed.
    scrollRowIntoView: function (rowId) {
        var row = document.getElementById(rowId);
        if (!row) return;

        // Walk up to the element that actually scrolls
        var container = row.parentElement;
        while (container && container !== document.body) {
            var style = window.getComputedStyle(container);
            var scrolls = style.overflowY === 'auto' || style.overflowY === 'scroll';
            if (scrolls && container.scrollHeight > container.clientHeight)
                break;
            container = container.parentElement;
        }
        if (!container || container === document.body) return;

        // Measured through bounding rectangles instead of offsetTop: offsetTop is relative to the
        // nearest positioned ancestor, which is not necessarily this container. When those two
        // differ every row looked out of view, so each arrow key scrolled the list one row and the
        // selection appeared to stand still while the data slid underneath it.
        var rowRect = row.getBoundingClientRect();
        var boxRect = container.getBoundingClientRect();

        // The header is sticky, so it covers the top of the box
        var header = container.querySelector('thead');
        var headerHeight = header ? header.getBoundingClientRect().height : 0;

        var topEdge = boxRect.top + headerHeight;
        var bottomEdge = boxRect.bottom;

        if (rowRect.top < topEdge)
            container.scrollTop -= (topEdge - rowRect.top);
        else if (rowRect.bottom > bottomEdge)
            container.scrollTop += (rowRect.bottom - bottomEdge);
    },

    focusElement: function (element) {
        if (element && element.focus)
            element.focus();
    },

    initColumnResize: function (thElement, dotNetRef, columnName) {
        const handle = thElement.querySelector('.col-resize-handle');
        if (!handle) return;

        // A click on the gripper must never sort. Stopping mousedown is not enough: the browser
        // still fires a click afterwards, and that one bubbles to the <th> which carries the sort
        // handler. This covers the case where press and release both happen on the gripper.
        handle.addEventListener('click', function (e) {
            e.stopPropagation();
        });

        handle.addEventListener('mousedown', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const startX = e.clientX;
            const startWidth = thElement.offsetWidth;

            window.GridInterop._activeResize = {
                th: thElement,
                dotNetRef: dotNetRef,
                columnName: columnName,
                startX: startX,
                startWidth: startWidth,
                moved: false
            };

            document.addEventListener('mousemove', window.GridInterop._onResizeMove);
            document.addEventListener('mouseup', window.GridInterop._onResizeUp);
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
        });
    },

    _onResizeMove: function (e) {
        const r = window.GridInterop._activeResize;
        if (!r) return;

        const delta = e.clientX - r.startX;
        if (Math.abs(delta) > 2)
            r.moved = true;
        const newWidth = Math.max(30, r.startWidth + delta);
        r.th.style.width = newWidth + 'px';
        r.th.style.minWidth = newWidth + 'px';
        r.th.style.maxWidth = newWidth + 'px';
    },

    _onResizeUp: function (e) {
        const r = window.GridInterop._activeResize;
        if (!r) return;

        document.removeEventListener('mousemove', window.GridInterop._onResizeMove);
        document.removeEventListener('mouseup', window.GridInterop._onResizeUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        // After a real drag the mouse is usually released away from the gripper, so the click that
        // follows targets the <th> itself and the handler above never sees it. Swallow that one
        // click during the capture phase, before it can reach the sort handler.
        if (r.moved) {
            const swallow = function (ev) {
                ev.stopPropagation();
                ev.preventDefault();
            };
            document.addEventListener('click', swallow, { capture: true, once: true });
            // "once" removes it as soon as a click arrives. Nothing guarantees one ever does
            // (releasing outside the window), so disarm it after a moment as well. Not on the next
            // frame: the click is dispatched right after mouseup and a zero timeout is not reliably
            // ordered behind it, which would leave the sort firing again on some releases.
            setTimeout(function () {
                document.removeEventListener('click', swallow, { capture: true });
            }, 250);
        }

        const finalWidth = r.th.offsetWidth;
        r.dotNetRef.invokeMethodAsync('OnColumnResized', r.columnName, finalWidth);

        window.GridInterop._activeResize = null;
    },

    initAllResizeHandles: function (tableElement, dotNetRef) {
        if (!tableElement) return;
        const headers = tableElement.querySelectorAll('th[data-col]');
        headers.forEach(function (th) {
            const colName = th.getAttribute('data-col');
            window.GridInterop.initColumnResize(th, dotNetRef, colName);
        });
    },

    _activeSplitter: null,

    initSplitter: function (handleElement, sidebarElement, dotNetRef) {
        if (!handleElement || !sidebarElement) return;

        handleElement.addEventListener('mousedown', function (e) {
            e.preventDefault();
            window.GridInterop._activeSplitter = {
                sidebar: sidebarElement,
                dotNetRef: dotNetRef,
                startX: e.clientX,
                startWidth: sidebarElement.offsetWidth
            };
            document.addEventListener('mousemove', window.GridInterop._onSplitterMove);
            document.addEventListener('mouseup', window.GridInterop._onSplitterUp);
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
        });
    },

    _onSplitterMove: function (e) {
        const s = window.GridInterop._activeSplitter;
        if (!s) return;
        const delta = e.clientX - s.startX;
        const newWidth = Math.max(100, Math.min(800, s.startWidth + delta));
        s.sidebar.style.width = newWidth + 'px';
    },

    _onSplitterUp: function (e) {
        const s = window.GridInterop._activeSplitter;
        if (!s) return;
        document.removeEventListener('mousemove', window.GridInterop._onSplitterMove);
        document.removeEventListener('mouseup', window.GridInterop._onSplitterUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        const finalWidth = s.sidebar.offsetWidth;
        s.dotNetRef.invokeMethodAsync('OnSidebarResized', finalWidth);
        window.GridInterop._activeSplitter = null;
    }
};
