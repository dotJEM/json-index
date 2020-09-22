using System.Data;
using System.Runtime.CompilerServices;
using Lucene.Net.Analysis;
using Lucene.Net.Util;

namespace DotJEM.Json.Index.Configuration
{
    public interface IJsonIndexConfiguration
    {
        LuceneVersion Version { get; set; }

        Analyzer Analyzer { get; set; }
    }

    public class JsonIndexConfiguration : IJsonIndexConfiguration
    {
        public LuceneVersion Version { get; set; }
        public Analyzer Analyzer { get; set; }
    }

    public class ReadOnlyJsonIndexConfiguration : IJsonIndexConfiguration
    {
        private readonly IJsonIndexConfiguration inner;

        public ReadOnlyJsonIndexConfiguration(IJsonIndexConfiguration inner)
        {
            this.inner = inner;
        }

        public LuceneVersion Version
        {
            get => inner.Version;
            set => BlockWrite();
        }

        public Analyzer Analyzer
        {
            get => inner.Analyzer;
            set => BlockWrite();
        }

        private void BlockWrite([CallerMemberName]string propertyName = null)
        {
            throw new ReadOnlyException($"Cannot write property {propertyName} on a {nameof(ReadOnlyJsonIndexConfiguration)}.");
        }
    }

    public static class JsonIndexConfigurationExtentions
    {
        /// <summary>
        /// Returns a readonly version of a <see cref="IJsonIndexConfiguration"/>.
        /// </summary>
        /// <remarks>
        /// If either null or an already readonly <see cref="IJsonIndexConfiguration"/> is passed to this method
        /// it just returns the object passed in.
        /// Otherwise it wraps the <see cref="IJsonIndexConfiguration"/> passed into this method in a readonly wrapper.
        ///
        /// </remarks>
        /// <param name="self">The <see cref="IJsonIndexConfiguration"/> to return as readonly.</param>
        /// <returns>
        /// If <see cref="self"/> is null or already readonly, it is returned. Otherwise a readonly wrapper returned.
        /// </returns>
        public static IJsonIndexConfiguration AsReadOnly(this IJsonIndexConfiguration self)
        {
            return self == null || self is ReadOnlyJsonIndexConfiguration
                ? self
                : new ReadOnlyJsonIndexConfiguration(self);
        }
    }

}