using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Lucene.Net.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Serialization
{
    public interface IJsonSerializer
    {
        JObject Deserialize(byte[] value);
        byte[] Serialize(JObject json);
    }

    public class GZipJsonSerialier : IJsonSerializer
    {
        public JObject Deserialize(byte[] value)
        {
            using (MemoryStream stream = new MemoryStream(value))
            {
                using (GZipStream zip = new GZipStream(stream, CompressionMode.Decompress))
                {
                    JsonTextReader reader = new JsonTextReader(new StreamReader(zip));
                    var entity = (JObject)JToken.ReadFrom(reader);
                    reader.Close();
                    return entity;
                }
            }
        }

        public byte[] Serialize(JObject json)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(stream, CompressionLevel.Optimal))
                {
                    JsonTextWriter jsonWriter = new JsonTextWriter(new StreamWriter(zip));
                    json.WriteTo(jsonWriter);
                    jsonWriter.Close();
                }
                return stream.GetBuffer();
            }
        }
    }
}
