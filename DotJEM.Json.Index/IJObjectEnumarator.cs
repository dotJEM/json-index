using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public class JNode
    {
        public string Path { get; private set; }
        public JToken Token { get; private set; }

        public bool IsLeaf { get { return Token is JValue; } }
        public JTokenType Type { get { return Token.Type; } }

        public JNode(string path, JToken token)
        {
            Path = path;
            Token = token;
        }
    }

    public interface IJObjectEnumarator
    {
        IEnumerable<JNode> Enumerate(JObject json, string path = "");
        IEnumerable<JNode> Enumerate(JValue json, string path = "");
        IEnumerable<JNode> Enumerate(JArray json, string path = "");

        IEnumerable<T> Flatten<T>(JObject json, Func<string, JValue, T> factory, string path = "");
    }

    public class JObjectEnumerator : IJObjectEnumarator
    {
        public IEnumerable<JNode> Enumerate(JObject json, string path = "")
        {
            if(json == null) yield break;

            yield return new JNode(path, json);
            foreach (JNode node in from property in json.Properties() let fullname = string.IsNullOrEmpty(path) ? property.Name : path + "." + property.Name from node in Enumerable.Empty<JNode>()
                .Union(Enumerate(property.Value as JObject, fullname))
                .Union(Enumerate(property.Value as JValue, fullname))
                .Union(Enumerate(property.Value as JArray, fullname)) select node)
            {
                yield return node;
            }
        }

        public IEnumerable<JNode> Enumerate(JValue json, string path = "")
        {
            if (json == null) yield break;
            yield return new JNode(path, json);
        }

        public IEnumerable<JNode> Enumerate(JArray json, string path = "")
        {
            
            return json == null ? Enumerable.Empty<JNode>() :
                json.SelectMany(token => Enumerable.Empty<JNode>()
                .Union(Enumerate(token as JObject, path))
                .Union(Enumerate(token as JValue, path))
                .Union(Enumerate(token as JArray, path)));
        }


        public IEnumerable<T> Flatten<T>(JObject json, Func<string, JValue, T> factory, string path = "")
        {
            foreach (JProperty property in json.Properties())
            {
                string fullname = string.IsNullOrEmpty(path) ? property.Name : path + "." + property.Name;

                JObject jobj = property.Value as JObject;
                if (jobj != null)
                {
                    foreach (T item in Flatten(jobj, factory, fullname))
                        yield return item;
                    continue;
                }

                JValue jval = property.Value as JValue;
                if (jval != null)
                {
                    yield return factory(fullname, jval);
                    continue;
                }

                JArray jarr = property.Value as JArray;
                if (jarr != null)
                {
                    foreach (var p in FlattenArray(factory, jarr, fullname)) yield return p;
                }
            }
        }

        private IEnumerable<T> FlattenArray<T>(Func<string, JValue, T> factory, IEnumerable<JToken> jarr, string fullname)
        {
            foreach (JToken token in jarr)
            {
                JObject tjobj = token as JObject;
                if (tjobj != null)
                {
                    foreach (T item in Flatten(tjobj, factory, fullname))
                        yield return item;
                    continue;
                }

                JValue tjval = token as JValue;
                if (tjval != null)
                {
                    yield return factory(fullname, tjval);
                    continue;
                }

                JArray tjarr = token as JArray;
                if (tjarr != null)
                {
                    foreach (var p in FlattenArray(factory, tjarr, fullname)) yield return p;
                }
            }
        }
    }
}