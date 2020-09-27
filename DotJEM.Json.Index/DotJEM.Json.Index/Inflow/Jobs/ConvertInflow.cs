using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Inflow
{
    public class ConvertInflow : IInflowJob
    {
        public int EstimatedCost { get; }
     
        private readonly IReservedSlot slot;
        private readonly IEnumerable<JObject> docs;
        private readonly ILuceneDocumentFactory factory;

        public ConvertInflow(IReservedSlot slot, JObject[] docs, ILuceneDocumentFactory factory)
        {
            this.EstimatedCost = docs.Length;
            this.slot = slot;
            this.docs = docs;
            this.factory = factory;
        }

        public void Execute(IInflowScheduler scheduler)
        {
            List<LuceneDocumentEntry> documents = factory
                .Create(docs)
                .ToList();
            scheduler.Enqueue(new WriteInflow(slot, documents), Priority.Highest);
        }
    }
}