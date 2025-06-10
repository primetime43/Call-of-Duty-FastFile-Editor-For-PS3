using System;
using System.Collections.Generic;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class ZoneFileAssetManifest
    {
        /// <summary>
        /// List of asset records in the zone file.
        /// Parsed from the asset pool.
        /// Order MUST stay the same as in the zone file.
        /// </summary>
        public List<ZoneAssetRecord>? ZoneAssetRecords { get; set; } = new List<ZoneAssetRecord>();
    }

    public struct ZoneAssetRecord
    {
        public ZoneFileAssetType_COD5 AssetType_COD5 { get; set; }
        public ZoneFileAssetType_COD4 AssetType_COD4 { get; set; }
        public int HeaderStartOffset { get; set; }
        /// <summary>
        /// The offset where the header ends before the data. (Includes the null terminator)
        /// </summary>
        public int HeaderEndOffset { get; set; }
        public int AssetPoolRecordOffset { get; set; }
        /// <summary>
        /// The offset where the asset data starts after the header.
        /// </summary>
        public int AssetDataStartPosition { get; set; }
        /// <summary>
        /// The offset where the asset data ends. (Before the null terminator if it has one)
        /// </summary>
        public int AssetDataEndOffset { get; set; }

        /// <summary>
        /// The offset where the asset record ends. (After the null terminator if it has one)
        /// </summary>
        public int AssetRecordEndOffset { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public int Size { get; set; }
        public byte[] RawDataBytes { get; set; }
        public string AdditionalData { get; set; }
    }

    // Maybe move this to Constants?
    public enum ZoneFileAssetType_COD5
    {
        physpreset = 0x01,
        physconstraints = 0x02,
        destructibledef = 0x03,
        xanim = 0x04,
        xmodel = 0x05,
        material = 0x06,
        pixelshader = 0x07,
        vertexshader = 0x08,
        techset = 0x09,
        image = 0x0A,
        sound = 0x0B,
        loaded_sound = 0x0C,
        col_map_sp = 0x0D,
        col_map_mp = 0x0E,
        com_map = 0x0F,
        game_map_sp = 0x10,
        game_map_mp = 0x11,
        map_ents = 0x12,
        gfx_map = 0x13,
        lightdef = 0x14,
        ui_map = 0x15,
        font = 0x16,
        menufile = 0x17,
        menu = 0x18,
        localize = 0x19,
        weapon = 0x1A,
        snddriverglobals = 0x1B,
        fx = 0x1C,
        impactfx = 0x1D,
        aitype = 0x1E,
        mptype = 0x1F,
        character = 0x20,
        xmodelalias = 0x21,
        rawfile = 0x22,
        stringtable = 0x23,
        packindex = 0x24
    }

    public enum ZoneFileAssetType_COD4
    {
        physpreset = 0x01,  // physpreset
        xanim = 0x02,  // xanim
        xmodel = 0x03,  // xmodel
        material = 0x04,  // material
        pixelshader = 0x05,  // pixelshader
        vertexshader = 0x06,  // vertexshader
        techset = 0x07,  // techset
        image = 0x08,  // image
        sound = 0x09,  // sound
        sndcurve = 0x0A,  // sndcurve
        loaded_sound = 0x0B,  // loaded_sound
        col_map_sp = 0x0C,  // col_map_sp
        col_map_mp = 0x0D,  // col_map_mp
        com_map = 0x0E,  // com_map
        game_map_sp = 0x0F,  // game_map_sp
        game_map_mp = 0x10,  // game_map_mp
        map_ents = 0x11,  // map_ents
        gfx_map = 0x12,  // gfx_map
        lightdef = 0x13,  // lightdef
        font = 0x15,  // font
        menufile = 0x16,  // menufile
        menu = 0x17,  // menu
        localize = 0x18,  // localize
        weapon = 0x19,  // weapon
        snddriverglobals = 0x1A, // snddriverglobals
        fx = 0x1B,  // fx
        impactfx = 0x1C,  // impactfx
        rawfile = 0x21,  // rawfile
        stringtable = 0x22   // stringtable
    }
}
