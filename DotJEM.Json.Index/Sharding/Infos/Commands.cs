using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Infos
{
    public class ShardChanges
    {
        public ShardInfo Shard { get; }
        public IEnumerable<JObject> Changes { get; }

        public ShardChanges(ShardInfo shard, IEnumerable<JObject> changes)
        {
            Shard = shard;
            Changes = changes;
        }
    }
    
    public class ShardInfo
    {
        public string Name { get; }
        public string Partition { get; }

        public ShardInfo(string name, string partition)
        {
            Name = name;
            Partition = partition;
        }

        protected bool Equals(ShardInfo other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Partition, other.Partition);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ShardInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name?.GetHashCode() ?? 0)*397) ^ (Partition?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return $"Shard<{Name}-{Partition}>";
        }
    }
}
