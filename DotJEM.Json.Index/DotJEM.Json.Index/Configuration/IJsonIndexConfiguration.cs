using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Info;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace DotJEM.Json.Index.Configuration
{
    public interface IJsonIndexConfiguration
    {
        LuceneVersion Version { get; set; }
    }

    public class JsonIndexConfiguration : IJsonIndexConfiguration
    {
        public LuceneVersion Version { get; set; }

    }


}