using System;
using System.Collections.Generic;
using System.IO;

namespace XKAsset
{
    public class DepsInfo
    {
        public List<string> deps;
        public AssetPkgType pkgType;
        public ProviderType providerType;

        public DepsInfo(){}
        
        public DepsInfo(string[] depNames, AssetPkgType type, ProviderType pType)
        {
            deps = new List<string>(depNames);
            pkgType = type;
            providerType = pType;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)pkgType);
            writer.Write((byte)providerType);
            writer.Write((Int16)deps.Count);
            for (int i = 0; i < deps.Count; i++)
            {
                writer.Write(deps[i]);
            }
        }

        public void DeSerialize(BinaryReader reader)
        {
            pkgType = (AssetPkgType)reader.ReadByte();
            providerType = (ProviderType)reader.ReadByte();
            int cnt = reader.ReadInt16();
            deps = new List<string>();
            for (int i = 0; i < cnt; i++)
            {
                deps.Add(reader.ReadString());
            }
        }
    }
    
    public class BuildAssetData
    {
        //依赖
        public Dictionary<string, DepsInfo> assetData;

        public void Add(string key, string[] value, AssetPkgType type, ProviderType providerType)
        {
            if (assetData == null)
                assetData = new Dictionary<string, DepsInfo>();
            assetData.Add(key, new DepsInfo(value, type, providerType));
        }
        
        public void Serialize(Stream stream)
        {
            using BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(assetData.Count);
            foreach (var item in assetData)
            {
                writer.Write(item.Key);
                item.Value.Serialize(writer);
            }
        }

        public void DeSerialize(byte[] data)
        {
            if (assetData == null)
                assetData = new Dictionary<string, DepsInfo>();
            using BinaryReader reader = new BinaryReader(new MemoryStream(data));
            int nIdx = reader.ReadInt32();
            for (int i = 0; i < nIdx; i++)
            {
                string key = reader.ReadString();
                var depsInfo = new DepsInfo();
                depsInfo.DeSerialize(reader);
                assetData.Add(key, depsInfo);
            }
        }
        
        public void DeSerialize(Stream data)
        {
            if (assetData == null)
                assetData = new Dictionary<string, DepsInfo>();
            using BinaryReader reader = new BinaryReader(data);
            int nIdx = reader.ReadInt32();
            for (int i = 0; i < nIdx; i++)
            {
                string key = reader.ReadString();
                var depsInfo = new DepsInfo();
                depsInfo.DeSerialize(reader);
                assetData.Add(key, depsInfo);
            }
        }
    }
}