using System;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index.Schema
{
    

    public class JSchemeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JSchema schema = value as JSchema;
            Debug.Assert(schema != null);

            JRootSchema root = value as JRootSchema;
            writer.WriteStartObject();
            if (root != null)
            {
                WriteProperty(writer, serializer, "$schema", root.Schema);
            }
            WriteProperty(writer, serializer, "id", schema.Id);
            WriteTypeEnum(writer, serializer, schema.Type);
            WriteProperty(writer, serializer, "required", schema.Required);
            WriteProperty(writer, serializer, "field", schema.Field);
            WriteProperty(writer, serializer, "title", schema.Title);
            WriteProperty(writer, serializer, "description", schema.Description);

            WriteProperty(writer, serializer, "items", schema.Items);
            WriteProperty(writer, serializer, "properties", schema.Properties);

            writer.WriteEndObject();
        }

        private static void WriteTypeEnum(JsonWriter writer, JsonSerializer serializer, JsonSchemaType value)
        {
            var enumArr = Enum.GetValues(value.GetType())
                .Cast<JsonSchemaType>()
                .Where(x => (x & value) == x)
                .Select(x => Enum.ToObject(value.GetType(), x).ToString().ToLowerInvariant())
                .ToArray();

            if (enumArr.Length > 1)
                enumArr = enumArr.Skip(1).ToArray();

            writer.WritePropertyName("type");
            if (enumArr.Length == 1)
                serializer.Serialize(writer, enumArr.First());
            else
                serializer.Serialize(writer, enumArr);
        }

        private static void WriteProperty(JsonWriter writer, JsonSerializer serializer, string name, object value)
        {
            if (value == null)
                return;

            writer.WritePropertyName(name);
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(JSchema).IsAssignableFrom(objectType);
        }
    }
}