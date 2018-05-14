using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Fields
{
    public interface IFieldResolver
    {
        string IndentityField { get; }
        string ContentTypeField { get; }
        string StorageField { get; }

        string ContentType(JObject entity);
        Term Identity(JObject entity);
    }

    public class FieldResolver : IFieldResolver
    {
        public string IndentityField { get; }
        public string ContentTypeField { get; }
        public string StorageField { get; }

        private readonly Func<JObject, Term> indentityFieldLookup;
        private readonly Func<JObject, string> contentTypeFieldLookup;

        public FieldResolver(string indentityField = "$id", string contentTypeField = "$contentType", string storageField = "$$RAW")
        {
            IndentityField = indentityField;
            ContentTypeField = contentTypeField;
            StorageField = storageField;
            indentityFieldLookup = UseSelect(indentityField)
                ? new Func<JObject, Term>(obj => new Term(indentityField, (string)obj.SelectToken(indentityField)))
                : new Func<JObject, Term>(obj => new Term(indentityField, (string)obj[indentityField]));

            contentTypeFieldLookup = UseSelect(contentTypeField)
                ? new Func<JObject, string>(obj => (string)obj.SelectToken(contentTypeField))
                : new Func<JObject, string>(obj => (string)obj[contentTypeField]);
        }

        private static bool UseSelect(string indentityField)
        {
            return indentityField.Contains(".") || indentityField.Contains("[");
        }

        public string ContentType(JObject entity) => contentTypeFieldLookup(entity);

        public Term Identity(JObject entity) => indentityFieldLookup(entity);
    }
}
