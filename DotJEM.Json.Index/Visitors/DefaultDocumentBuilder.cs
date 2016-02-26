using System;
using System.Globalization;
using DotJEM.Json.Index.Configuration;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    public class DefaultDocumentBuilder : AbstractDocumentBuilder
    {
        public DefaultDocumentBuilder(IStorageIndex index, string contentType) 
            : base(index, contentType)
        {
        }

        protected override void VisitArray(JArray json, IDocumentBuilderContext context)
        {
            AddField(new NumericField(context.Path + ".@count", Field.Store.NO, true).SetIntValue(json.Count));
            base.VisitArray(json, context);
        }

        protected override void VisitInteger(JValue json, IDocumentBuilderContext context)
        {
            long value = json.Value<long>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            AddField(new NumericField(context.Path, Field.Store.NO, true).SetLongValue(value));
            base.VisitInteger(json, context);
        }

        protected override void VisitFloat(JValue json, IDocumentBuilderContext context)
        {
            double value = json.Value<double>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            AddField(new NumericField(context.Path, Field.Store.NO, true).SetDoubleValue(value));
            base.VisitFloat(json, context);
        }

        protected override void VisitString(JValue json, IDocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            if (context.Strategy.Visit(AddField, json, str, context))
                return;
 
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitString(json, context);
        }

        protected override void VisitBoolean(JValue json, IDocumentBuilderContext context)
        {
            bool value = json.Value<bool>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
            base.VisitBoolean(json, context);
        }

        protected override void VisitNull(JValue json, IDocumentBuilderContext context)
        {
            if (context.Strategy.VisitNull(AddField, json, context))
                return;

            AddField(new Field(context.Path, "$$NULL$$", Field.Store.NO, Field.Index.NOT_ANALYZED));
            //AddField(new Field(context.Path, "$$NULL$$", Field.Store.NO, Field.Index.ANALYZED));
            base.VisitNull(json, context);
        }

        protected override void VisitUndefined(JValue json, IDocumentBuilderContext context)
        {
            if (context.Strategy.VisitUndefined(AddField, json, context))
                return;

            AddField(new Field(context.Path, "$$UNDEFINED$$", Field.Store.NO, Field.Index.NOT_ANALYZED));
            //AddField(new Field(context.Path, "$$UNDEFINED$$", Field.Store.NO, Field.Index.ANALYZED));
            base.VisitUndefined(json, context);
        }

        protected override void VisitDate(JValue json, IDocumentBuilderContext context)
        {
            DateTime value = json.Value<DateTime>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            AddField(new NumericField(context.Path + ".@ticks", Field.Store.NO, true).SetLongValue(value.Ticks));

            //TODO: It is likely that we can switch to a better format such as lucene it self uses, this is very short and should therefore probably
            //      perform even better.
            //
            //   Examples: 
            //      2014-09-10T11:00 => 0hzwfs800
            //      2014-09-10T13:00 => 0hzxzie7z
            AddField(new NumericField(context.Path + ".@year", Field.Store.NO, true).SetIntValue(value.Year));
            AddField(new NumericField(context.Path + ".@month", Field.Store.NO, true).SetIntValue(value.Month));
            AddField(new NumericField(context.Path + ".@day", Field.Store.NO, true).SetIntValue(value.Day));
            AddField(new NumericField(context.Path + ".@hour", Field.Store.NO, true).SetIntValue(value.Hour));
            AddField(new NumericField(context.Path + ".@minute", Field.Store.NO, true).SetIntValue(value.Minute));
            base.VisitDate(json, context);
        }

        protected override void VisitGuid(JValue json, IDocumentBuilderContext context)
        {
            Guid value = json.Value<Guid>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
            base.VisitGuid(json, context);
        }

        protected override void VisitUri(JValue json, IDocumentBuilderContext context)
        {
            Uri value = (Uri) json;
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
            base.VisitGuid(json, context);
        }

        protected override void VisitTimeSpan(JValue json, IDocumentBuilderContext context)
        {
            TimeSpan value = json.Value<TimeSpan>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            AddField(new NumericField(context.Path + ".@ticks", Field.Store.NO, true).SetLongValue(value.Ticks));

            AddField(new NumericField(context.Path + ".@days", Field.Store.NO, true).SetIntValue(value.Days));
            AddField(new NumericField(context.Path + ".@hours", Field.Store.NO, true).SetIntValue(value.Hours));
            AddField(new NumericField(context.Path + ".@minutes", Field.Store.NO, true).SetIntValue(value.Minutes));
            base.VisitDate(json, context);
        }
    }
}