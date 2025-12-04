using Call_of_Duty_FastFile_Editor.Models;
using System.Diagnostics;
using System.Text;

namespace Call_of_Duty_FastFile_Editor.ZoneParsers
{
    /// <summary>
    /// Parser for MaterialTechniqueSet assets in zone files.
    ///
    /// Structure (from codresearch.dev):
    /// struct MaterialTechniqueSet {
    ///     char *name;                              // 0x0 (4 bytes) - 0xFFFFFFFF if inline
    ///     MaterialWorldVertexFormat worldVertFormat; // 0x4 (1 byte, values 0x00-0x0B)
    ///     MaterialTechnique *techniques[34];       // 0x5 (136 bytes for WaW - 34 techniques)
    /// };
    /// Total header: 141 bytes for CoD4/WaW (34 techniques)
    ///
    /// After header comes:
    /// - The name string (null-terminated) if name pointer was 0xFFFFFFFF
    /// - Technique data for each non-null technique pointer
    /// </summary>
    public static class TechSetParser
    {
        // Technique counts by platform/game (from codresearch.dev)
        // PS3: CoD4=26, WaW=51, MW2=37
        // PC:  CoD4=34, WaW=59, MW2=48
        private const int TECHNIQUE_COUNT_PS3_WAW = 51;
        private const int TECHNIQUE_COUNT_PS3_COD4 = 26;
        private const int TECHNIQUE_COUNT_PS3_MW2 = 37;

        // Default to PS3 WaW for now
        private const int TECHNIQUE_COUNT = TECHNIQUE_COUNT_PS3_WAW;
        private const int HEADER_SIZE = 4 + 1 + (TECHNIQUE_COUNT * 4); // 209 bytes for PS3 WaW

        /// <summary>
        /// Technique type names for PS3 WaW (51 technique slots).
        /// Based on IW engine research - these are shader rendering passes.
        /// </summary>
        private static readonly string[] TechniqueTypeNames = new string[]
        {
            "depth_prepass",               // 0
            "build_shadowmap_depth",       // 1
            "build_shadowmap_color",       // 2
            "build_floatz",                // 3
            "unlit",                       // 4  - fullbright rendering
            "emissive",                    // 5
            "emissive_shadow",             // 6
            "lit",                         // 7  - basic lit
            "lit_sun",                     // 8  - lit with sun
            "lit_sun_shadow",              // 9  - lit with sun + shadows
            "lit_spot",                    // 10 - lit with spotlight
            "lit_spot_shadow",             // 11 - lit with spotlight + shadows
            "lit_omni",                    // 12 - lit with omni light
            "lit_omni_shadow",             // 13 - lit with omni + shadows
            "lit_instanced",               // 14 - instanced (grass/trees)
            "lit_instanced_sun",           // 15
            "lit_instanced_sun_shadow",    // 16
            "lit_instanced_spot",          // 17
            "lit_instanced_spot_shadow",   // 18
            "lit_instanced_omni",          // 19
            "lit_instanced_omni_shadow",   // 20
            "light_spot",                  // 21 - fx spot lights
            "light_omni",                  // 22 - fx omni lights
            "light_spot_shadow",           // 23
            "light_omni_shadow",           // 24
            "fakelight_normal",            // 25 - radiant fakelight
            "fakelight_view",              // 26 - radiant fakelight
            "sunlight_preview",            // 27
            "case_texture",                // 28 - radiant only
            "wireframe_solid",             // 29 - radiant only
            "wireframe_shaded",            // 30 - radiant only
            "debug_bumpmap",               // 31 - r_debugShader
            "debug_bumpmap_instanced",     // 32
            "shadowcookie_caster",         // 33
            "shadowcookie_receiver",       // 34
            "debug_texcoord",              // 35
            "debug_tangent",               // 36
            "debug_binormal",              // 37
            "debug_normal",                // 38
            "debug_thermal",               // 39
            "effect_distortion",           // 40
            "effect_lit",                  // 41
            "effect_unlit",                // 42
            "effect_falloff",              // 43
            "effect_zfeather",             // 44
            "effect_distortion_thermal",   // 45
            "effect_lit_thermal",          // 46
            "effect_unlit_thermal",        // 47
            "effect_falloff_thermal",      // 48
            "effect_zfeather_thermal",     // 49
            "effect_custom"                // 50
        };

        /// <summary>
        /// Parses a MaterialTechniqueSet asset from zone data at the exact offset.
        /// </summary>
        public static TechSetAsset? ParseTechSet(byte[] zoneData, int offset, bool isBigEndian = true)
        {
            Debug.WriteLine($"[TechSetParser] Parsing at offset 0x{offset:X}");

            if (offset + HEADER_SIZE > zoneData.Length)
            {
                Debug.WriteLine($"[TechSetParser] Not enough data for header at 0x{offset:X}");
                return null;
            }

            return TryParseTechSetAt(zoneData, offset, isBigEndian);
        }

        /// <summary>
        /// Searches for and parses the next TechSet using pattern matching.
        /// Searches from startOffset up to maxSearchBytes forward.
        /// </summary>
        public static TechSetAsset? FindNextTechSet(byte[] zoneData, int startOffset, int maxSearchBytes = 50000, bool isBigEndian = true)
        {
            Debug.WriteLine($"[TechSetParser] Pattern searching from 0x{startOffset:X}, max {maxSearchBytes} bytes (header size={HEADER_SIZE}, techniques={TECHNIQUE_COUNT})");

            int endOffset = Math.Min(startOffset + maxSearchBytes, zoneData.Length - HEADER_SIZE);
            int ffCount = 0; // Count how many 0xFFFFFFFF we find
            int detailedLogCount = 0; // Limit detailed logging

            for (int pos = startOffset; pos < endOffset; pos++)
            {
                // Quick check for 0xFFFFFFFF first
                uint val = ReadUInt32(zoneData, pos, isBigEndian);
                if (val == 0xFFFFFFFF)
                {
                    ffCount++;
                    byte worldVertFormat = pos + 4 < zoneData.Length ? zoneData[pos + 4] : (byte)0xFF;

                    // Log first few with detailed info
                    if (detailedLogCount < 20)
                    {
                        detailedLogCount++;
                        Debug.WriteLine($"[TechSetParser] Found 0xFFFFFFFF #{ffCount} at 0x{pos:X}, worldVertFormat byte=0x{worldVertFormat:X2}");

                        // If worldVertFormat is valid, log more details about what happens
                        if (worldVertFormat <= 0x0B)
                        {
                            // Check first few technique pointers to diagnose
                            int validPtrs = 0, inlinePtrs = 0;
                            for (int i = 0; i < Math.Min(10, TECHNIQUE_COUNT); i++)
                            {
                                uint techPtr = ReadUInt32(zoneData, pos + 5 + (i * 4), isBigEndian);
                                if (techPtr == 0x00000000) validPtrs++;
                                else if (techPtr == 0xFFFFFFFF) { validPtrs++; inlinePtrs++; }
                            }
                            Debug.WriteLine($"[TechSetParser]   First 10 tech ptrs: valid={validPtrs}, inline={inlinePtrs}");

                            // Check what's at the name position
                            int nameOff = pos + HEADER_SIZE;
                            if (nameOff < zoneData.Length)
                            {
                                string maybeName = ReadNullTerminatedString(zoneData, nameOff);
                                if (maybeName.Length > 0 && maybeName.Length < 50)
                                {
                                    Debug.WriteLine($"[TechSetParser]   String at header end (0x{nameOff:X}): '{maybeName}'");
                                }
                            }
                        }
                    }
                    else if (ffCount % 500 == 0)
                    {
                        Debug.WriteLine($"[TechSetParser] ... found {ffCount} 0xFFFFFFFF markers so far at 0x{pos:X}");
                    }

                    if (LooksLikeTechSetHeader(zoneData, pos, isBigEndian))
                    {
                        Debug.WriteLine($"[TechSetParser] Header pattern matched at 0x{pos:X}, trying to parse...");
                        var result = TryParseTechSetAt(zoneData, pos, isBigEndian);
                        if (result != null)
                        {
                            Debug.WriteLine($"[TechSetParser] Successfully parsed techset '{result.Name}' at 0x{pos:X}");
                            return result;
                        }
                        else
                        {
                            Debug.WriteLine($"[TechSetParser] Parse failed at 0x{pos:X}");
                        }
                    }
                }
            }

            Debug.WriteLine($"[TechSetParser] No techset found in search range. Found {ffCount} 0xFFFFFFFF markers total.");
            return null;
        }

        /// <summary>
        /// Checks if the bytes at the given offset look like a TechSet header.
        /// Pattern:
        /// - Offset 0: 0xFFFFFFFF (name pointer inline)
        /// - Offset 4: worldVertFormat (0x00-0x0B)
        /// - Offset 5+: technique pointers (each is 0x00000000 or 0xFFFFFFFF)
        /// </summary>
        private static bool LooksLikeTechSetHeader(byte[] data, int offset, bool isBigEndian)
        {
            if (offset + HEADER_SIZE >= data.Length)
                return false;

            // Check name pointer is 0xFFFFFFFF (inline)
            uint namePtr = ReadUInt32(data, offset, isBigEndian);
            if (namePtr != 0xFFFFFFFF)
                return false;

            // Check worldVertFormat is valid (0x00-0x0B)
            byte worldVertFormat = data[offset + 4];
            if (worldVertFormat > 0x0B)
            {
                // Debug: log if worldVertFormat is out of range
                Debug.WriteLine($"[TechSetParser] At 0x{offset:X}: worldVertFormat=0x{worldVertFormat:X2} > 0x0B, rejecting");
                return false;
            }

            // Check at least some technique pointers look valid
            // They should be either 0x00000000 (null) or 0xFFFFFFFF (inline)
            int validPointers = 0;
            int inlinePointers = 0;
            int invalidPointers = 0;
            for (int i = 0; i < TECHNIQUE_COUNT; i++)
            {
                uint techPtr = ReadUInt32(data, offset + 5 + (i * 4), isBigEndian);
                if (techPtr == 0x00000000 || techPtr == 0xFFFFFFFF)
                {
                    validPointers++;
                    if (techPtr == 0xFFFFFFFF)
                        inlinePointers++;
                }
                else
                {
                    invalidPointers++;
                }
            }

            // Be more lenient - at least half should be valid null/inline pointers
            // And there should be at least 1 inline technique
            if (validPointers < TECHNIQUE_COUNT / 2 || inlinePointers < 1)
            {
                Debug.WriteLine($"[TechSetParser] At 0x{offset:X}: validPointers={validPointers}/{TECHNIQUE_COUNT}, inlinePointers={inlinePointers}, invalidPointers={invalidPointers}, rejecting");
                return false;
            }

            // Check that after header there's a valid name string
            int nameOffset = offset + HEADER_SIZE;
            if (nameOffset >= data.Length)
                return false;

            // First byte after header should be printable ASCII (start of name)
            byte firstChar = data[nameOffset];
            if (firstChar < 0x20 || firstChar > 0x7E)
            {
                Debug.WriteLine($"[TechSetParser] At 0x{offset:X}: firstChar after header=0x{firstChar:X2} not printable, rejecting");
                return false;
            }

            // Try to read the name and validate it looks like a techset name
            string potentialName = ReadNullTerminatedString(data, nameOffset);
            if (string.IsNullOrEmpty(potentialName) || potentialName.Length < 2 || !IsValidTechSetName(potentialName))
            {
                Debug.WriteLine($"[TechSetParser] At 0x{offset:X}: name '{potentialName}' invalid, rejecting");
                return false;
            }

            Debug.WriteLine($"[TechSetParser] At 0x{offset:X}: VALID header found! worldVertFormat=0x{worldVertFormat:X2}, validPointers={validPointers}, inlinePointers={inlinePointers}, name='{potentialName}'");
            return true;
        }

        /// <summary>
        /// Debug method to dump bytes at an offset for analysis.
        /// </summary>
        public static void DumpBytesAt(byte[] data, int offset, int count = 32)
        {
            if (offset + count > data.Length)
                count = data.Length - offset;

            var sb = new StringBuilder();
            sb.Append($"[TechSetParser] Bytes at 0x{offset:X}: ");
            for (int i = 0; i < count; i++)
            {
                sb.Append($"{data[offset + i]:X2} ");
            }
            Debug.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Attempts to parse a TechSet at the exact given offset.
        /// </summary>
        private static TechSetAsset? TryParseTechSetAt(byte[] zoneData, int offset, bool isBigEndian)
        {
            int currentOffset = offset;

            // Read name pointer (4 bytes at offset 0)
            uint namePtr = ReadUInt32(zoneData, currentOffset, isBigEndian);
            currentOffset += 4;

            bool nameInline = (namePtr == 0xFFFFFFFF);
            if (!nameInline)
            {
                Debug.WriteLine($"[TechSetParser] Name pointer at 0x{offset:X} is 0x{namePtr:X8}, not inline");
                return null;
            }

            // Read worldVertFormat (1 byte at offset 4)
            byte worldVertFormat = zoneData[currentOffset];
            currentOffset += 1;

            // Read technique pointers (34 * 4 bytes starting at offset 5)
            uint[] techniquePointers = new uint[TECHNIQUE_COUNT];
            int activeTechniqueCount = 0;

            for (int i = 0; i < TECHNIQUE_COUNT; i++)
            {
                techniquePointers[i] = ReadUInt32(zoneData, currentOffset, isBigEndian);
                if (techniquePointers[i] == 0xFFFFFFFF)
                {
                    activeTechniqueCount++;
                }
                currentOffset += 4;
            }

            // Current offset is now at end of header (offset + 141)
            int dataOffset = currentOffset;

            // Read name string (null-terminated)
            string name = ReadNullTerminatedString(zoneData, dataOffset);
            if (string.IsNullOrEmpty(name) || !IsValidTechSetName(name))
            {
                Debug.WriteLine($"[TechSetParser] Invalid name at 0x{dataOffset:X}: '{name}'");
                return null;
            }
            dataOffset += Encoding.ASCII.GetByteCount(name) + 1;

            Debug.WriteLine($"[TechSetParser] Found techset '{name}' at 0x{offset:X}, worldVertFormat=0x{worldVertFormat:X2}, techniques={activeTechniqueCount}");

            // Create the asset
            var asset = new TechSetAsset
            {
                Name = name,
                WorldVertFormat = worldVertFormat,
                HasBeenUploaded = false,
                ActiveTechniqueCount = activeTechniqueCount,
                StartOffset = offset,
                Techniques = new TechniqueInfo[TECHNIQUE_COUNT]
            };

            // Initialize techniques array
            for (int i = 0; i < TECHNIQUE_COUNT; i++)
            {
                asset.Techniques[i] = new TechniqueInfo
                {
                    IsPresent = (techniquePointers[i] == 0xFFFFFFFF),
                    Name = GetTechniqueTypeName(i)
                };
            }

            // For now, set end offset after the name
            // Full technique parsing would require parsing each MaterialTechnique structure
            asset.EndOffset = dataOffset;
            asset.AdditionalData = $"{activeTechniqueCount} techniques";

            return asset;
        }

        /// <summary>
        /// Validates that a string looks like a valid techset name.
        /// TechSet names are typically like: "mc_l_sm_r0c0n0s0", "2d", "effect_falloff_sm", etc.
        /// </summary>
        private static bool IsValidTechSetName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 2 || name.Length > 64)
                return false;

            // TechSet names contain lowercase letters, digits, underscores
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            // Should start with a letter or digit
            if (!char.IsLetterOrDigit(name[0]))
                return false;

            return true;
        }

        /// <summary>
        /// Gets the technique type name for a given index.
        /// </summary>
        public static string GetTechniqueTypeName(int index)
        {
            if (index >= 0 && index < TechniqueTypeNames.Length)
            {
                return TechniqueTypeNames[index];
            }
            return $"technique_{index}";
        }

        private static string ReadNullTerminatedString(byte[] data, int offset)
        {
            var sb = new StringBuilder();
            while (offset < data.Length && data[offset] != 0x00)
            {
                char c = (char)data[offset];
                if (c < 0x20 || c > 0x7E) break; // Stop at non-printable
                sb.Append(c);
                offset++;
            }
            return sb.ToString();
        }

        private static uint ReadUInt32(byte[] data, int offset, bool bigEndian)
        {
            if (bigEndian)
            {
                return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                              (data[offset + 2] << 8) | data[offset + 3]);
            }
            else
            {
                return (uint)(data[offset] | (data[offset + 1] << 8) |
                              (data[offset + 2] << 16) | (data[offset + 3] << 24));
            }
        }
    }
}
