using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Index.Searching;
using DotJEM.Json.Index.Serialization;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Results
{
    public interface ISearchHit
    {
        float Score { get; }
        dynamic Json { get; }
        JObject Entity { get; }
    }

    public class SearchHit : ISearchHit
    {
        public SearchHit(float score, byte[] value)
        {
            Score = score;
            Entity = new GZipJsonSerialier().Deserialize(value);
        }

        public float Score { get; }
        public dynamic Json => Entity;
        public JObject Entity { get; }
    }


    public interface ISearch : IEnumerable<ISearchHit>
    {
    }

    public sealed  class Search : ISearch
    {
        private readonly IIndexSearcherManager manager;

        private readonly int skip, take;
        private readonly Query query;
        private readonly Filter filter;
        private readonly bool doDocScores;
        private readonly bool doMaxScores;
        private readonly Sort sort;

        public Search Take(int newTake) => new Search(manager, query, skip, newTake, sort, filter, doDocScores, doMaxScores);
        public Search Skip(int newSkip) => new Search(manager, query, newSkip, take, sort, filter, doDocScores, doMaxScores);
        public Search Query(Query newQuery) => new Search(manager, newQuery, skip, take, sort, filter, doDocScores, doMaxScores);
        public Search OrderBy(Sort newSort) => new Search(manager, query, skip, take, newSort, filter, doDocScores, doMaxScores);
        public Search Filter(Filter newFilter) => new Search(manager, query, skip, take, sort, newFilter, doDocScores, doMaxScores);

        public Search WithoutDocScores() => new Search(manager, query, skip, take, sort, filter, false, doMaxScores);
        public Search WithoutMaxScores() => new Search(manager, query, skip, take, sort, filter, doDocScores, false);
        public Search WithoutScores() => new Search(manager, query, skip, take, sort, filter, false, false);

        public Search WithDocScores() => new Search(manager, query, skip, take, sort, filter, true, doMaxScores);
        public Search WithMaxScores() => new Search(manager, query, skip, take, sort, filter, doDocScores, true);
        public Search WithScores() => new Search(manager, query, skip, take, sort, filter, true, true);

        public Search(IIndexSearcherManager manager, Query query = null, int skip = 0, int take = 25, Sort sort = null, Filter filter = null, bool doDocScores = true, bool doMaxScores = true)
        {
            this.manager = manager;
            this.skip = skip;
            this.take = take;
            this.query = query;
            this.filter = filter;
            this.doDocScores = doDocScores;
            this.doMaxScores = doMaxScores;
            this.sort = sort ?? Sort.RELEVANCE;
        }

        public int Count => Execute(manager, query, 0, 1, null, filter, false, false).TotalHits;
        public SearchResults Result => Execute(manager, query, skip, take, sort, filter, doDocScores, doMaxScores);

        private static SearchResults Execute(IIndexSearcherManager manager, Query query, int skip, int take, Sort sort, Filter filter, bool doDocScores, bool doMaxScores)
        {
            Stopwatch timer = Stopwatch.StartNew();
            using (IIndexSearcherContext context = manager.Acquire())
            {
                IndexSearcher s = context.Searcher;
                query = s.Rewrite(query);
                //TODO: Remove or replace with INFO stream.
                Console.WriteLine(query);
                TopFieldDocs results = Task.Run(() => s.Search(query, filter, take, sort, doDocScores, doMaxScores)).Result;
                var searchTime = timer.Elapsed;
                IEnumerable<Task<ISearchHit>> tasks = results.ScoreDocs.Skip(skip).Select(hit =>
                {
                    return Task.Run(() =>
                    {
                        //TODO: Lazy + Fetch serializer
                        Document document = s.Doc(hit.Doc);
                        return (ISearchHit)new SearchHit(hit.Score, document.GetBinaryValue("$$RAW").Bytes);
                    });
                });
                ISearchHit[] r = Task.WhenAll(tasks).ConfigureAwait(false).GetAwaiter().GetResult();
                return new SearchResults(r, results.TotalHits, searchTime, timer.Elapsed);
            }
        }

        public IEnumerator<ISearchHit> GetEnumerator()
        {
            return Result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class SearchResults : IEnumerable<ISearchHit>
    {
        public ISearchHit[] Hits { get; }
        public int TotalHits { get; }

        //TODO: These should be in info streams.
        public TimeSpan SearchTime { get; }
        public TimeSpan LoadTime => TotalTime - SearchTime;
        public TimeSpan TotalTime { get; }

        public SearchResults(ISearchHit[] hits, int totalHits, TimeSpan searchTime, TimeSpan totalTime)
        {
            Hits = hits;
            TotalHits = totalHits;
            SearchTime = searchTime;
            TotalTime = totalTime;
        }


        public IEnumerator<ISearchHit> GetEnumerator() => Hits.AsEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator SearchResults(Search search) => search.Result;
    }
}
