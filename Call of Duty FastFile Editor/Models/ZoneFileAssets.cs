using System;
using System.Collections.Generic;

namespace Call_of_Duty_FastFile_Editor.Models
{
    public class ZoneFileAssets
    {
        // the order is imporant because thats the order they appear in the zone file
        public List<ZoneAssetRecord>? ZoneAssetsPool { get; set; } = new List<ZoneAssetRecord>();

        // A dictionary: key = asset type, value = list of records of that type
        public Dictionary<ZoneFileAssetType, List<ZoneAssetRecord>> AssetsByType { get; set; }
            = new Dictionary<ZoneFileAssetType, List<ZoneAssetRecord>>();

        public List<StringTable> StringTables { get; set; } = new List<StringTable>();
        public List<RawFileNode> RawFiles { get; set; } = new List<RawFileNode>();
        public Tags? Tags { get; set; } = new Tags();
    }

    public struct ZoneAssetRecord
    {
        public ZoneFileAssetType AssetType { get; set; }
        public int AdditionalData { get; set; }
        public int Offset { get; set; }
        public int DataStartOffset { get; set; }
        public int DataEndOffset { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public int Size { get; set; }
        public byte[] RawDataBytes { get; set; }
    }

    public enum ZoneFileAssetType
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
}
