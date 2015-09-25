using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Benchmarks.TestFactories
{
    public class JsonGenerator
    {

        public JToken Generate(JToken json)
        {
            return json;
        }

        //ValueFacs

        /* Value Factories:
         * 
         *   range(0,10) => 0,1,2,3,4,5,6,7,8,9
         *   repeat(0, 5) => 0,0,0,0,0
         *   guid() => 7e30d198-1e66-4a8e-8f4d-a71d440bbaa3
         *   index() => x in repeat
         *   bool() => true/false
         *   float(0, 10) => 7.32  
         *   int(0, 10) => 7
         *   random(A,B,C) => B
         *   format(int(0,10), "##.00") => "7.00"
         *   name() => Random name from list
         *   surname() => Random surname name from list
         *   date() => Random date
         *   date(min, max) => Random date between
         *   date().day() => Day part of a date
         *   text(0,100) => Random full text between 0 and 100 words
         *   word() => Random word
         *   
         *   lambda: fullname: json => json.firstname + ' ' + json.lastname;
         * 
         * Property Modifiers:
         * 
         *   repeat(5)
         * 
         */

    }
}
