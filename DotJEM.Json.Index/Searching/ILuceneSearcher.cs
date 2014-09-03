using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Searching
{
    public interface ILuceneSearcher
    {
        ISearchResult Search(Query query);
        ISearchResult Search(string value);
        ISearchResult Search(JObject query, string contentType = "");
        ISearchResult Search(string query, string contentType);
        ISearchResult Search(string query, IEnumerable<string> fields, string contentType = "");

        IEnumerable<string> Terms(string field);
    }

    public class LuceneSearcher : ILuceneSearcher
    {
        private readonly IStorageIndex index;
        private readonly IQueryBuilder queryBuilder;

        public LuceneSearcher(IStorageIndex index)
            : this(index, new LuceneQueryBuilder(index.Configuration))
        {
        }

        public LuceneSearcher(IStorageIndex index, IQueryBuilder queryBuilder)
        {
            this.index = index;
            this.queryBuilder = queryBuilder;
        }

        public ISearchResult Search(Query query)
        {
            return InternalSearch(query);
        }

        public ISearchResult Search(string value)
        {
            return Search(queryBuilder.Build(value));
        }

        public ISearchResult Search(string value, string contentType)
        {
            return Search(value, index.Fields.AllFields(), contentType);
        }

        public ISearchResult Search(string text, IEnumerable<string> fields, string contentType = "")
        {
            return Search(queryBuilder.Build(text, fields, contentType));
        }

        public ISearchResult Search(JObject query, string contentType = "")
        {
            return Search(queryBuilder.Build(query, contentType));
        }

        private ISearchResult InternalSearch(Query query)
        {
            return new SearchResultCollector(query, index);
        }

        public IEnumerable<string> Terms(string field)
        {
            if (!index.Storage.Exists)
                yield break;

            using (IndexReader reader = index.Storage.OpenReader())
            {
                TermEnum terms = reader.Terms(new Term(field));
                do
                {
                    if (terms.Term.Field != field)
                        yield break;
                    yield return terms.Term.Text;
                } while (terms.Next());

            }
        }
        //private dynamic CreateJson(dynamic hit, Query queryBuilder)
        //{
        //    string content = hit.Document.GetField("html").StringValue;
        //    string title = hit.Document.GetField("title").StringValue;

        //    dynamic json = new JObject();
        //    json.Id = hit.Id;
        //    json.Content = new JsonRaw(hit.Document.GetField("json").StringValue);
        //    json.Text = FragmentContent(queryBuilder, content);
        //    json.Title = FragmentTitle(queryBuilder, title);
        //    json.Score = hit.Score;
        //    return json;
        //}

        //public string FragmentContent(Query queryBuilder, string text)
        //{
        //    QueryScorer scorer = new QueryScorer(queryBuilder);
        //    SimpleHTMLFormatter formatter = new SimpleHTMLFormatter("<span class=\"hit\">", "</span>");
        //    Highlighter highlighter = new Highlighter(formatter, scorer);
        //    highlighter.TextFragmenter = new SimpleFragmenter(60);

        //    return highlighter.GetBestFragments(analyzer.ReusableTokenStream("text", new StringReader(text)), text, 8, "........ ");
        //}

        //public string FragmentTitle(Query queryBuilder, string text)
        //{
        //    QueryScorer scorer = new QueryScorer(queryBuilder);
        //    SimpleHTMLFormatter formatter = new SimpleHTMLFormatter("<span class=\"hit\">", "</span>");
        //    Highlighter highlighter = new Highlighter(formatter, scorer);
        //    highlighter.TextFragmenter = new SimpleFragmenter(256);

        //    var fragment = highlighter.GetBestFragments(analyzer.ReusableTokenStream("text", new StringReader(text)), text, 1, "");
        //    return fragment.Length > 0 ? fragment : text;
        //}
    }
}