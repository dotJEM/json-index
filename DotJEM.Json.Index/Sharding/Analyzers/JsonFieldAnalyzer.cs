using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;

namespace DotJEM.Json.Index.Sharding.Analyzers
{
    public sealed class JsonFieldAnalyzer : Analyzer
    {
        private readonly Analyzer @default = new KeywordAnalyzer();

        public JsonFieldAnalyzer()
        {
            #pragma warning disable 618
            //NOTE: overridesTokenStreamMethod is obsolete, but this implementation is taken directly from Lucene
            SetOverridesTokenStreamMethod<PerFieldAnalyzerWrapper>();
            #pragma warning restore 618
        }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return ResolveAnalyzer(fieldName).TokenStream(fieldName, reader);
        }

        private Analyzer ResolveAnalyzer(string fieldName)
        {
            //TODO: (jmd 2015-10-08) Resolve field analyzer from configuration. 
            return @default;
        }

        public override TokenStream ReusableTokenStream(string fieldName, TextReader reader)
        {
            #pragma warning disable 612
            //NOTE: overridesTokenStreamMethod is obsolete, but this implementation is taken directly from Lucene
            return overridesTokenStreamMethod
                ? TokenStream(fieldName, reader)
                : ResolveAnalyzer(fieldName).ReusableTokenStream(fieldName, reader);
            #pragma warning restore 612
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return ResolveAnalyzer(fieldName).GetPositionIncrementGap(fieldName);
        }

        /// <summary>
        /// Return the offsetGap from the analyzer assigned to field
        /// </summary>
        public override int GetOffsetGap(IFieldable field)
        {
            return ResolveAnalyzer(field.Name).GetOffsetGap(field);
        }

        //public override string ToString()
        //{
        //    return "JsonFieldAnalyzer(" + (object)this.analyzerMap + ", default=" + (string)(object)this.defaultAnalyzer + ")";
        //}
    }
}