using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Voltaic.Serialization
{
    public static class EnumMap
    {
        public static EnumMap<T> For<T>() where T : struct, Enum => EnumMap<T>.Instance;
    }

    public abstract class EnumMap<T>
        where T : struct, Enum
    {
        public static readonly EnumMap<T> Instance = CreateInstance();
        private static EnumMap<T> CreateInstance()
        {
            var baseType = Enum.GetUnderlyingType(typeof(T));
            if (baseType == typeof(sbyte))
                return new SByteEnumMap<T>();
            else if (baseType == typeof(short))
                return new Int16EnumMap<T>();
            else if (baseType == typeof(int))
                return new Int32EnumMap<T>();
            else if (baseType == typeof(long))
                return new Int64EnumMap<T>();
            else if (baseType == typeof(byte))
                return new ByteEnumMap<T>();
            else if (baseType == typeof(ushort))
                return new UInt16EnumMap<T>();
            else if (baseType == typeof(uint))
                return new UInt32EnumMap<T>();
            else if (baseType == typeof(ulong))
                return new UInt64EnumMap<T>();
            else
                throw new InvalidOperationException($"{baseType.Name} enums are not supported");
        }

        private readonly Dictionary<string, T> _keyToValue;
        private readonly MemoryDictionary<T> _utf8KeyToValue;
        private readonly Dictionary<T, string> _valueToKey;
        private readonly Dictionary<T, Utf8String> _valueToUtf8Key;

        public ulong MaxValue { get; }
        public bool IsStringEnum { get; } = typeof(T).GetTypeInfo().GetCustomAttribute<ModelStringEnumAttribute>() != null;
        public bool IsFlagsEnum { get; } = typeof(T).GetTypeInfo().GetCustomAttribute<FlagsAttribute>() != null;

        public EnumMap()
        {
            var typeInfo = typeof(T).GetTypeInfo();
            if (!typeInfo.IsEnum)
                throw new InvalidOperationException($"{typeInfo.Name} is not an Enum");
            if (IsStringEnum && IsFlagsEnum)
                throw new NotSupportedException("ModelStringEnum cannot be used on a Flags enum");

            _keyToValue = new Dictionary<string, T>();
            _utf8KeyToValue = new MemoryDictionary<T>();
            //_intToValue = new Dictionary<long, T>();

            _valueToKey = new Dictionary<T, string>();
            _valueToUtf8Key = new Dictionary<T, Utf8String>();
            //_valueToInt = new Dictionary<T, long>();

            foreach (T val in Enum.GetValues(typeof(T)).OfType<T>())
            {
                var fieldInfo = typeInfo.GetDeclaredField(Enum.GetName(typeof(T), val));
                var attr = fieldInfo.GetCustomAttribute<ModelEnumValueAttribute>();
                if (attr != null)
                {
                    string utf16Key = attr.Key;

                    var utf16Bytes = MemoryMarshal.AsBytes(utf16Key.AsSpan());
                    if (Encodings.Utf16.ToUtf8Length(utf16Bytes, out var length) != OperationStatus.Done)
                        throw new ArgumentException("Failed to serialize enum key to UTF8");
                    var utf8Key = new byte[length].AsSpan();
                    if (Encodings.Utf16.ToUtf8(utf16Bytes, utf8Key, out _, out _) != OperationStatus.Done)
                        throw new ArgumentException("Failed to serialize enum key to UTF8");

                    if (attr.Type != EnumValueType.WriteOnly)
                    {
                        _keyToValue.Add(utf16Key, val);
                        _utf8KeyToValue.Add(utf8Key, val);
                    }
                    if (attr.Type != EnumValueType.ReadOnly)
                    {
                        _valueToKey.Add(val, utf16Key);
                        _valueToUtf8Key.Add(val, new Utf8String(utf8Key));
                    }
                }

                var underlyingType = Enum.GetUnderlyingType(typeof(T));
                long baseVal;
                if (underlyingType == typeof(sbyte))
                    baseVal = (sbyte)(ValueType)val;
                else if (underlyingType == typeof(short))
                    baseVal = (short)(ValueType)val;
                else if (underlyingType == typeof(int))
                    baseVal = (int)(ValueType)val;
                else if (underlyingType == typeof(long))
                    baseVal = (long)(ValueType)val;
                else if (underlyingType == typeof(byte))
                    baseVal = (byte)(ValueType)val;
                else if (underlyingType == typeof(ushort))
                    baseVal = (ushort)(ValueType)val;
                else if (underlyingType == typeof(uint))
                    baseVal = (uint)(ValueType)val;
                else if (underlyingType == typeof(ulong))
                    baseVal = (long)(ulong)(ValueType)val;
                else
                    throw new SerializationException($"Unsupported underlying enum type: {underlyingType.Name}");

                //_intToValue.Add(baseVal, val);
                //_valueToInt.Add(val, baseVal);
                if (baseVal > 0 && (ulong)baseVal > MaxValue)
                    MaxValue = (ulong)baseVal;
            }
        }

        public bool TryFromKey(ReadOnlyMemory<byte> key, out T value)
            => TryFromKey(key.Span, out value);
        public bool TryFromKey(ReadOnlySpan<byte> key, out T value)
        {
            if (IsFlagsEnum)
                throw new NotSupportedException("TryFromKey is not support on a Flags enum");
            return _utf8KeyToValue.TryGetValue(key, out value);
        }

        public string ToUtf16Key(T value)
        {
            if (!IsFlagsEnum && _valueToKey.TryGetValue(value, out var key))
                return key;
            return value.ToString();
        }
        public Utf8String ToUtf8Key(T value)
        {
            if (!IsFlagsEnum && _valueToUtf8Key.TryGetValue(value, out var key))
                return key;
            return new Utf8String(value.ToString());
        }

        public abstract T FromInt64(long value);
        public abstract T FromUInt64(ulong value);

        public abstract long ToInt64(T value);
        public abstract ulong ToUInt64(T value);

        //    public T FromInt64(long value)
        //    {
        //        if (!IsFlagsEnum && _intToValue.TryGetValue(value, out var enumValue))
        //            return enumValue;
        //        return (T)(ValueType)value;
        //    }
        //    public T FromUInt64(ulong value)
        //    {
        //        if (!IsFlagsEnum && _intToValue.TryGetValue((long)value, out var enumValue))
        //            return enumValue;
        //        return (T)(ValueType)value;
        //    }

        //    public long ToInt64(T value)
        //    {
        //        if (!IsFlagsEnum && _valueToInt.TryGetValue(value, out var intValue))
        //            return intValue;
        //        return (long)(ValueType)value;
        //    }
        //    public ulong ToUInt64(T value)
        //    {
        //        if (!IsFlagsEnum && _valueToInt.TryGetValue(value, out var intValue))
        //            return (ulong)intValue;
        //        return (ulong)(ValueType)value;
        //    }
    }

    // TODO: How do we avoid boxing? Does JIT optimize this?
    internal class SByteEnumMap<T> : EnumMap<T>
        where T : struct, Enum
    {
        public override T FromInt64(long value) => (T)(ValueType)(sbyte)value;
        public override long ToInt64(T value) => (sbyte)(ValueType)value;

        public override T FromUInt64(ulong value)
            => FromInt64((long)value);
        public override ulong ToUInt64(T value)
            => (ulong)ToInt64(value);
    }
    internal class Int16EnumMap<T> : EnumMap<T>
        where T : struct, Enum
    {
        public override T FromInt64(long value) => (T)(ValueType)(short)value;
        public override long ToInt64(T value) => (short)(ValueType)value;

        public override T FromUInt64(ulong value)
            => FromInt64((long)value);
        public override ulong ToUInt64(T value)
            => (ulong)ToInt64(value);
    }
    internal class Int32EnumMap<T> : EnumMap<T>
        where T : struct, Enum
    {
        public override T FromInt64(long value) => (T)(ValueType)(int)value;
        public override long ToInt64(T value) => (int)(ValueType)value;

        public override T FromUInt64(ulong value)
            => FromInt64((long)value);
        public override ulong ToUInt64(T value)
            => (ulong)ToInt64(value);
    }
    internal class Int64EnumMap<T> : EnumMap<T>
        where T : struct, Enum
    {
        public override T FromInt64(long value) => (T)(ValueType)(long)value;
        public override long ToInt64(T value) => (long)(ValueType)value;

        public override T FromUInt64(ulong value)
            => FromInt64((long)value);
        public override ulong ToUInt64(T value)
            => (ulong)ToInt64(value);
    }

    internal class ByteEnumMap<T> : EnumMap<T>
        where T : struct, Enum
    {
        public override T FromUInt64(ulong value) => (T)(ValueType)(byte)value;
        public override ulong ToUInt64(T value) => (byte)(ValueType)value;

        public override T FromInt64(long value)
            => FromUInt64((ulong)value);
        public override long ToInt64(T value)
            => (long)ToUInt64(value);
    }
    internal class UInt16EnumMap<T> : EnumMap<T>
        where T : struct, Enum
    {
        public override T FromUInt64(ulong value) => (T)(ValueType)(ushort)value;
        public override ulong ToUInt64(T value) => (ushort)(ValueType)value;

        public override T FromInt64(long value)
            => FromUInt64((ulong)value);
        public override long ToInt64(T value)
            => (long)ToUInt64(value);
    }
    internal class UInt32EnumMap<T> : EnumMap<T>
        where T : struct, Enum
    {
        public override T FromUInt64(ulong value) => (T)(ValueType)(uint)value;
        public override ulong ToUInt64(T value) => (uint)(ValueType)value;

        public override T FromInt64(long value)
            => FromUInt64((ulong)value);
        public override long ToInt64(T value)
            => (long)ToUInt64(value);
    }
    internal class UInt64EnumMap<T> : EnumMap<T>
        where T : struct, Enum
    {
        public override T FromUInt64(ulong value) => (T)(ValueType)(ulong)value;
        public override ulong ToUInt64(T value) => (ulong)(ValueType)value;

        public override T FromInt64(long value)
            => FromUInt64((ulong)value);
        public override long ToInt64(T value)
            => (long)ToUInt64(value);
    }
}
