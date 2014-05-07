using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public interface IJObjectEnumarator
    {
        IEnumerable<T> Flatten<T>(JObject json, Func<string, JValue, T> factory, string path = "");
    }

    public class JObjectEnumerator : IJObjectEnumarator
    {
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
                            //TODO: Bull!
                        }
                    }
                }
            }
        }
    }
}