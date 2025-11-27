namespace FastFileCompiler.Models;

/// <summary>
/// Represents a raw file asset to be compiled into a FastFile.
/// </summary>
public class RawFile
{
    /// <summary>
    /// The name/path of the raw file (e.g., "maps/mp/gametypes/_globallogic.gsc").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The raw file content as bytes.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Creates a new RawFile instance.
    /// </summary>
    public RawFile() { }

    /// <summary>
    /// Creates a new RawFile instance with the specified name and data.
    /// </summary>
    public RawFile(string name, byte[] data)
    {
        Name = name;
        Data = data;
    }

    /// <summary>
    /// Creates a RawFile from a file on disk.
    /// </summary>
    /// <param name="filePath">The path to the file on disk.</param>
    /// <param name="assetName">The asset name to use in the FastFile. If null, uses the filename.</param>
    public static RawFile FromFile(string filePath, string? assetName = null)
    {
        return new RawFile
        {
            Name = assetName ?? Path.GetFileName(filePath),
            Data = File.ReadAllBytes(filePath)
        };
    }

    /// <summary>
    /// Creates a RawFile from a string content.
    /// </summary>
    /// <param name="name">The asset name.</param>
    /// <param name="content">The string content.</param>
    public static RawFile FromString(string name, string content)
    {
        return new RawFile
        {
            Name = name,
            Data = System.Text.Encoding.ASCII.GetBytes(content)
        };
    }

    public override string ToString() => $"{Name} ({Data.Length} bytes)";
}
