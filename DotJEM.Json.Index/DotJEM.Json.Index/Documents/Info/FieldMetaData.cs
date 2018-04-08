using System;
using System.Linq;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Info
{
    public interface IFieldMetaData
    {
        string Key { get; }
    }

    public class FieldMetaData : IFieldMetaData
    {
        private readonly IndexFlags flags;

        public string Key { get; }
        public FieldType FieldType { get; }
        public JTokenType TokenType { get; }
        public Type Type { get; }

        public FieldMetaData(string root, FieldType fieldType, JTokenType tokenType, Type type)
        {
            FieldType = fieldType;
            TokenType = tokenType;
            Type = type;
            flags = new IndexFlags
            {
                IsIndexed = fieldType.IsIndexed,
                IsStored = fieldType.IsStored,
                IsTokenized = fieldType.IsTokenized,
                OmitNorms = fieldType.OmitNorms,
                StoreTermVectorOffsets = fieldType.StoreTermVectorOffsets,
                StoreTermVectorPayloads = fieldType.StoreTermVectorPayloads,
                StoreTermVectorPositions = fieldType.StoreTermVectorPositions,
                StoreTermVectors = fieldType.StoreTermVectors
            };

            Key =
                $"{root};{(int)fieldType.DocValueType};{(int)fieldType.IndexOptions};{(int)fieldType.IndexOptions};{(int)fieldType.DocValueType};" +
                $"{flags.Base64};{fieldType.NumericPrecisionStep};{(int)tokenType};{type}";
        }

        public override string ToString() => Key;

        private struct IndexFlags
        {
            private byte Value { get; set; }

            public bool IsIndexed
            {
                get => GetBit(BIT_MASK_0);
                set => SetBit(BIT_MASK_0, value);
            }

            public bool IsStored
            {
                get => GetBit(BIT_MASK_1);
                set => SetBit(BIT_MASK_1, value);
            }

            public bool IsTokenized
            {
                get => GetBit(BIT_MASK_2);
                set => SetBit(BIT_MASK_2, value);
            }

            public bool OmitNorms
            {
                get => GetBit(BIT_MASK_3);
                set => SetBit(BIT_MASK_3, value);
            }

            public bool StoreTermVectorOffsets
            {
                get => GetBit(BIT_MASK_4);
                set => SetBit(BIT_MASK_4, value);
            }

            public bool StoreTermVectorPayloads
            {
                get => GetBit(BIT_MASK_5);
                set => SetBit(BIT_MASK_5, value);
            }

            public bool StoreTermVectorPositions
            {
                get => GetBit(BIT_MASK_6);
                set => SetBit(BIT_MASK_6, value);
            }

            public bool StoreTermVectors
            {
                get => GetBit(BIT_MASK_7);
                set => SetBit(BIT_MASK_7, value);
            }

            public string Base64
            {
                get => Base64Encode();
                set => Base64Decode(value);
            }
            private void Base64Decode(string str)
            {
                Value = Convert.FromBase64String(str).Single();
            }

            private string Base64Encode()
            {
                return Convert.ToBase64String(new[] { Value });
            }

            private void SetBit(byte mask, bool on)
            {
                Value = (byte)(on ? (Value | mask) : (Value & ~mask));
            }

            private bool GetBit(byte mask)
            {
                return (Value & mask) != 0;
            }

            private const byte BIT_MASK_0 = 1 << 0;
            private const byte BIT_MASK_1 = 1 << 1;
            private const byte BIT_MASK_2 = 1 << 2;
            private const byte BIT_MASK_3 = 1 << 3;
            private const byte BIT_MASK_4 = 1 << 4;
            private const byte BIT_MASK_5 = 1 << 5;
            private const byte BIT_MASK_6 = 1 << 6;
            private const byte BIT_MASK_7 = 1 << 7;
        }

    }
}