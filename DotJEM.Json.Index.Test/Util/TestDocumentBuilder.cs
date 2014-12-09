using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Test.Util
{
    public class TestDocumentBuilder
    {
        private readonly JObject json = new JObject();

        private string contentType;
        private Guid id;

        public Guid Id
        {
            get { return id; }
            set
            {
                id = value;
                json["id"] = value;
            }
        }

        public string ContentType
        {
            get { return contentType; }
            set
            {
                contentType = value;
                json["contentType"] = value;
            }
        }

        public TestDocumentBuilder(string contentType, Guid id)
        {
            //TODO: Dummy Data based on ContentType

            ContentType = contentType;
            Id = id;
        }

        public TestDocumentBuilder(Document prototype, Guid id)
            : this(prototype.ContentType, id)
        {
            json.Merge(prototype.Data);
        }

        public TestDocumentBuilder Set(string key, dynamic obj)
        {
            json[key] = obj;
            return this;
        }

        public JObject Build()
        {
            return json;
        }
    }
}