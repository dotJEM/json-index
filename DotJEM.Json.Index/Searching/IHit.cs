using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Searching
{
    public interface IHit
    {
        int Doc { get; }
        float Score { get; }
        dynamic Json { get; }
        JObject Entity { get; }
    }

    public class Hit : IHit
    {
        private readonly Lazy<JObject> entity; 

        public int Doc { get; private set; }
        public float Score { get; private set; }

        public dynamic Json { get { return entity.Value; } }
        public JObject Entity { get { return entity.Value; } }

        public Hit(int doc, float score, Func<IHit, JObject> func)
        {
            entity = new Lazy<JObject>(() => func(this));
            Doc = doc;
            Score = score;
        }
    }
}