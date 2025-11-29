namespace FastFileCompiler.Models;

/// <summary>
/// Represents a localized string entry.
/// </summary>
public class LocalizedEntry
{
    /// <summary>
    /// The reference key for the localized string.
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// The localized string value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    public LocalizedEntry() { }

    public LocalizedEntry(string reference, string value)
    {
        Reference = reference;
        Value = value;
    }

    public override string ToString() => $"{Reference}: {Value}";
}
