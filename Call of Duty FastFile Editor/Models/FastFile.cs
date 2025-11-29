using System.Net;
using System.Text;
using Call_of_Duty_FastFile_Editor.Constants;
using Call_of_Duty_FastFile_Editor.GameDefinitions;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class FastFile
    {
        public string FfFilePath { get; }
        public string ZoneFilePath { get; }
        public ZoneFile OpenedFastFileZone { get; private set; }
        public FastFileHeader OpenedFastFileHeader { get; }

        public string FastFileName => Path.GetFileName(FfFilePath);
        public string FastFileMagic => OpenedFastFileHeader.FastFileMagic;
        public int GameVersion => OpenedFastFileHeader.GameVersion;
        public int FileLength => OpenedFastFileHeader.FileLength;
        public bool IsValid => OpenedFastFileHeader.IsValid;
        public bool IsCod4File => OpenedFastFileHeader.IsCod4File;
        public bool IsCod5File => OpenedFastFileHeader.IsCod5File;
        public bool IsMW2File => OpenedFastFileHeader.IsMW2File;

        public FastFile(string filePath)
        {
            FfFilePath = filePath
                ?? throw new ArgumentException("File path cannot be null.", nameof(filePath));
            ZoneFilePath = Path.ChangeExtension(filePath, ".zone");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"FastFile not found: {filePath}", filePath);

            // Defer loading .zone until after you decompress it:
            OpenedFastFileZone = new ZoneFile(ZoneFilePath, this);

            OpenedFastFileHeader = new FastFileHeader(filePath);
        }

        /// <summary>
        /// AFTER you’ve written the .zone to disk, call this to load it.
        /// </summary>
        public void LoadZone()
        {
            OpenedFastFileZone = ZoneFile.Load(ZoneFilePath, this);
        }

        public class FastFileHeader
        {
            public string FastFileMagic { get; private set; }
            public int GameVersion { get; private set; }
            public int FileLength { get; private set; }
            public bool IsValid { get; private set; }
            public bool IsCod4File { get; private set; }
            public bool IsCod5File { get; private set; }
            public bool IsMW2File { get; private set; }

            public FastFileHeader(string filePath)
            {
                using var br = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read), Encoding.Default);
                if (br.BaseStream.Length < 12)
                {
                    IsValid = false;
                    return;
                }

                FastFileMagic = new string(br.ReadChars(8)).TrimEnd('\0');
                GameVersion = IPAddress.NetworkToHostOrder(br.ReadInt32());
                FileLength = (int)new FileInfo(filePath).Length;

                ValidateHeader();
            }

            /// <summary>
            /// Validates the Fast File header.
            /// </summary>
            private void ValidateHeader()
            {
                // Initial validation
                IsValid = false;

                // Check the FastFileMagic and GameVersion to determine validity
                if (FastFileMagic == FastFileHeaderConstants.UnSignedFF)
                {
                    if (GameVersion == CoD4Definition.VersionValue ||
                        GameVersion == CoD4Definition.PCVersionValue)
                    {
                        IsCod4File = true;
                        IsValid = true;
                    }
                    else if (GameVersion == CoD5Definition.VersionValue ||
                             GameVersion == CoD5Definition.PCVersionValue)
                    {
                        IsCod5File = true;
                        IsValid = true;
                    }
                    else if (GameVersion == MW2Definition.VersionValue ||
                             GameVersion == MW2Definition.PCVersionValue)
                    {
                        IsMW2File = true;
                        IsValid = true;
                    }
                }
            }
        }
    }
}
