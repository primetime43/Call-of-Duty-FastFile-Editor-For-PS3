using System.Text;

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

    /// <summary>
    /// Detects and strips the zone raw file header if present.
    /// Raw files exported with headers have: [4-byte ptr] [4-byte len] [name\0] [data]
    /// </summary>
    public void StripHeaderIfPresent()
    {
        if (Data.Length < 16)
            return;

        // Check if first bytes look like a header (non-printable bytes followed by path)
        // Header format: 4 bytes (ptr) + 4 bytes (len) + null-terminated name + data

        // First 4 bytes should be non-ASCII (pointer placeholder like 0x01010101 or 0xFFFFFFFF)
        bool hasNonAsciiStart = Data[0] < 0x20 || Data[0] > 0x7E ||
                                Data[1] < 0x20 || Data[1] > 0x7E ||
                                Data[2] < 0x20 || Data[2] > 0x7E ||
                                Data[3] < 0x20 || Data[3] > 0x7E;

        if (!hasNonAsciiStart)
            return;

        // Look for a path pattern (e.g., "maps/", "common_scripts/", etc.) within first 512 bytes
        int searchLimit = Math.Min(512, Data.Length);
        int pathStart = -1;

        // Common path prefixes in CoD raw files
        string[] pathPrefixes = { "maps/", "common_scripts/", "animscripts/", "clientscripts/", "zzzz/" };

        for (int i = 8; i < searchLimit - 5; i++)
        {
            foreach (var prefix in pathPrefixes)
            {
                if (MatchesAt(Data, i, prefix))
                {
                    pathStart = i;
                    break;
                }
            }
            if (pathStart >= 0)
                break;
        }

        if (pathStart < 0)
            return;

        // Find the null terminator after the path/name
        int nullPos = -1;
        for (int i = pathStart; i < searchLimit; i++)
        {
            if (Data[i] == 0x00)
            {
                nullPos = i;
                break;
            }
        }

        if (nullPos < 0)
            return;

        // The actual content starts after the null terminator
        int contentStart = nullPos + 1;

        // Verify the content looks like script/text (starts with printable chars or common patterns)
        if (contentStart >= Data.Length)
            return;

        // Extract just the content
        int contentLength = Data.Length - contentStart;
        byte[] newData = new byte[contentLength];
        Array.Copy(Data, contentStart, newData, 0, contentLength);
        Data = newData;
    }

    /// <summary>
    /// Checks if the byte array matches the given string at the specified position.
    /// </summary>
    private static bool MatchesAt(byte[] data, int position, string pattern)
    {
        if (position + pattern.Length > data.Length)
            return false;

        for (int i = 0; i < pattern.Length; i++)
        {
            if (data[position + i] != (byte)pattern[i])
                return false;
        }
        return true;
    }

    public override string ToString() => $"{Name} ({Data.Length} bytes)";
}
