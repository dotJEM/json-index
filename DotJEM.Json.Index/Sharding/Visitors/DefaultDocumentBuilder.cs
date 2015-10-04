using System;
using System.Globalization;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Visitors
{
    public class DefaultDocumentBuilder : AbstractDocumentBuilder
    {
        public override void VisitJArray(JArray json, DocumentBuilderContext context)
        {
            context.AddField(new NumericField(context.Path + ".@count", Field.Store.NO, true).SetIntValue(json.Count));
            base.VisitJArray(json, context);
        }

        public override void VisitInteger(JValue json, DocumentBuilderContext context)
        {
            context.AddField(new NumericField(context.Path, Field.Store.NO, true).SetLongValue(json.Value<long>()));
            base.VisitInteger(json, context);
        }

        public override void VisitFloat(JValue json, DocumentBuilderContext context)
        {
            context.AddField(new NumericField(context.Path, Field.Store.NO, true).SetDoubleValue(json.Value<double>()));
            base.VisitFloat(json, context);
        }

        public override void VisitString(JValue json, DocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            if (str.Contains(" "))
            {
                context.AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
                context.AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            }
            else
            {
                context.AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            }

            base.VisitString(json, context);
        }

        public override void VisitBoolean(JValue json, DocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            context.AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitBoolean(json, context);
        }

        public override void VisitNull(JValue json, DocumentBuilderContext context)
        {
            context.AddField(new Field(context.Path, "$$NULL$$", Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitNull(json, context);
        }

        public override void VisitUndefined(JValue json, DocumentBuilderContext context)
        {
            context.AddField(new Field(context.Path, "$$UNDEFINED$$", Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitUndefined(json, context);
        }

        public override void VisitDate(JValue json, DocumentBuilderContext context)
        {
            DateTime date = json.Value<DateTime>();
            context.AddField(new NumericField(context.Path + ".@ticks", Field.Store.NO, true).SetLongValue(date.Ticks));

            context.AddField(new NumericField(context.Path + ".@year", Field.Store.NO, true).SetIntValue(date.Year));
            context.AddField(new NumericField(context.Path + ".@month", Field.Store.NO, true).SetIntValue(date.Month));
            context.AddField(new NumericField(context.Path + ".@day", Field.Store.NO, true).SetIntValue(date.Day));
            context.AddField(new NumericField(context.Path + ".@hour", Field.Store.NO, true).SetIntValue(date.Hour));
            context.AddField(new NumericField(context.Path + ".@minute", Field.Store.NO, true).SetIntValue(date.Minute));
            base.VisitDate(json, context);
        }

        public override void VisitGuid(JValue json, DocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            context.AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitGuid(json, context);
        }

        public override void VisitUri(JValue json, DocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            context.AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitGuid(json, context);
        }

        public override void VisitTimeSpan(JValue json, DocumentBuilderContext context)
        {
            TimeSpan date = json.Value<TimeSpan>();
            context.AddField(new NumericField(context.Path + ".@ticks", Field.Store.NO, true).SetLongValue(date.Ticks));

            context.AddField(new NumericField(context.Path + ".@days", Field.Store.NO, true).SetIntValue(date.Days));
            context.AddField(new NumericField(context.Path + ".@hours", Field.Store.NO, true).SetIntValue(date.Hours));
            context.AddField(new NumericField(context.Path + ".@minutes", Field.Store.NO, true).SetIntValue(date.Minutes));
            base.VisitDate(json, context);
        }
    }
}