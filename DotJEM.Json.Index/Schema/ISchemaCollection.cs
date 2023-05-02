﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Schema
{
    public interface ISchemaCollection : IEnumerable<JSchema>
    {
        IEnumerable<string> ContentTypes { get; }
        JSchema this[string contentType] { get; set; }

        JsonSchemaExtendedType ExtendedType(string field);
        IEnumerable<string> AllFields();
        IEnumerable<string> Fields(string contentType);

        JSchema Add(string contentType, JSchema schema);
    }

    public class SchemaCollection : ISchemaCollection
    {
        private readonly IDictionary<string, JSchema> schemas = new ConcurrentDictionary<string, JSchema>();

        public IEnumerable<string> ContentTypes { get { return schemas.Keys; } }

        public JSchema this[string contentType]
        {
            get
            {
                return schemas.ContainsKey(contentType) ? schemas[contentType] : null;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentNullException("contentType");
                if (value == null) throw new ArgumentNullException("value");

                schemas[contentType] = value;
            }
        }

        public JSchema Add(string contentType, JSchema schema)
        {
            if (contentType == null) throw new ArgumentNullException("contentType");
            if (schema == null) throw new ArgumentNullException("schema");

            schema.ContentType = contentType;
            if (schemas.ContainsKey(contentType))
            {
                return this[contentType] = this[contentType].Merge(schema);
            }
            schemas.Add(contentType, schema);
            return schema;
        }

        public IEnumerable<string> AllFields()
        {
            return schemas.Values
                .SelectMany(s => s.Traverse())
                .Select(s => s.Field)
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct();
        }

        public JsonSchemaExtendedType ExtendedType(string field)
        {
            return schemas.Aggregate(JsonSchemaExtendedType.None,
                (types, next) => next.Value.LookupExtentedType(field) | types);
        }

        public IEnumerable<string> Fields(string contentType)
        {
            JSchema schema = this[contentType];

            return schema == null
                ? Enumerable.Empty<string>()
                : schema.Traverse().Select(s => s.Field).Where(f => !string.IsNullOrEmpty(f));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<JSchema> GetEnumerator()
        {
            return schemas.Values.GetEnumerator();
        }
    }
}