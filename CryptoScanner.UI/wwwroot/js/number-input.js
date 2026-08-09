// Min/max protection for <input type="number">.
//
// A browser only uses the min/max attributes for the spinner buttons and for form validation; typed
// text is accepted as-is. The settings dialog is not an HTML form, so nothing ever validated and a
// hand typed 9999 went straight into the settings — the Avalonia NumericUpDown does clamp.
//
// The listener runs on the CAPTURE phase, so it corrects the element before Blazor's delegated
// change handler (which listens on the bubble phase) reads event.target.value. That way the bound
// property receives the clamped number and the field shows what was actually stored.
(function () {
    function clampNumberInput(event) {
        const el = event.target;
        if (!el || el.tagName !== 'INPUT' || el.type !== 'number')
            return;

        // An empty field is left alone: the binding decides what an empty value means.
        if (el.value === '')
            return;

        const typed = parseFloat(el.value);
        const min = parseFloat(el.min);
        const max = parseFloat(el.max);

        if (isNaN(typed)) {
            el.value = isNaN(min) ? '0' : String(min);
            return;
        }

        let value = typed;
        if (!isNaN(min) && value < min)
            value = min;
        if (!isNaN(max) && value > max)
            value = max;

        if (value !== typed)
            el.value = String(value);
    }

    document.addEventListener('change', clampNumberInput, true);
})();
