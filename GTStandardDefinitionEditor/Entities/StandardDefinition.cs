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
        public SDEFParameter ParameterRoot { get; set; }
        public List<SDEFParameter> ParameterList { get; set; } = new List<SDEFParameter>();

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
                writer.WriteInt32(1);
                writer.Position += 1;
                writer.WriteInt32(metadataList.Count);

                for (int i = 0; i < metadataList.Count; i++)
                {
                    SDEFParameter type = metadataList[i];
                    writer.WriteInt32(type.CustomTypeName.Length + 1);
                    writer.WriteString(type.CustomTypeName, StringCoding.ZeroTerminated);
                    writer.WriteInt32(type.ChildParameters.Count);

                    foreach (var entry in type.ChildParameters)
                    {
                        writer.WriteInt32(entry.Name.Length + 1);
                        writer.WriteString(entry.Name, StringCoding.ZeroTerminated);
                        if (entry.NodeType == NodeType.CustomTypeArray || entry.NodeType == NodeType.RawValueArray)
                            writer.WriteInt16((short)ValueType.Array);
                        else if (entry.NodeType == NodeType.RawValue)
                            writer.WriteInt16((short)entry.RawValue.Type);
                        else if (entry.NodeType == NodeType.CustomType)
                        {
                            int typeIndex = metadataList.FindIndex(metaType => metaType.CustomTypeName == entry.CustomTypeName);
                            writer.WriteInt16((short)typeIndex);
                        }

                        writer.WriteBoolean(entry.NodeType == NodeType.CustomType, BooleanCoding.Word);

                        if (entry.NodeType == NodeType.CustomTypeArray || entry.NodeType == NodeType.RawValueArray)
                        {
                            if (entry.NodeType == NodeType.RawValueArray)
                                writer.WriteInt16((short)entry.RawValuesArray[0].Type);
                            else
                            {
                                int typeIndex = metadataList.FindIndex(metaType => metaType.CustomTypeName == entry.CustomTypeName);
                                writer.WriteInt16((short)typeIndex);
                            }

                            writer.WriteBoolean(entry.NodeType == NodeType.CustomTypeArray, BooleanCoding.Word);

                            if (entry.NodeType == NodeType.CustomTypeArray)
                                writer.WriteInt32((short)entry.CustomTypeArrayLength);
                            else
                                writer.WriteInt32((short)entry.RawValuesArray.Length);


                        }
                    }
                }

                // Write the first data to read
                writer.WriteInt16((short)metadataList.FindIndex(e => e.CustomTypeName == ParameterRoot.CustomTypeName));
                writer.WriteBoolean(ParameterRoot.NodeType == NodeType.CustomTypeArray || ParameterRoot.NodeType == NodeType.CustomType, BooleanCoding.Word);

                // Now the data itself if needed
                TraverseAndWriteData(writer, ParameterRoot);
            }

        }

        private void TraverseAndWriteData(BinaryStream writer, SDEFParameter param)
        {
            foreach (var entry in param.ChildParameters)
            {
                if (entry.NodeType == NodeType.CustomType)
                    TraverseAndWriteData(writer, entry);
                else if (entry.NodeType == NodeType.RawValue)
                    entry.RawValue.WriteToStream(writer);
                else if (entry.NodeType == NodeType.CustomTypeArray)
                {
                    for (int i = 0; i < entry.CustomTypeArrayLength; i++)
                        TraverseAndWriteData(writer, entry);
                }
                else if (entry.NodeType == NodeType.RawValueArray)
                {
                    foreach (var val in entry.RawValuesArray)
                        val.WriteToStream(writer);
                }
            }
        }
    }
}
