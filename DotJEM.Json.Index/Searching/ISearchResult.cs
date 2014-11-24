using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Searching
{
    public interface ISearchResult : IEnumerable<IHit>
    {
        long TotalCount { get; }

        ISearchResult Take(int count);
        ISearchResult Skip(int count);
        
        //TODO: Filter Builder instead.
        ISearchResult Filter(Filter filtering);

        //TODO: Sort Builder instead.
        ISearchResult Sort(Sort sorting);

        //TODO: Sort Builder instead.
        ISearchResult All();
    }

    public class SearchResultCollector : ISearchResult
    {
        private int take = 25, skip;
        private Filter filtering;
        private Sort sorting;
        private Query query;
        private IndexSearcher searcher;

        private readonly IStorageIndex index;

        public long TotalCount { get; private set; }

        public SearchResultCollector(Query query, IStorageIndex index)
        {
            this.query = query;
            this.index = index;
        }
        
        private JObject ResolveJObject(IHit hit)
        {
            Document document = searcher.Doc(hit.Doc);
            dynamic json = JObject.Parse(document.GetField(index.Configuration.RawField).StringValue);
            json[index.Configuration.ScoreField] = hit.Score;
            return json;
        }

        public ISearchResult Take(int count)
        {
            take = count;
            return this;
        }

        public ISearchResult Skip(int count)
        {
            skip = count;
            return this;
        }

        public ISearchResult Filter(Filter value)
        {
            filtering = value;
            return this;
        }

        public ISearchResult Sort(Sort value)
        {
            sorting = value;
            return this;
        }

        public ISearchResult All()
        {
            take = int.MaxValue;
            return this;
        }

        public IEnumerator<IHit> GetEnumerator()
        {
            if(!index.Storage.Exists)
                yield break;

            using (searcher = new IndexSearcher(index.Storage.OpenReader()))
            {
                query = searcher.Rewrite(query);

                TopDocs hits = sorting == null
                    ? searcher.Search(query, filtering, take + skip)
                    : searcher.Search(query, filtering, take + skip, sorting);

                TotalCount = hits.TotalHits;

                foreach (ScoreDoc hit in hits.ScoreDocs.Skip(skip))
                    yield return new Hit(hit.Doc, hit.Score, ResolveJObject);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}