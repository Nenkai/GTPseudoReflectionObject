using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;
using GTStandardDefinitionEditor.Utils;

namespace GTStandardDefinitionEditor.Entities
{
    public class SDEFData
    {
        public const string MAGIC = "SDEF";

        public List<SDEFDataCategory> Categories { get; set; } = new List<SDEFDataCategory>();
        public ushort MasterTypeIndexOrID { get; set; }
        public bool MasterHasCustomType { get; set; }

        public static StandardDefinition FromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            using (var bs = new BinaryStream(new MemoryStream(bytes)))
            {
                SDEFData sdef = new SDEFData();
                if (bs.ReadString(4) != MAGIC)
                    throw new InvalidDataException("Not a SDEF file.");

                bs.Position += 4; // File Ptr
                bs.ReadInt32(); // One
                bs.ReadByte(); // Empty

                int catCount = bs.ReadInt32();

                for (int i = 0; i < catCount; i++)
                {
                    int strLength = bs.ReadInt32();
                    var categoryName = bs.ReadString(strLength - 1); bs.Position += 1; // Null

                    var category = new SDEFDataCategory();
                    category.Name = categoryName;
                    sdef.Categories.Add(category);
                    int entryCount = bs.ReadInt32();
                    for (int j = 0; j < entryCount; j++)
                    {
                        int entryNameLength = bs.ReadInt32(); 
                        var entryName = bs.ReadString(entryNameLength - 1); bs.Position += 1; // Null

                        var entry = new SDEFDataEntry();
                        entry.Name = entryName;
                        category.Entries.Add(entry);

                        entry.TypeOrIndex = bs.ReadUInt16();
                        entry.HasCustomType = bs.ReadBoolean(BooleanCoding.Word);

                        if (entry.TypeOrIndex == 2)
                        {
                            if (!entry.HasCustomType)
                            {
                                entry.ArrayCategoryIndex = bs.ReadUInt16();
                                entry.ArrayHasCustomType = bs.ReadBoolean(BooleanCoding.Word);
                                entry.ArrayLength = bs.ReadUInt32();
                            }
                        }
                    }
                }

                sdef.MasterTypeIndexOrID = bs.ReadUInt16();
                sdef.MasterHasCustomType = bs.ReadBoolean(BooleanCoding.Word);

                // Traverse
                var def = new StandardDefinition();
                var mainCategory = sdef.Categories[sdef.MasterTypeIndexOrID];

                def.ParameterRoot = new SDEFParameter();
                def.ParameterRoot.CustomTypeName = mainCategory.Name;
                def.ParameterRoot.NodeType = NodeType.CustomType;

                int depth = 0;
                Traverse(bs, def, def.ParameterRoot, sdef, mainCategory, ref depth);
                return def;
            }
        }

        public static void Traverse(BinaryStream reader, StandardDefinition sdef, SDEFParameter parentNode, SDEFData sdefMetadata, SDEFDataCategory nodeCategory, ref int depth)
        {
            depth++;
            foreach (var entry in nodeCategory.Entries)
            {
                var current = new SDEFParameter();
                current.Name = entry.Name;
                parentNode.ChildParameters.Add(current);

                sdef.ParameterList.Add(current);
                if (entry.HasCustomType)
                {
                    current.CustomTypeName = sdefMetadata.Categories[entry.TypeOrIndex].Name;
                    current.NodeType = NodeType.CustomType;

                    Traverse(reader, sdef, current, sdefMetadata, sdefMetadata.Categories[entry.TypeOrIndex], ref depth);
                }
                else if ((ValueType)entry.TypeOrIndex == ValueType.Array)
                {
                    if (entry.ArrayHasCustomType)
                    {
                        current.NodeType = NodeType.CustomTypeArray;
                        current.CustomTypeName = sdefMetadata.Categories[entry.ArrayCategoryIndex].Name;
                        current.CustomTypeArrayLength = (int)entry.ArrayLength;

                        for (int i = 0; i < entry.ArrayLength; i++)
                            Traverse(reader, sdef, current, sdefMetadata, sdefMetadata.Categories[entry.ArrayCategoryIndex], ref depth);
                    }
                    else
                    {
                        current.CustomTypeName = nodeCategory.Name;
                        current.NodeType = NodeType.RawValueArray;
                        current.RawValuesArray = new SDEFVariant[entry.ArrayLength];
                        for (int i = 0; i < entry.ArrayLength; i++)
                        {
                            var val = ReadData(reader, entry, (ValueType)entry.ArrayCategoryIndex);
                            current.RawValuesArray[i] = val;
                        }
                    }
                }
                else
                {
                    current.NodeType = NodeType.RawValue;
                    var variant = ReadData(reader, entry, (ValueType)entry.TypeOrIndex);
                    current.RawValue = variant;
                }

                current.Depth = depth;            
            }
            depth--;
        }

        public static SDEFVariant ReadData(BinaryStream bs, SDEFDataEntry entry, ValueType valType)
        {
            SDEFVariant variant;
            if (valType == ValueType.Float)
                variant = new SDEFVariant(bs.ReadSingle());
            else if (valType == ValueType.Int)
            {
                variant = new SDEFVariant(bs.ReadInt32());
            }
            else if (valType == ValueType.Byte)
            {
                variant = new SDEFVariant((byte)bs.ReadByte());
            }
            else if (valType == ValueType.Bool)
            {
                variant = new SDEFVariant(bs.ReadBoolean(BooleanCoding.Byte));
            }
            else if (valType == ValueType.UInt)
            {
                variant = new SDEFVariant((uint)bs.ReadUInt32());
            }
            else if (valType == ValueType.Double)
            {
                variant = new SDEFVariant(bs.ReadDouble());
            }
            else if (valType == ValueType.SByte)
            {
                variant = new SDEFVariant((sbyte)bs.ReadByte());
            }
            else if (valType == (ValueType)14)
            {
                bs.Position += 8;
                variant = new SDEFVariant(1);
                //throw new InvalidDataException($"Encountered unsupported type {valType} at 0x{bs.Position:X2}");
            }
            else
            {
                throw new InvalidDataException($"Encountered unsupported type {valType} at 0x{bs.Position:X2}");
            }

            return variant;
        }

    }
}
