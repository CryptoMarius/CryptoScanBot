namespace CryptoScanner.UI.Models;

/// <summary>One entry of a <c>MenuButton</c> dropdown: the caption and what it does.</summary>
/// <param name="Label">Caption, as the Avalonia MenuItem header spells it.</param>
/// <param name="Invoke">What the entry does when it is picked.</param>
public record MenuAction(string Label, Action Invoke);
