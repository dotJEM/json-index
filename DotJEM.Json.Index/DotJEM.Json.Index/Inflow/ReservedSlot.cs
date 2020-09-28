using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Documents;

namespace DotJEM.Json.Index.Inflow
{
    public interface IReservedSlot
    {
        bool IsReady { get; }
        void Ready(IEnumerable<LuceneDocumentEntry> documents);
        void Complete();
        void OnComplete(Action<IEnumerable<LuceneDocumentEntry>> handler);
    }  
    
    public class ReservedSlot : IReservedSlot
    {
        private readonly Action onReady;
        private readonly string caller;
        private IEnumerable<LuceneDocumentEntry> documents;
        private readonly List<Action<IEnumerable<LuceneDocumentEntry>>> receivers = new List<Action<IEnumerable<LuceneDocumentEntry>>>();
        public bool IsReady { get; private set; }
        public int Index { get; set; }

        public ReservedSlot(Action onReady, string caller)
        {
            this.onReady = onReady;
            this.caller = caller;
        }

        public void Ready(IEnumerable<LuceneDocumentEntry> documents)
        {
            this.documents = documents;
            this.IsReady = true;
            onReady();
        }

        public void Complete()
        {
            foreach (Action<IEnumerable<LuceneDocumentEntry>> receiver in receivers)
                receiver(documents);

            receivers.Clear();
            documents = null;
        }

        public void OnComplete(Action<IEnumerable<LuceneDocumentEntry>> handler)
        {
            receivers.Add(handler);
        }

        public override string ToString()
        {
            return $"IsReady={IsReady}  caller={caller}";
        }
    }
}