using DotJEM.Json.Index.IO;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Manager
{
    public interface IJsonIndexWriterCommand
    {
        void Execute(IJsonIndexWriter writer);
    }
    public class JsonIndexSingleUpdate : IJsonIndexWriterCommand
    {
        private readonly JObject value;

        public JsonIndexSingleUpdate(JObject value)
        {
            this.value = value;
        }

        public void Execute(IJsonIndexWriter writer) => writer.Update(value);
    }

    public class JsonIndexMultiUpdate : IJsonIndexWriterCommand
    {
        private readonly JObject[] values;

        public JsonIndexMultiUpdate(JObject[] values)
        {
            this.values = values;
        }

        public void Execute(IJsonIndexWriter writer) => writer.Update(values);

    }
    
    public class JsonIndexSingleCreate : IJsonIndexWriterCommand
    {
        private readonly JObject value;

        public JsonIndexSingleCreate(JObject value)
        {
            this.value = value;
        }

        public void Execute(IJsonIndexWriter writer) => writer.Create(value);
    }

    public class JsonIndexMultiCreate : IJsonIndexWriterCommand
    {
        private readonly JObject[] values;

        public JsonIndexMultiCreate(JObject[] values)
        {
            this.values = values;
        }

        public void Execute(IJsonIndexWriter writer) => writer.Create(values);

    }
    
    public class JsonIndexSingleDelete : IJsonIndexWriterCommand
    {
        private readonly JObject value;

        public JsonIndexSingleDelete(JObject value)
        {
            this.value = value;
        }

        public void Execute(IJsonIndexWriter writer) => writer.Delete(value);
    }

    public class JsonIndexMultiDelete: IJsonIndexWriterCommand
    {
        private readonly JObject[] values;

        public JsonIndexMultiDelete(JObject[] values)
        {
            this.values = values;
        }

        public void Execute(IJsonIndexWriter writer) => writer.Delete(values);

    }
}