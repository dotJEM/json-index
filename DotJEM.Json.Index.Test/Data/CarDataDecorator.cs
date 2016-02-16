using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotJEM.Json.Index.Test.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Test.Data
{
    public partial class CarDataDecorator : TestDataDecorator
    {
        class Car
        {
            public int year;
            public string make, model;
        }

        private static readonly Car[] cars;

        static CarDataDecorator()
        {
            string data = TestResource.LoadText("cars.csv");

            cars = ParseCarsFile(data).ToArray();
        }

        private static IEnumerable<Car> ParseCarsFile(string data)
        {
            using (StringReader reader = new StringReader(data))
            {
                string next;
                while ((next = reader.ReadLine()) != null)
                {
                    string[] fields = next.Split(',');
                    yield return new Car
                    {
                        year = int.Parse(fields[0]),
                        make = fields[1].Trim(),
                        model = fields[2].Trim()
                    };
                }
            }
        }


        public override JObject Decorate(JObject json, Random rnd)
        {
            json["type"] = "Car";

            Car select = Random(cars, rnd);
            json["year"] = new DateTime(select.year, 1, 1);
            json["make"] = select.make;
            json["model"] = select.model;
            return json;
        }
    }
}