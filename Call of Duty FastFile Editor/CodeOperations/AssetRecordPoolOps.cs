using Call_of_Duty_FastFile_Editor.Models;
using System.Diagnostics;

namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    public class AssetRecordPoolOps
    {
        public static void AddRawFileAssetRecordToPool(Zone currentZone, string zoneFilePath)
        {
            if (currentZone.AssetPoolStartOffset < 0 || currentZone.AssetPoolEndOffset < 0)
            {
                throw new InvalidOperationException("Asset pool offsets are not properly set.");
            }

            byte[] newRecord = new byte[8] { 0x00, 0x00, 0x00, 0x22, 0xFF, 0xFF, 0xFF, 0xFF };

            currentZone.ModifyZoneFile(fs =>
            {
                long insertPosition = currentZone.AssetPoolStartOffset;
                long originalLength = fs.Length;

                fs.Seek(insertPosition, SeekOrigin.Begin);
                byte[] tailBuffer = new byte[originalLength - insertPosition];
                fs.Read(tailBuffer, 0, tailBuffer.Length);

                fs.SetLength(originalLength + newRecord.Length);

                fs.Seek(insertPosition + newRecord.Length, SeekOrigin.Begin);
                fs.Write(tailBuffer, 0, tailBuffer.Length);

                fs.Seek(insertPosition, SeekOrigin.Begin);
                fs.Write(newRecord, 0, newRecord.Length);

                int assetRecordCountOffset = Constants.ZoneFile.AssetRecordCountOffset;
                fs.Seek(assetRecordCountOffset, SeekOrigin.Begin);
                byte[] countBytes = new byte[4];
                fs.Read(countBytes, 0, 4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(countBytes);
                }
                uint currentCount = BitConverter.ToUInt32(countBytes, 0);
                uint newCount = currentCount + 1;
                byte[] newCountBytes = BitConverter.GetBytes(newCount);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(newCountBytes);
                }
                fs.Seek(assetRecordCountOffset, SeekOrigin.Begin);
                fs.Write(newCountBytes, 0, newCountBytes.Length);
            });

            currentZone.AssetPoolEndOffset += newRecord.Length;

            if (currentZone.ZoneFileAssets.ZoneAssetRecords != null)
            {
                for (int i = 0; i < currentZone.ZoneFileAssets.ZoneAssetRecords.Count; i++)
                {
                    ZoneAssetRecord record = currentZone.ZoneFileAssets.ZoneAssetRecords[i];
                    if (record.AssetPoolRecordOffset >= currentZone.AssetPoolStartOffset)
                    {
                        record.AssetPoolRecordOffset += newRecord.Length;
                        currentZone.ZoneFileAssets.ZoneAssetRecords[i] = record;
                    }
                }
            }

            ZoneAssetRecord newAssetRecord = new ZoneAssetRecord
            {
                AssetType = ZoneFileAssetType_COD5.rawfile,
                AssetPoolRecordOffset = currentZone.AssetPoolStartOffset,
            };
            currentZone.ZoneFileAssets.ZoneAssetRecords.Insert(0, newAssetRecord);

            Debug.WriteLine($"New asset record inserted at offset 0x{currentZone.AssetPoolStartOffset:X}. New asset count: {currentZone.AssetRecordCount + 1}");
        }
    }
}