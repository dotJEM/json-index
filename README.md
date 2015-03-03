dotJEM JSON Index
=================

Handles indexing of any arbitrary JSON objects based in Lucene.NET.

This basically enables the following concept:

```C#
IStorageIndex index = new LuceneStorageIndex();
index
    .Write(JObject.Parse("{ $id: '00000000-0000-0000-0000-000000000001', $contentType: 'person', name: 'Peter', age: 20 }"))
    .Write(JObject.Parse("{ $id: '00000000-0000-0000-0000-000000000002', $contentType: 'person', name: 'Lars', age: 30 }"))
    .Write(JObject.Parse("{ $id: '00000000-0000-0000-0000-000000000003', $contentType: 'person', name: 'John', age: 42 }"));

Assert.That(
    index.Search("name: Peter").Select(hit => hit.Json).Single(),
    Is.EqualTo(JObject.Parse("{ $id: '00000000-0000-0000-0000-000000000001', $contentType: 'person', name: 'Peter', age: 20 }")));

Assert.That(
    index.Search("age: [40 TO 50]").Select(hit => hit.Json).Single(),
    Is.EqualTo(JObject.Parse("{ $id: '00000000-0000-0000-0000-000000000003', $contentType: 'person', name: 'John', age: 42 }")));

Assert.That(
    index.Search("$contentType: person").ToArray().Length,
    Is.EqualTo(3));
```
