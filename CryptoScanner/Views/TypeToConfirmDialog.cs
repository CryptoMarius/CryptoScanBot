using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace CryptoScanner.Views;

/// <summary>
/// The second, deliberate step of a destructive command: the user has to type a given text (the
/// number of things that will be deleted) before OK does anything. Typing a number cannot be done on
/// autopilot the way a second Yes can. Same idea as "Delete all runs" in the emulator.
/// Built in code like the emulator's prompt, so it needs no axaml of its own.
/// </summary>
public static class TypeToConfirmDialog
{
    /// <summary>
    /// Returns true only when the user typed <paramref name="expected"/> exactly (surrounding spaces
    /// ignored) and pressed OK or Enter; Cancel, Escape or closing the window give false.
    /// </summary>
    public static async Task<bool> ShowAsync(Window owner, string title, string prompt, string expected)
    {
        string? typed = null;

        var textBox = new TextBox { MinWidth = 360, AcceptsReturn = false };
        var okButton = new Button { Content = "OK", MinWidth = 80, IsDefault = true };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 80, IsCancel = true };

        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new Border
            {
                BorderBrush = Avalonia.Media.Brushes.Gray,
                BorderThickness = new Thickness(1),
                Child = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 14,
                    Children =
                    {
                        new TextBlock { Text = prompt, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 420, FontSize = 13 },
                        textBox,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 10,
                            Children = { okButton, cancelButton },
                        },
                    },
                },
            },
        };

        okButton.Click += (_, _) =>
        {
            typed = textBox.Text ?? "";
            dialog.Close();
        };
        cancelButton.Click += (_, _) =>
        {
            typed = null;
            dialog.Close();
        };

        // Enter in the text box confirms, Escape cancels (the button flags are not reached from inside a TextBox)
        textBox.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                typed = textBox.Text ?? "";
                dialog.Close();
                args.Handled = true;
            }
            else if (args.Key == Key.Escape)
            {
                typed = null;
                dialog.Close();
                args.Handled = true;
            }
        };

        dialog.Opened += (_, _) => textBox.Focus();

        await dialog.ShowDialog(owner);
        return typed != null && typed.Trim() == expected;
    }
}
