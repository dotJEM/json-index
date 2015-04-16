using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index.Schema
{
    public class JSchemeConverter : JsonConverter
    {
        private readonly string url;

        public JSchemeConverter() : this("")
        {
        }

        public JSchemeConverter(string url)
        {
            this.url = url;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JSchema schema = value as JSchema;
            Debug.Assert(schema != null);

            writer.WriteStartObject();
            if (schema.IsRoot)
            {
                WriteProperty(writer, serializer, "$schema", schema.Schema);
            }
            WriteProperty(writer, serializer, "id", schema.Id);
            
            WriteFlagsEnum("type", writer, serializer, schema.Type);
            WriteFlagsEnum("extendedType", writer, serializer, schema.ExtendedType);

            WriteProperty(writer, serializer, "required", schema.Required);
            WriteProperty(writer, serializer, "field", schema.Field);
            WriteProperty(writer, serializer, "title", schema.Title);
            WriteProperty(writer, serializer, "description", schema.Description);
            WriteProperty(writer, serializer, "contentType", schema.ContentType);
            WriteProperty(writer, serializer, "area", schema.Area);

            WriteProperty(writer, serializer, "items", schema.Items);
            WriteProperty(writer, serializer, "properties", schema.Properties);

            foreach (JProperty property in schema.Extensions)
                WriteProperty(writer, serializer, property.Name, property.Value);

            writer.WriteEndObject();
        }

        private static void WriteFlagsEnum(string name, JsonWriter writer, JsonSerializer serializer, object value)
        {
            var enumArr = Enum.GetValues(value.GetType())
                .Cast<int>()
                .Where(x => (x & (int)value) == x)
                .Select(x => Enum.ToObject(value.GetType(), x).ToString().ToLowerInvariant())
                .ToArray();

            if (enumArr.Length > 1)
                enumArr = enumArr.Skip(1).ToArray();

            writer.WritePropertyName(name);
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

        private int ReadFlagsEnum<T>(JToken token)
        {
            if (token == null) return 0;

            switch (token.Type)
            {
                case JTokenType.Array:
                    return ((JArray)token)
                        .Values<string>()
                        .Select(item => Enum.Parse(typeof(T), item, true))
                        .Aggregate(0, (current, value) => current | (int)value);

                case JTokenType.String:
                    return (int) Enum.Parse(typeof (T), token.ToString(), true);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            
            JSchema schema = new JSchema(
                (JsonSchemaType) ReadFlagsEnum<JsonSchemaType>(json["type"]),
                (JsonSchemaExtendedType)ReadFlagsEnum<JsonSchemaExtendedType>(json["extendedType"]));
            
            if (json["$schema"] != null)
            {
                schema.IsRoot = true;
                schema.Schema = (string) json["$schema"];
            }

            schema.Id = (string)json["id"];
            schema.Required = (bool)json["required"];
            schema.Field = (string) json["field"];
            schema.Title = (string) json["title"];
            schema.Description = (string)json["description"];
            schema.ContentType = (string)json["contentType"];
            schema.Area = (string)json["area"];

            schema.Items = json["items"] != null ? json["items"].ToObject<JSchema>() : null;
            schema.Properties = json["properties"] != null ? json["properties"].ToObject<JSchemaProperties>() : null;

            json.Remove("type");
            json.Remove("extendedType");
            json.Remove("$schema");
            json.Remove("id");
            json.Remove("required");
            json.Remove("field");
            json.Remove("description");
            json.Remove("contentType");
            json.Remove("area");
            json.Remove("items");
            json.Remove("properties");

            json.Properties().Aggregate(schema, (sc, property) =>
            {
                sc[property.Name] = property.Value;
                return sc;
            });

            return schema;
        }



        public override bool CanConvert(Type objectType)
        {
            return typeof(JSchema).IsAssignableFrom(objectType);
        }
    }

    public class FlagsEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum && objectType.GetCustomAttributes(typeof(FlagsAttribute), false).Any();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var values = serializer.Deserialize<string[]>(reader);
            return values
                .Select(x => Enum.Parse(objectType, x))
                .Aggregate(0, (current, value) => current | (int)value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var enumArr = Enum.GetValues(value.GetType())
                .Cast<int>()
                .Where(x => (x & (int)value) == x)
                .Select(x => Enum.ToObject(value.GetType(), x).ToString())
                .ToArray();

            if (enumArr.Length == 1)
                serializer.Serialize(writer, enumArr.First());
            else
                serializer.Serialize(writer, enumArr);
        }
    }

}