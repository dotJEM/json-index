using System;
using System.Collections.Generic;
using System.Linq;
using J2N.Collections.ObjectModel;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.Documents.Builder
{
    public interface IIndexableJsonField
    {
        string SourcePath { get; }
        Type SourceType { get; }
        IReadOnlyList<IIndexableField> LuceneFields { get; }
    }

    public class IndexableJsonField<T> : IIndexableJsonField
    {
        public string SourcePath { get; }
        public Type SourceType { get; } = typeof(T);

        public IReadOnlyList<IIndexableField> LuceneFields { get; }

        public IndexableJsonField(string sourcePath, IIndexableField field)
            : this(sourcePath, new [] { field })
        {
        }

        public IndexableJsonField(string sourcePath, IEnumerable<IIndexableField> fields)
        {
            SourcePath = sourcePath;
            LuceneFields = new ReadOnlyList<IIndexableField>(fields.ToList());
        }
    }

}