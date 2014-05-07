using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Searching
{
    public interface IHit
    {
        int Doc { get; }
        float Score { get; }
        dynamic Json { get; }
    }

    public class Hit : IHit
    {
        private readonly Func<IHit, JObject> func;

        public int Doc { get; private set; }
        public float Score { get; private set; }

        public dynamic Json { get { return func(this); } }

        public Hit(int doc, float score, Func<IHit, JObject> func)
        {
            this.func = func;
            Doc = doc;
            Score = score;
        }
    }
}