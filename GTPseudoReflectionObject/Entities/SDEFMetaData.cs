using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;
using GTPseudoReflectionObject.Utils;

namespace GTPseudoReflectionObject.Entities
{
    public class SDEFMetaData
    {
        public const string MAGIC = "SDEF";

        public List<SDEFMetaDataCategory> Categories { get; set; } = new List<SDEFMetaDataCategory>();
        public ushort MasterTypeIndexOrID { get; set; }
        public bool MasterHasCustomType { get; set; }

        public static PseudoReflectionObject FromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);

            // Read the sdef type metadata
            using (var bs = new BinaryStream(new MemoryStream(bytes)))
            {
                SDEFMetaData sdef = new SDEFMetaData();
                if (bs.ReadString(4) != MAGIC)
                    throw new InvalidDataException("Not a SDEF file.");

                uint unk = bs.ReadUInt32();
                int fixedArrLengthVersion;
                if (unk >= 1) // GT7SP Uses that
                {
                    fixedArrLengthVersion = 1; // By default seems like its fixed arrays
                    uint unkSize = bs.ReadUInt32();
                    bs.Position += unkSize; // Not figured
                }
                else
                {
                    // When 1, all arrays are fixed length and provided in the type metadata.
                    fixedArrLengthVersion = bs.ReadInt32();
                    if (fixedArrLengthVersion >= 1)
                        bs.ReadByte(); // Empty
                }

                int catCount = bs.ReadInt32();

                for (int i = 0; i < catCount; i++)
                {
                    int strLength = bs.ReadInt32();
                    var categoryName = bs.ReadString(strLength - 1); bs.Position += 1; // Null

                    var category = new SDEFMetaDataCategory();
                    category.Name = categoryName;
                    sdef.Categories.Add(category);
                    int entryCount = bs.ReadInt32();
                    for (int j = 0; j < entryCount; j++)
                    {
                        int entryNameLength = bs.ReadInt32(); 
                        var entryName = bs.ReadString(entryNameLength - 1); bs.Position += 1; // Null

                        var entry = new SDEFMetaDataEntry();
                        entry.Name = entryName;
                        category.Entries.Add(entry);

                        entry.TypeOrIndex = bs.ReadUInt16();
                        entry.HasCustomType = bs.ReadBoolean(BooleanCoding.Word);

                        // If its an array, additional data is stored to know if its an array of types rather than raw values
                        if (entry.TypeOrIndex == (int)(ValueType.Array))
                        {
                            if (!entry.HasCustomType)
                            {
                                entry.ArrayCategoryIndex = bs.ReadUInt16();
                                entry.ArrayHasCustomType = bs.ReadBoolean(BooleanCoding.Word);

                                entry.ArrayLength = bs.ReadUInt32(); // 0 when version is 0 - its variable
                            }
                        }
                    }
                }

                // Final data is pretty much which type is first
                sdef.MasterTypeIndexOrID = bs.ReadUInt16();
                sdef.MasterHasCustomType = bs.ReadBoolean(BooleanCoding.Word);

                // The data part is the tree structure reassembling & data
                var def = new PseudoReflectionObject();
                def.Version = fixedArrLengthVersion;
                var mainCategory = sdef.Categories[sdef.MasterTypeIndexOrID];

                def.ParameterRoot = new SDEFParam();
                def.ParameterRoot.CustomTypeName = mainCategory.Name;
                def.ParameterRoot.NodeType = NodeType.CustomType;

                int depth = 0;
                Traverse(bs, fixedArrLengthVersion, def, def.ParameterRoot, sdef, mainCategory, ref depth);
                return def;
            }
        }

        public static void Traverse(BinaryStream reader, int version, PseudoReflectionObject sdef, SDEFBase parentNode, SDEFMetaData sdefMetadata, SDEFMetaDataCategory nodeCategory, ref int depth)
        {
            depth++;
            foreach (var entry in nodeCategory.Entries)
            {
                SDEFBase current;
                if (!entry.HasCustomType && (ValueType)entry.TypeOrIndex == ValueType.Array)
                    current = new SDEFParamArray(); // Param is a param array
                else
                    current = new SDEFParam(); // Param is regular parameter, but if its a custom type it may have children parameters

                current.Name = entry.Name;
                parentNode.ChildParameters.Add(current);

                sdef.ParameterList.Add(current);
                if (entry.HasCustomType)
                {
                    current.CustomTypeName = sdefMetadata.Categories[entry.TypeOrIndex].Name;
                    current.NodeType = NodeType.CustomType;

                    // Traverse children parameter for this basic type
                    Traverse(reader, version, sdef, current, sdefMetadata, sdefMetadata.Categories[entry.TypeOrIndex], ref depth);
                }
                else if ((ValueType)entry.TypeOrIndex == ValueType.Array)
                {
                    if (entry.ArrayHasCustomType)
                    {
                        current.NodeType = NodeType.CustomTypeArray;
                        current.CustomTypeName = sdefMetadata.Categories[entry.ArrayCategoryIndex].Name;
                        if (version == 0)
                            entry.ArrayLength = reader.ReadUInt32();

                        for (int i = 0; i < entry.ArrayLength; i++)
                        {
                            // Create the element for the array to add later
                            var arrayElement = new SDEFParam();
                            arrayElement.CustomTypeName = current.CustomTypeName;
                            arrayElement.NodeType = NodeType.CustomType;
                            arrayElement.Name = $"[{i}]";
                            Traverse(reader, version, sdef, arrayElement, sdefMetadata, sdefMetadata.Categories[entry.ArrayCategoryIndex], ref depth);

                            // Don't forget to add our array element to the global parameter list
                            sdef.ParameterList.Add(arrayElement);
                            (current as SDEFParamArray).Values.Add(arrayElement);
                        }
                    }
                    else
                    {
                        current.CustomTypeName = nodeCategory.Name;
                        current.NodeType = NodeType.RawValueArray;
                        if (version == 0)
                            entry.ArrayLength = reader.ReadUInt32();

                        (current as SDEFParamArray).RawValuesArray = new SDEFVariant[entry.ArrayLength];
                        for (int i = 0; i < entry.ArrayLength; i++)
                        {
                            var val = ReadData(reader, entry, (ValueType)entry.ArrayCategoryIndex);
                            (current as SDEFParamArray).RawValuesArray[i] = val;
                        }
                    }
                }
                else
                {
                    current.NodeType = NodeType.RawValue;
                    var variant = ReadData(reader, entry, (ValueType)entry.TypeOrIndex);
                    (current as SDEFParam).RawValue = variant;
                }

                current.Depth = depth;            
            }
            depth--;
        }

        public static SDEFVariant ReadData(BinaryStream bs, SDEFMetaDataEntry entry, ValueType valType)
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
                variant = new SDEFVariant(bs.ReadUInt32());
            }
            else if (valType == ValueType.Double)
            {
                variant = new SDEFVariant(bs.ReadDouble());
            }
            else if (valType == ValueType.SByte)
            {
                variant = new SDEFVariant((sbyte)bs.ReadByte());
            }
            else if (valType == ValueType.ULong)
            {
                variant = new SDEFVariant(bs.ReadUInt64());
            }
            else if (valType == ValueType.String)
            {
                variant = new SDEFVariant(bs.ReadString(StringCoding.Int32CharCount));
            }
            else
            {
                throw new InvalidDataException($"Encountered unsupported type {valType} at 0x{bs.Position:X2}");
            }

            return variant;
        }

    }
}
