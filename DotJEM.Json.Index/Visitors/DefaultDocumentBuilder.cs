using System;
using System.Globalization;
using DotJEM.Json.Index.Configuration;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    public class DefaultDocumentBuilder : AbstractDocumentBuilder
    {
        public DefaultDocumentBuilder(IStorageIndex index) 
            : base(index)
        {
        }

        protected override void VisitArray(JArray json, IDocumentBuilderContext context)
        {
            AddField(new NumericField(context.Path + ".@count", Field.Store.NO, true).SetIntValue(json.Count));
            base.VisitArray(json, context);
        }

        protected override void VisitInteger(JValue json, IDocumentBuilderContext context)
        {
            AddField(new NumericField(context.Path, Field.Store.NO, true).SetLongValue(json.Value<long>()));
            base.VisitInteger(json, context);
        }

        protected override void VisitFloat(JValue json, IDocumentBuilderContext context)
        {
            AddField(new NumericField(context.Path, Field.Store.NO, true).SetDoubleValue(json.Value<double>()));
            base.VisitFloat(json, context);
        }

        protected override void VisitString(JValue json, IDocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));

            // Consider to stor it as
            //if (str.Contains(" "))
            //{
            //    AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
            //    AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            //}
            //else
            //{
            //    AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
            //    AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            //}

            base.VisitString(json, context);
        }

        protected override void VisitBoolean(JValue json, IDocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitBoolean(json, context);
        }

        protected override void VisitNull(JValue json, IDocumentBuilderContext context)
        {
            AddField(new Field(context.Path, "$$NULL$$", Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitNull(json, context);
        }

        protected override void VisitUndefined(JValue json, IDocumentBuilderContext context)
        {
            AddField(new Field(context.Path, "$$UNDEFINED$$", Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitUndefined(json, context);
        }

        protected override void VisitDate(JValue json, IDocumentBuilderContext context)
        {
            DateTime date = json.Value<DateTime>();
            AddField(new NumericField(context.Path + ".@ticks", Field.Store.NO, true).SetLongValue(date.Ticks));

            AddField(new NumericField(context.Path + ".@year", Field.Store.NO, true).SetIntValue(date.Year));
            AddField(new NumericField(context.Path + ".@month", Field.Store.NO, true).SetIntValue(date.Month));
            AddField(new NumericField(context.Path + ".@day", Field.Store.NO, true).SetIntValue(date.Day));
            AddField(new NumericField(context.Path + ".@hour", Field.Store.NO, true).SetIntValue(date.Hour));
            AddField(new NumericField(context.Path + ".@minute", Field.Store.NO, true).SetIntValue(date.Minute));
            base.VisitDate(json, context);
        }

        protected override void VisitGuid(JValue json, IDocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitGuid(json, context);
        }

        protected override void VisitUri(JValue json, IDocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            base.VisitGuid(json, context);
        }

        protected override void VisitTimeSpan(JValue json, IDocumentBuilderContext context)
        {
            TimeSpan date = json.Value<TimeSpan>();
            AddField(new NumericField(context.Path + ".@ticks", Field.Store.NO, true).SetLongValue(date.Ticks));

            AddField(new NumericField(context.Path + ".@days", Field.Store.NO, true).SetIntValue(date.Days));
            AddField(new NumericField(context.Path + ".@hours", Field.Store.NO, true).SetIntValue(date.Hours));
            AddField(new NumericField(context.Path + ".@minutes", Field.Store.NO, true).SetIntValue(date.Minutes));
            base.VisitDate(json, context);
        }
    }
}