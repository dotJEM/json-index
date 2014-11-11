using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Schema
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
    }

    public class JObjectEnumerator : IJObjectEnumarator
    {
        public IEnumerable<JNode> Enumerate(JObject json, string path = "")
        {
            if(json == null) yield break;

            yield return new JNode(path, json);
            foreach (JNode node in from property in json.Properties() 
                                   let fullname = string.IsNullOrEmpty(path) ? property.Name : path + "." + property.Name 
                                   from node in Enumerable.Empty<JNode>()
                                            .Union(Enumerate(property.Value as JObject, fullname))
                                            .Union(Enumerate(property.Value as JValue, fullname))
                                            .Union(Enumerate(property.Value as JArray, fullname)) 
                                   select node) yield return node;
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
    }
}