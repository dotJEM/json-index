using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Configuration;

namespace DotJEM.Json.Index.Test.Unit.Configuration
{
    public class Class1
    {
        public void Test()
        {
            IIndexConfiguration config = new IndexConfiguration();

            config.SetTypeResolver("contentType");

        }

    }
}
