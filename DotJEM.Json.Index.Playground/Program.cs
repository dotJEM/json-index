using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            IStorageIndex index = new LuceneStorageIndex("index");

            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c61', '$contentType': 'dummy', $area: 'foo' }"));
            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c62', '$contentType': 'dummy', $area: 'foo' }"));
            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c63', '$contentType': 'dummy', $area: 'foo' }"));
            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c64', '$contentType': 'dummy', $area: 'foo' }"));
            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c65', '$contentType': 'dummy', $area: 'foo' }"));
            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c66', '$contentType': 'dummy', $area: 'foo' }"));
            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c67', '$contentType': 'dummy', $area: 'foo' }"));
            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c68', '$contentType': 'dummy', $area: 'foo' }"));
            index.Write(JObject.Parse("{ '$id': '9f83c882-2bac-4e9f-937d-d3444ee89c69', '$contentType': 'dummy', $area: 'foo' }"));

            Console.WriteLine(index.Search("*:*").Documents.Count());
            index.Storage.Purge();
            Console.WriteLine(index.Search("*:*").Documents.Count());
        }
    }
}
