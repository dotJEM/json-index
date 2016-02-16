using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Test.Data
{
    public interface ITestDataDecorator
    {
        JObject Decorate(JObject json, Random rnd);
    }

    public abstract class TestDataDecorator : ITestDataDecorator
    {
        protected T Random<T>(T[] elements, Random rnd)
        {
            return elements[rnd.Next(elements.Length - 1)];
        }

        public abstract JObject Decorate(JObject json, Random rnd);
    }
}
