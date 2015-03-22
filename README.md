dotJEM JSON Index
=================

Handles indexing of any arbitrary JSON objects based in Lucene.NET.

This basically enables the following concept:

```C#
IStorageIndex index = new LuceneStorageIndex();
index
    .Write(JObject.Parse("{ $id: '...', $contentType: 'person', name: 'Peter', age: 20 }"))
    .Write(JObject.Parse("{ $id: '...', $contentType: 'person', name: 'Lars', age: 30 }"))
    .Write(JObject.Parse("{ $id: '...', $contentType: 'person', name: 'John', age: 42 }"));

Assert.That(
    index.Search("name: Peter").Select(hit => hit.Json).Single(),
    Is.EqualTo(JObject.Parse("{ $id: '...', $contentType: 'person', name: 'Peter', age: 20 }")));

Assert.That(
    index.Search("age: [40 TO 50]").Select(hit => hit.Json).Single(),
    Is.EqualTo(JObject.Parse("{ $id: '...', $contentType: 'person', name: 'John', age: 42 }")));

Assert.That(
    index.Search("$contentType: person").ToArray().Length,
    Is.EqualTo(3));
```

Note that `$id` is a GUID, but this is obmitted with `...` in the above for readability. This is a strategy that can be replaced with e.g. a more simple int/long strategy. The name of reserved fields like `$id` and `$contentType` can also be configured with other strategies enabling a high degree of flexibility.
