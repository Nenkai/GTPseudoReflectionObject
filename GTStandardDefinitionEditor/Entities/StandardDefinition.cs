using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Syroot.BinaryData;

namespace GTStandardDefinitionEditor.Entities
{
    public class StandardDefinition
    {
        public SDEFParam ParameterRoot { get; set; }
        public List<SDEFBase> ParameterList { get; set; } = new List<SDEFBase>();
        public int Version { get; set; }

        public void Save(string path)
        {
            
            // First we build the type metadata
            var metadataList = ParameterList
                .OrderByDescending(e => e.Depth) // Base Tree Meta Data is stored by depth within the tree
                .Where(e => e.NodeType == NodeType.CustomType || e.NodeType == NodeType.CustomTypeArray) // We only want custom types
                .GroupBy(e => e.CustomTypeName).Select(e => e.FirstOrDefault()) // DistinctBy - We only want one of each definition
                .ToList();
            metadataList.Add(ParameterRoot);

            using (var fs = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryStream(fs, ByteConverter.Little))
            {
                writer.WriteString("SDEF", StringCoding.Raw);
                writer.Position += 4; // Runtime File Ptr
                writer.WriteInt32(Version);
                if (Version >= 1)
                    writer.Position += 1;

                writer.WriteInt32(metadataList.Count);

                for (int i = 0; i < metadataList.Count; i++)
                {
                    SDEFBase type = metadataList[i];
                    writer.WriteInt32(type.CustomTypeName.Length + 1);
                    writer.WriteString(type.CustomTypeName, StringCoding.ZeroTerminated);

                    if (type.NodeType == NodeType.CustomTypeArray)
                    {
                        var firstElement = (type as SDEFParamArray).Values.First();
                        writer.WriteInt32(firstElement.ChildParameters.Count);
                        WriteParameterMetadata(writer, metadataList, firstElement);
                        
                    }
                    else
                    {
                        writer.WriteInt32(type.ChildParameters.Count);
                        WriteParameterMetadata(writer, metadataList, type);
                        
                    }
                }

                // Write the first data to read
                writer.WriteInt16((short)metadataList.FindIndex(e => e.CustomTypeName == ParameterRoot.CustomTypeName));
                writer.WriteBoolean(ParameterRoot.NodeType == NodeType.CustomTypeArray || ParameterRoot.NodeType == NodeType.CustomType, BooleanCoding.Word);

                // Now the data itself if needed
                TraverseAndWriteData(writer, ParameterRoot);
            }

        }

        public void WriteParameterMetadata(BinaryStream writer, List<SDEFBase> metadataList, SDEFBase sdefBase)
        {
            for (int j = 0; j < sdefBase.ChildParameters.Count; j++)
            {
                SDEFBase entry = sdefBase.ChildParameters[j];
                writer.WriteInt32(entry.Name.Length + 1);
                writer.WriteString(entry.Name, StringCoding.ZeroTerminated);

                if (entry is SDEFParamArray)
                    writer.WriteInt16((short)ValueType.Array);
                else
                {
                    SDEFParam param = entry as SDEFParam;
                    if (entry.NodeType == NodeType.RawValue)
                        writer.WriteInt16((short)param.RawValue.Type);
                    else if (entry.NodeType == NodeType.CustomType)
                    {
                        int typeIndex = metadataList.FindIndex(metaType => metaType.CustomTypeName == entry.CustomTypeName);
                        writer.WriteInt16((short)typeIndex);
                    }
                }

                writer.WriteBoolean(entry.NodeType == NodeType.CustomType, BooleanCoding.Word);

                if (entry is SDEFParamArray array)
                {
                    if (entry.NodeType == NodeType.RawValueArray)
                        writer.WriteInt16((short)array.RawValuesArray[0].Type);
                    else
                    {
                        int typeIndex = metadataList.FindIndex(metaType => metaType.CustomTypeName == entry.CustomTypeName);
                        writer.WriteInt16((short)typeIndex);
                    }

                    writer.WriteBoolean(entry.NodeType == NodeType.CustomTypeArray, BooleanCoding.Word);

                    if (entry.NodeType == NodeType.CustomTypeArray)
                    {
                        if (Version == 0)
                            writer.WriteInt32(0);
                        else
                            writer.WriteInt32((short)array.Values.Count);
                    }
                    else
                    {
                        if (Version == 0)
                            writer.WriteInt32(0);
                        else
                            writer.WriteInt32((short)array.RawValuesArray.Length);
                    }
                }
            }
        }

        private void TraverseAndWriteData(BinaryStream writer, SDEFBase param)
        {
            foreach (var entry in param.ChildParameters)
            {
                if (entry.NodeType == NodeType.CustomType)
                    TraverseAndWriteData(writer, entry);
                else if (entry.NodeType == NodeType.RawValue)
                    (entry as SDEFParam).RawValue.WriteToStream(writer);
                else
                {
                    var arr = entry as SDEFParamArray;
                    if (entry.NodeType == NodeType.CustomTypeArray)
                    {
                        if (Version == 0)
                            writer.WriteInt32(arr.Values.Count);

                        for (int i = 0; i < arr.Values.Count; i++)
                          TraverseAndWriteData(writer, arr.Values[i]);
                    }
                    else if (entry.NodeType == NodeType.RawValueArray)
                    {
                        if (Version == 0)
                            writer.WriteInt32(arr.RawValuesArray.Length);

                        foreach (var val in arr.RawValuesArray)
                            val.WriteToStream(writer);
                    }
                }
            }
        }
    }
}
