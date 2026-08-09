// "Follow system" for the theme.
//
// Avalonia resolves that setting itself (ThemeVariant.Default), but a web view has to be asked:
// prefers-color-scheme reports what the operating system is set to. Without this the Blazor hosts
// fell back to dark for "Follow system", so a machine running the light theme never switched.
window.cryptoScannerTheme = {
    prefersDark: function () {
        return window.matchMedia('(prefers-color-scheme: dark)').matches;
    },

    // Report later changes as well, so switching the operating system theme repaints straight away.
    watch: function (dotNetRef) {
        const query = window.matchMedia('(prefers-color-scheme: dark)');
        query.addEventListener('change', function (event) {
            dotNetRef.invokeMethodAsync('OnSystemThemeChanged', event.matches);
        });
    }
};
