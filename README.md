[![Build status](https://ci.appveyor.com/api/projects/status/y64ia7mb9e3uxks3/branch/master?svg=true)](https://ci.appveyor.com/project/jeme/json-index/branch/master)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FdotJEM%2Fjson-index.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FdotJEM%2Fjson-index?ref=badge_shield)

dotJEM JSON Index
=================

!! This is a Work in progress and many lessons has been learned for the first version which the a next version will use.

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


Configuration
=============

It is possible to configure the indexing for different ways of doing Document identification etc.

Document identification is important for Updating documents, Content type is what automated schema generation is based around.

```C#
IStorageIndex index = new LuceneStorageIndex();
var config = index.Configuration;
config
    // Set how the Type of a document is identified, ContentType, Type, SchemaType or similar, this is used to categorize data
    // and update associated Schemas as data goes into the index.
    .SetTypeResolver("Type")
    // This describes a document source, this is will be deprecated in the future.
    .SetAreaResolver("Area")
    // At this point we begin to target data based on their categorization (Type), we can use "ForAll" to say that this goes for all
    // Documents of any type or For("Type") to target specifit types.
    .ForAll()
    // Sets how documents are identified, this is used to update rather than add documents when they are allready in the index.
    // Specifically.
    .SetIdentity("Id");
```

What is next?
=============

So far this framework has proved to simplify allot of things for us, but parts of it are still viewed as a prototype from our perspective, the core works well and we use that in production but there are allot of unfinished edges.

The plan going forward is to move up to Lucene 4.8 and with the allot of changes will happen, I am looking into making the framework more mudular in terms of packages and give patterns than provide better means to extend it with own query parser logic, document creation etc...


## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FdotJEM%2Fjson-index.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FdotJEM%2Fjson-index?ref=badge_large)
