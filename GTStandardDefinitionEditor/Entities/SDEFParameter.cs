using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Syroot.BinaryData;

namespace GTStandardDefinitionEditor.Entities
{
    public class SDEFParameter
    {
        public int Depth { get; set; }
        public List<SDEFParameter> ChildParameters { get; set; } = new List<SDEFParameter>();

        public NodeType NodeType { get; set; }
        public string Name { get; set; }
        public string CustomTypeName { get; set; }

        public int CustomTypeArrayLength { get; set; }
        public SDEFVariant RawValue;
        public SDEFVariant[] RawValuesArray;

        public override string ToString()
            => Name;
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

        public SDEFVariant(double val)
        {
            Type = ValueType.Double;
            _double = val;
        }

        public int GetInt() => _int;
        public byte GetByte() => _byte;
        public bool GetBool() => _bool;
        public float GetFloat() => _float;
        public uint GetUInt() => _uint;
        public double GetDouble() => _double;
        public sbyte GetSByte() => _sByte;

        public void Set(int value) => _int = value;
        public void Set(uint value) => _uint = value;
        public void Set(byte value) => _byte = value;
        public void Set(sbyte value) => _sByte = value;
        public void Set(bool value) => _bool = value;
        public void Set(float value) => _float = value;
        public void Set(double value) => _double = value;

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
                case ValueType.Double:
                    return $"{_double} (Double)";
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
        SByte = 4,
        Byte = 5,
        Bool = 7,
        Int = 10,
        UInt = 11,
        Float = 12,
        Double = 15,
    }
}
