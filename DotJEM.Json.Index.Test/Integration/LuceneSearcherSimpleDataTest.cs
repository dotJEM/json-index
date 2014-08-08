using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Test.Constraints;
using DotJEM.Json.Index.Test.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    //public class Class1
    //{
    //    public void Configuration()
    //    {
    //        //builder.Container.Register(Component.For<IContentDataService>().ImplementedBy<ContentDataService>().LifestyleSingleton());
    //        //builder.Container.Register(Component.For<ILuceneWriter>().ImplementedBy<LuceneIndexWriter>().LifestyleTransient());
    //        //builder.Container.Register(Component.For<IDocumentFactory>().ImplementedBy<LuceneDocumentFactory>().LifestyleTransient());
    //        //builder.Container.Register(Component.For<IIndexStorage>().ImplementedBy<LuceneStorage>().LifestyleSingleton());
    //        //builder.Container.Register(Component.For<IFieldCollection>().ImplementedBy<LuceneFieldCollectionCollection>().LifestyleSingleton());
    //        //builder.Container.Register(Component.For<ILuceneSearcher>().ImplementedBy<LuceneSearcher>().LifestyleSingleton());
    //        //builder.Container.Register(Component.For<IQueryBuilder>().ImplementedBy<LuceneQueryBuilder>().LifestyleSingleton());

    //        //builder.Container.Register(Component.For<IStorageIndex>().ImplementedBy<LuceneStorageIndex>().LifestyleSingleton());

    //        //builder.Container.Register(Component.For<IFieldFactory>().ImplementedBy<FieldFactory>().LifestyleSingleton());
    //        //builder.Container.Register(Component.For<IIndexConfiguration>().ImplementedBy<IndexConfiguration>().LifestyleSingleton());

    //        IStorageIndex init = new LuceneStorageIndex();
    //        init.Configuration.SetTypeResolver("$contentType")
    //            .ForAll()
    //            .SetIdentity("$id")

    //            //Index Strategies for all types
    //            .Index("$contentType", As.Default().Analyzed(Field.Index.NOT_ANALYZED))
    //            .Index("$created", As.Numeric((f, v) => f.SetLongValue(v.Value<DateTime>().Ticks)))
    //            .Index("$updated", As.Numeric((f, v) => f.SetLongValue(v.Value<DateTime>().Ticks)))

    //            //Query Strategies for all types

    //            .Query("$created", As.Range().When.Specified())
    //            .Query("$updated", As.Term().When.Specified());
    //    }

    //}

    [TestFixture]
    public class LuceneSearcherSimpleDataTest
    {
        private readonly IStorageIndex index = new LuceneStorageIndex();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = index.Configuration;
            config.SetTypeResolver("Type")
                .ForAll().SetIdentity("Id");

            //config.For("Car").Index("Model", As.Default().Analyzed(Field.Index.NOT_ANALYZED))
            //                 .Query("Model", Using.Term().When.Always());

            ILuceneWriter writer = index.CreateWriter();
            writer.Write(JObject.FromObject(new { Id = 1, Type = "Person", Name = "John", LastName = "Doe" }));
            writer.Write(JObject.FromObject(new { Id = 2, Type = "Person", Name = "Peter", LastName = "Pan" }));
            writer.Write(JObject.FromObject(new { Id = 3, Type = "Person", Name = "Alice" }));

            writer.Write(JObject.FromObject(new { Id = 4, Type = "Car", Brand = "Ford", Model = "Mustang" }));
            writer.Write(JObject.FromObject(new { Id = 5, Type = "Car", Brand = "Dodge", Model = "Charger" }));
            writer.Write(JObject.FromObject(new { Id = 6, Type = "Car", Brand = "Chevrolet", Model = "Camaro" }));

            writer.Write(JObject.FromObject(new { Id = 7, Type = "Flower", Name = "Lilly", Meaning = "Majesty" }));
            writer.Write(JObject.FromObject(new { Id = 8, Type = "Flower", Name = "Freesia", Meaning = "Innocence" }));
            writer.Write(JObject.FromObject(new { Id = 9, Type = "Flower", Name = "Aster", Meaning = "Patience" }));
        }

        [Test]
        public void Search_ForMustangWithSpecifiedFields_ReturnsCarMustang()
        {
            List<dynamic> result = index.CreateSearcher().Search("Mustang", "Model".Split(',')).Select(hit=>hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 4, Type: 'Car', Brand: 'Ford', Model: 'Mustang' }"))));
        }

        [Test]
        public void Search_ForMustang_ReturnsCarMustang()
        {
            List<dynamic> result = index.CreateSearcher().Search("Mustang").Select(hit => hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 4, Type: 'Car', Brand: 'Ford', Model: 'Mustang' }"))));
        }

        [Test]
        public void Search_ForMustang3_ReturnsCarMustang()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new WildcardQuery(new Term("Model", "Mustang*")), Occur.SHOULD);
            query.Add(new FuzzyQuery(new Term("Model", "Mustang")), Occur.SHOULD);

            List<dynamic> result = index.CreateSearcher().Search(query).Select(hit => hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 4, Type: 'Car', Brand: 'Ford', Model: 'Mustang' }"))));
        }
    }
}
