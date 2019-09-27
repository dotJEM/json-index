using System;
using Lucene.Net.Documents;

namespace DotJEM.Json.Index.Documents.Info
{

    [Flags]
    public enum LuceneFieldFlags : byte
    {
        None = 0,
        IsIndexed = 1 << 0,
        IsStored = 1 << 1,
        IsTokenized = 1 << 2,
        OmitNorms = 1 << 3,
        StoreTermVectorOffsets = 1 << 4,
        StoreTermVectorPayloads = 1 << 5,
        StoreTermVectorPositions = 1 << 6,
        StoreTermVectors = 1 << 7
    }

    public static class FieldTypeExtensions
    {
        public static LuceneFieldFlags GetFlags(this FieldType field)
        {
            LuceneFieldFlags flags = LuceneFieldFlags.None;
            if (field.IsIndexed)
                flags |= LuceneFieldFlags.IsIndexed;
            if (field.IsStored)
                flags |= LuceneFieldFlags.IsStored;
            if (field.IsTokenized)
                flags |= LuceneFieldFlags.IsTokenized;
            if (field.OmitNorms)
                flags |= LuceneFieldFlags.OmitNorms;
            if (field.StoreTermVectorOffsets)
                flags |= LuceneFieldFlags.StoreTermVectorOffsets;
            if (field.StoreTermVectorPayloads)
                flags |= LuceneFieldFlags.StoreTermVectorPayloads;
            if (field.StoreTermVectorPositions)
                flags |= LuceneFieldFlags.StoreTermVectorPositions;
            if (field.StoreTermVectors)
                flags |= LuceneFieldFlags.StoreTermVectors;
            return flags;
        }

        public static string GetBase64Flags(this FieldType field)
        {
            return Convert.ToBase64String(new[] { (byte)field.GetFlags() });
        }
    }
}