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
    /// Zone raw file format: [4-byte ptr] [4-byte len (BE)] [4-byte ptr] [name\0] [data]
    /// </summary>
    public void StripHeaderIfPresent()
    {
        if (Data.Length < 16)
            return;

        // Method 1: Try to validate using the embedded length field
        // Zone format: FF FF FF FF [4-byte size BE] FF FF FF FF [name\0] [data]
        if (TryStripUsingLengthField())
            return;

        // Method 2: Check for non-ASCII start followed by null-terminated path string
        if (TryStripUsingNullTerminator())
            return;
    }

    /// <summary>
    /// Attempts to strip header by validating the embedded length field.
    /// </summary>
    private bool TryStripUsingLengthField()
    {
        // Check for FF FF FF FF marker at start (or similar non-ASCII bytes)
        bool hasMarkerStart = (Data[0] == 0xFF && Data[1] == 0xFF && Data[2] == 0xFF && Data[3] == 0xFF) ||
                              (Data[0] < 0x20 && Data[1] < 0x20 && Data[2] < 0x20 && Data[3] < 0x20);

        if (!hasMarkerStart)
            return false;

        // Read embedded length at bytes 4-7 (big-endian)
        int embeddedLength = (Data[4] << 24) | (Data[5] << 16) | (Data[6] << 8) | Data[7];

        // Sanity check: length should be reasonable (less than total file size minus header)
        if (embeddedLength <= 0 || embeddedLength > Data.Length - 12)
            return false;

        // Find null terminator starting at byte 12 (after the two 4-byte markers and length)
        int nameStart = 12;
        int nullPos = FindNullTerminator(nameStart, Math.Min(512, Data.Length));

        if (nullPos < 0)
            return false;

        // Validate: the name should look like a valid path (contains / or \)
        string embeddedName = Encoding.ASCII.GetString(Data, nameStart, nullPos - nameStart);
        if (!embeddedName.Contains('/') && !embeddedName.Contains('\\'))
            return false;

        // Content starts after the null terminator
        int contentStart = nullPos + 1;

        // Validate: embedded length should match remaining data (with some tolerance for trailing null)
        int actualRemainingLength = Data.Length - contentStart;
        if (Math.Abs(embeddedLength - actualRemainingLength) > 1)
            return false;

        // Strip the header
        ExtractContent(contentStart);
        return true;
    }

    /// <summary>
    /// Attempts to strip header by finding a null-terminated path string.
    /// </summary>
    private bool TryStripUsingNullTerminator()
    {
        // First bytes should be non-printable ASCII
        bool hasNonAsciiStart = Data[0] < 0x20 || Data[0] > 0x7E ||
                                Data[1] < 0x20 || Data[1] > 0x7E ||
                                Data[2] < 0x20 || Data[2] > 0x7E ||
                                Data[3] < 0x20 || Data[3] > 0x7E;

        if (!hasNonAsciiStart)
            return false;

        // Search for a null terminator that ends what looks like a file path
        int searchLimit = Math.Min(512, Data.Length);

        for (int nullPos = 8; nullPos < searchLimit; nullPos++)
        {
            if (Data[nullPos] != 0x00)
                continue;

            // Check if bytes before null look like a path (printable ASCII with / or extension)
            int pathStart = FindPathStart(nullPos);
            if (pathStart < 0)
                continue;

            string potentialPath = Encoding.ASCII.GetString(Data, pathStart, nullPos - pathStart);

            // Validate it looks like a file path (has extension or path separator)
            if (!LooksLikeFilePath(potentialPath))
                continue;

            // Content starts after the null terminator
            int contentStart = nullPos + 1;
            if (contentStart >= Data.Length)
                continue;

            // Verify content starts with something reasonable (printable or common bytes)
            if (!IsValidContentStart(contentStart))
                continue;

            // Strip the header
            ExtractContent(contentStart);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds where the path string likely starts by scanning backwards from null terminator.
    /// </summary>
    private int FindPathStart(int nullPos)
    {
        // Scan backwards to find where printable ASCII starts
        for (int i = nullPos - 1; i >= 8; i--)
        {
            byte b = Data[i];
            // If we hit a non-printable byte, the path starts after it
            if (b < 0x20 || b > 0x7E)
                return i + 1;
        }
        return 8; // Default to byte 8 (after potential header)
    }

    /// <summary>
    /// Checks if a string looks like a valid file path.
    /// </summary>
    private static bool LooksLikeFilePath(string str)
    {
        if (string.IsNullOrEmpty(str) || str.Length < 3)
            return false;

        // Should have an extension or path separator
        return str.Contains('/') || str.Contains('\\') ||
               str.EndsWith(".gsc") || str.EndsWith(".csc") ||
               str.EndsWith(".cfg") || str.EndsWith(".str") ||
               str.EndsWith(".csv") || str.EndsWith(".txt") ||
               str.EndsWith(".menu") || str.EndsWith(".vision");
    }

    /// <summary>
    /// Checks if the content at the given position looks valid.
    /// </summary>
    private bool IsValidContentStart(int position)
    {
        if (position >= Data.Length)
            return false;

        byte first = Data[position];

        // Common script/text file starts
        return first == '/' ||  // Comment
               first == '#' ||  // Preprocessor
               first == '\r' || // Newline
               first == '\n' ||
               first == ' ' ||  // Whitespace
               first == '\t' ||
               (first >= 'a' && first <= 'z') ||  // Lowercase letter
               (first >= 'A' && first <= 'Z') ||  // Uppercase letter
               first == '{' ||  // Block start
               first == '[';    // Array/section
    }

    /// <summary>
    /// Finds the first null byte starting from the given position.
    /// </summary>
    private int FindNullTerminator(int start, int limit)
    {
        for (int i = start; i < limit && i < Data.Length; i++)
        {
            if (Data[i] == 0x00)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Extracts content starting from the given position.
    /// </summary>
    private void ExtractContent(int contentStart)
    {
        int contentLength = Data.Length - contentStart;
        byte[] newData = new byte[contentLength];
        Array.Copy(Data, contentStart, newData, 0, contentLength);
        Data = newData;
    }

    public override string ToString() => $"{Name} ({Data.Length} bytes)";
}
