using System.Collections.Generic;
using DotJEM.Json.Index.Documents;

namespace DotJEM.Json.Index.Inflow
{
    public class WriteInflow : IInflowJob
    {
        public int EstimatedCost { get; } = 1;
      
        private readonly IReservedSlot slot;
        private readonly IEnumerable<LuceneDocumentEntry> documents;

        public WriteInflow(IReservedSlot slot, IEnumerable<LuceneDocumentEntry> documents)
        {
            this.slot = slot;
            this.documents = documents;
        }

        public void Execute(IInflowScheduler scheduler)
        {
            slot.Ready(documents);
        }
    }
}