using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.TestData
{
    public class JsonPlaceholder
    {
        public static JsonPlaceholder Instance { get; } = new JsonPlaceholder();

        private readonly string assemblyName;
        private readonly Assembly assembly;

        private JsonPlaceholder()
        {
            assembly = GetType().Assembly;
            assemblyName = assembly.GetName().Name;
        }

        public static JArray Albums => (JArray)Instance.Load();
        public static JArray Comments => (JArray)Instance.Load();
        public static JArray Photos => (JArray)Instance.Load();
        public static JArray Posts => (JArray)Instance.Load();
        public static JArray Todos => (JArray)Instance.Load();
        public static JArray Users => (JArray)Instance.Load();

        private JToken Load([CallerMemberName] string resource = "") => ReadEmbeddedResource($"{assemblyName}._resources.jsonplaceholder.typicode.com.{resource.ToLower()}.json");

        private JToken ReadEmbeddedResource(string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    JsonReader reader = new JsonTextReader(streamReader);
                    try
                    {
                        return JToken.ReadFrom(reader);
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }
        }
    }
}
