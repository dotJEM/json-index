using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Test.Data
{
    public class PersonDataDecorator : TestDataDecorator
    {
        public override JObject Decorate(JObject json, Random rnd)
        {
            json["type"] = "Person";

            switch (rnd.Next(2))
            {
                case 0:
                    json["name"] = "John";
                    json["surname"] = "Doe";
                    json["age"] = rnd.Next(1,100);
                    break;

                case 1:
                    json["name"] = "Peter";
                    json["surname"] = "Pan";
                    json["age"] = rnd.Next(1,100);
                    break;
                    
                case 2:
                    json["name"] = "Alice";
                    json["age"] = rnd.Next(1,100);
                    break;
            }
            return json;
        }
    }
}