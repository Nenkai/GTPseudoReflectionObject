using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Syroot.BinaryData;

namespace GTStandardDefinitionEditor.Entities
{
    public abstract class SDEFBase
    {
        public int Depth { get; set; }

        /// <summary>
        /// Type of parameter
        /// </summary>
        public NodeType NodeType { get; set; }

        /// <summary>
        /// Child parameters for this param if custom type
        /// </summary>
        public List<SDEFBase> ChildParameters { get; set; } = new List<SDEFBase>();

        /// <summary>
        /// Name for this param
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Custom Type Name for this param if applicable
        /// </summary>
        public string CustomTypeName { get; set; }

        public override string ToString()
            => Name;

    }

    public class SDEFParam : SDEFBase
    {
        /// <summary>
        /// Raw Value for this Param
        /// </summary>
        public SDEFVariant RawValue;
    }

    public class SDEFParamArray : SDEFBase
    {
        /// <summary>
        /// Custom Type Array Elements
        /// </summary>
        public List<SDEFBase> Values { get; set; } = new List<SDEFBase>();

        /// <summary>
        /// Raw Values Array Elements
        /// </summary>
        public SDEFVariant[] RawValuesArray = Array.Empty<SDEFVariant>();

        public override string ToString()
        {
            if (NodeType == NodeType.CustomTypeArray)
                return $"{Name}[{Values.Count}]";
            else
                return $"{Name}[{RawValuesArray.Length}]";
        }
    }

    public enum NodeType
    {
        RawValue,
        RawValueArray,
        CustomType,
        CustomTypeArray,
    }

    public class SDEFVariant
    {
        public ValueType Type;

        private int _int;
        private byte _byte;
        private bool _bool;
        private float _float;
        private uint _uint;
        private double _double;
        private sbyte _sByte;
        private ulong _ulong;
        private string _string;

        public SDEFVariant(int val)
        {
            Type = ValueType.Int;
            _int = val;
        }

        public SDEFVariant(uint val)
        {
            Type = ValueType.UInt;
            _uint = val;
        }

        public SDEFVariant(byte val)
        {
            Type = ValueType.Byte;
            _byte = val;
        }

        public SDEFVariant(sbyte val)
        {
            Type = ValueType.SByte;
            _sByte = val;
        }

        public SDEFVariant(bool val)
        {
            Type = ValueType.Bool;
            _bool = val;
        }

        public SDEFVariant(float val)
        {
            Type = ValueType.Float;
            _float = val;
        }

        public SDEFVariant(ulong val)
        {
            Type = ValueType.ULong;
            _ulong = val;
        }

        public SDEFVariant(double val)
        {
            Type = ValueType.Double;
            _double = val;
        }

        public SDEFVariant(string val)
        {
            Type = ValueType.String;
            _string = val;
        }

        public int GetInt() => _int;
        public byte GetByte() => _byte;
        public bool GetBool() => _bool;
        public float GetFloat() => _float;
        public uint GetUInt() => _uint;
        public double GetDouble() => _double;
        public sbyte GetSByte() => _sByte;
        public ulong GetULong() => _ulong;
        public string GetString() => _string;

        public void Set(int value) => _int = value;
        public void Set(uint value) => _uint = value;
        public void Set(byte value) => _byte = value;
        public void Set(sbyte value) => _sByte = value;
        public void Set(bool value) => _bool = value;
        public void Set(float value) => _float = value;
        public void Set(double value) => _double = value;
        public void Set(ulong value) => _ulong = value;
        public void Set(string value) => _string = value;

        public void WriteToStream(BinaryStream writer)
        {
            switch (Type)
            {
                case ValueType.Byte:
                    writer.WriteByte(_byte); break;
                case ValueType.Bool:
                    writer.WriteBoolean(_bool, BooleanCoding.Byte); break;
                case ValueType.SByte:
                    writer.WriteSByte(_sByte); break;
                case ValueType.Int:
                    writer.WriteInt32(_int); break;
                case ValueType.UInt:
                    writer.WriteUInt32(_uint); break;
                case ValueType.Float:
                    writer.WriteSingle(_float); break;
                case ValueType.Double:
                    writer.WriteDouble(_double); break;
                case ValueType.ULong:
                    writer.WriteDouble(_ulong); break;
                case ValueType.String:
                    writer.WriteString(_string, StringCoding.Int32CharCount); break;
                default:
                    break;
            }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case ValueType.Nothing:
                    return null;
                case ValueType.Null:
                    return null;
                case ValueType.Array:
                    return null;
                case ValueType.Byte:
                    return $"{_byte} (Byte)";
                case ValueType.Bool:
                    return $"{_bool} (Bool)";
                case ValueType.SByte:
                    return $"{_sByte} (SByte)";
                case ValueType.Int:
                    return $"{_int} (Int32)";
                case ValueType.UInt:
                    return $"{_uint} (UInt)";
                case ValueType.Float:
                    return $"{_float} (Float)";
                case ValueType.ULong:
                    return $"{_ulong} (ULong)";
                case ValueType.Double:
                    return $"{_double} (Double)";
                case ValueType.String:
                    return $"{_string} (String)";
                default:
                    return null;
            }
        }
    }

    public enum ValueType
    {
        Nothing,
        Null,
        Array = 2,
        String = 3,
        SByte = 4,
        Byte = 5,
        Bool = 7,
        Int = 10,
        UInt = 11,
        Float = 12,
        ULong = 14,
        Double = 15,
    }
}
