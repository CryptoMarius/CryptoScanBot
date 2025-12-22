using Avalonia.Controls;
using Avalonia.Threading;
using Xilium.CefGlue.Avalonia;
using System;
using CryptoScanner.Browser.Helpers;

namespace CryptoScanner.Browser.Views
{
    public partial class BrowserView : UserControl
    {
        private AvaloniaCefBrowser? _browser;
        private bool _isInitializing;
        private string? _pendingUrl;

        public BrowserView()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("BrowserView created (browser NOT initialized yet)");
        }

        /// <summary>
        /// Navigate to URL - creates browser on first call
        /// </summary>
        public void Navigate(string url)
        {
            System.Diagnostics.Debug.WriteLine($"Navigate called: {url}");

            // If browser exists, just navigate
            if (_browser != null)
            {
                System.Diagnostics.Debug.WriteLine("Browser exists, navigating directly");
                _browser.Address = url;
                return;
            }

            // Browser doesn't exist yet - create it
            System.Diagnostics.Debug.WriteLine("Browser doesn't exist, initializing now...");
            _pendingUrl = url;

            if (!_isInitializing)
            {
                _isInitializing = true;
                InitializeBrowserLazy();
            }
        }

        private void InitializeBrowserLazy()
        {
            System.Diagnostics.Debug.WriteLine("InitializeBrowserLazy starting...");

            // Force tab to become visible first
            // Find parent TabControl and switch to this tab
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent is TabControl tabControl)
                {
                    // Find which tab contains this view
                    for (int i = 0; i < tabControl.Items.Count; i++)
                    {
                        var tabItem = tabControl.Items[i] as TabItem;
                        if (tabItem?.Content == this || IsChildOf(tabItem?.Content, this))
                        {
                            System.Diagnostics.Debug.WriteLine($"Switching to tab {i} to make browser visible");
                            tabControl.SelectedIndex = i;
                            //var timer = new System.Timers.Timer(1000); // Was 100ms
                            break;
                        }
                    }
                    break;
                }
                parent = parent.Parent;
            }

            // Wait a bit for tab to become visible
            Dispatcher.UIThread.Post(() =>
            {
                CreateBrowser();
            }, DispatcherPriority.Background);
        }

        private static bool IsChildOf(object? potentialParent, Control child)
        {
            if (potentialParent == child) return true;
            if (potentialParent is Panel panel)
            {
                foreach (var c in panel.Children)
                {
                    if (c == child) return true;
                }
            }
            return false;
        }

        private void CreateBrowser()
        {
            System.Diagnostics.Debug.WriteLine("CreateBrowser starting...");

            try
            {
                var browserWrapper = this.FindControl<Decorator>("browserWrapper");
                if (browserWrapper == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: browserWrapper not found!");
                    _isInitializing = false;
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Creating AvaloniaCefBrowser instance...");

                _browser = new AvaloniaCefBrowser
                {
                    Address = _pendingUrl ?? "about:blank"
                };

                _browser.LoadEnd += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Browser loaded: {e.Frame.Url}");
                };

                _browser.LoadError += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Browser error: {e.ErrorText}");
                };

                browserWrapper.Child = _browser;

                System.Diagnostics.Debug.WriteLine($"Browser created successfully, navigating to: {_pendingUrl}");
                _isInitializing = false;
                _pendingUrl = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR creating browser: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                _isInitializing = false;
            }
        }
    }
}
