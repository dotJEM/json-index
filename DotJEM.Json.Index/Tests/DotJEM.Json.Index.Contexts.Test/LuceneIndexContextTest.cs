using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.QueryParsers;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.TestData;
using DotJEM.Json.Index.TestUtil;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Contexts.Test
{
    [TestFixture]
    public class LuceneIndexContextTest
    {
        [Test]
        public void Search_AcrossAllIndexes_ReturnsFromEach()
        {
            ILuceneIndexContext context = new TestIndexContextBuilder()
                .WithIndex("albums", builder => builder.With(JsonPlaceholder.Albums.Select(data => new TestObject("album", (JObject) data))))
                .WithIndex("comments", builder => builder.With(JsonPlaceholder.Comments.Select(data => new TestObject("comment", (JObject) data))))
                .WithIndex("photos", builder => builder.With(JsonPlaceholder.Photos.Select(data => new TestObject("photo", (JObject) data))))
                .WithIndex("posts", builder => builder.With(JsonPlaceholder.Posts.Select(data => new TestObject("post", (JObject) data))))
                .WithIndex("todos", builder => builder.With(JsonPlaceholder.Todos.Select(data => new TestObject("todo", (JObject) data))))
                .WithIndex("users", builder => builder.With(JsonPlaceholder.Users.Select(data => new TestObject("user", (JObject) data))))
                .Build().Result;

            using (ILuceneJsonIndexSearcher searcher = context.CreateSearcher())
            {
                Query q = NumericRangeQuery.NewInt64Range("id", 0, 2, false, false);
                //q = new MatchAllDocsQuery();
                SearchResults result = searcher.Search(q).Results.Result;

                Assert.That(result, 
                    Has.Exactly(1).Matches<ISearchResult>(hit => hit.Entity["$contentType"].Value<string>() == "album")
                    & Has.Exactly(1).Matches<ISearchResult>(hit => hit.Entity["$contentType"].Value<string>() == "comment")
                    & Has.Exactly(1).Matches<ISearchResult>(hit => hit.Entity["$contentType"].Value<string>() == "photo")
                    & Has.Exactly(1).Matches<ISearchResult>(hit => hit.Entity["$contentType"].Value<string>() == "post")
                    & Has.Exactly(1).Matches<ISearchResult>(hit => hit.Entity["$contentType"].Value<string>() == "todo")
                    & Has.Exactly(1).Matches<ISearchResult>(hit => hit.Entity["$contentType"].Value<string>() == "user")
                    );


            }
        }
    }
}
