using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotJEM.Json.Index.TestUtil
{
    public static class DebugOut
    {
        public static string WriteYaml<T>(T value)
        {
            string yaml = ToYaml(value);
            Console.WriteLine(yaml);
            return yaml;
        }

        public static string WriteJson<T>(T value)
        {
            string json = ToJson(value).ToString(Formatting.Indented);
            Console.WriteLine(json);
            return json;
        }

        public static string ToYaml<T>(T value) => ToYaml(ToJson(value));

        public static JObject ToJson<T>(T value)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = new TestBinder()
            };
            settings.Converters.Add(new StringEnumConverter());
            return JObject.FromObject(value, JsonSerializer.Create(settings));
        }

        public static string ToYaml(JObject json)
        {
            ExpandoObjectConverter expConverter = new ExpandoObjectConverter();
            dynamic deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(json.ToString(), expConverter);
            return new SerializerBuilder()
                .EmitDefaults()
                .Build()
                .Serialize(deserializedObject);
        }

        public static string Sanitize(string yaml)
        {
            string[] lines = Lines(yaml).ToArray();

            StringWriter writer = new StringWriter();
            int trim = lines.Select(Ws).FirstOrDefault(indent => indent > 0);
            foreach (string line in lines.Select(l => Trim(l, trim)))
                writer.WriteLine(line);
            return writer.ToString();

            IEnumerable<string> Lines(string str)
            {
                StringReader reader = new StringReader(str);
                string line;
                while ((line = reader.ReadLine()) != null)
                    yield return line;
            }

            string Trim(string str, int amount)
            {
                return str.StartsWith("  ") ? str.Substring(amount) : str;
            }

            int Ws(string str)
            {
                return str.TakeWhile(c => c == ' ').Count();
            }
        }

        public class TestBinder : ISerializationBinder
        {
            public Type BindToType(string assemblyName, string typeName)
            {
                throw new NotImplementedException();
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.Name;
            }
        }
    }
}
