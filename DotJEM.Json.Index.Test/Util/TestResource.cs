using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Test.Util
{
    public class TestResource
    {
        public static string LoadText(string resourceName)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            using (StreamReader sr = new StreamReader(a.GetManifestResourceStream(a.GetManifestResourceNames().Single(x => x.Contains(resourceName)))))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
