using System;
using System.Linq;
using Newtonsoft.Json;

namespace DotJEM.Json.Index.Schema
{
    public class JsonSchemeTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum && objectType.GetCustomAttributes(typeof(FlagsAttribute), false).Any();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var values = serializer.Deserialize<string[]>(reader);
            return values
                .Select(x => Enum.Parse(objectType, x, true))
                .Aggregate(0, (current, value) => current | (int)value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var enumArr = Enum.GetValues(value.GetType())
                .Cast<int>()
                .Where(x => (x & (int)value) == x)
                .Select(x => Enum.ToObject(value.GetType(), x).ToString().ToLowerInvariant())
                .ToArray();

            if (enumArr.Length > 1)
            {
                enumArr = enumArr.Skip(1).ToArray();
            }

            if (enumArr.Length == 1)
            {
                serializer.Serialize(writer, enumArr.First());
            }
            else
            {
                serializer.Serialize(writer, enumArr);
            }
           
        }
    }

    public class JSchemeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
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