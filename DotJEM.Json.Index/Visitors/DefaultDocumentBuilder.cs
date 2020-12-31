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
            AddField(new Int32Field(context.Path + ".@count", json.Count, Field.Store.NO));
            base.VisitArray(json, context);
        }

        protected override void VisitInteger(JValue json, IDocumentBuilderContext context)
        {
            long value = json.Value<long>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            AddField(new Int64Field(context.Path, value, Field.Store.NO));
            base.VisitInteger(json, context);
        }

        protected override void VisitFloat(JValue json, IDocumentBuilderContext context)
        {
            double value = json.Value<double>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            AddField(new DoubleField(context.Path, value, Field.Store.NO));
            base.VisitFloat(json, context);
        }

        protected override void VisitString(JValue json, IDocumentBuilderContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            if (context.Strategy.Visit(AddField, json, str, context))
                return;
 
            AddField(new TextField(context.Path, str, Field.Store.NO));
            AddField(new StringField(context.Path, str, Field.Store.NO));
            base.VisitString(json, context);
        }

        protected override void VisitBoolean(JValue json, IDocumentBuilderContext context)
        {
            bool value = json.Value<bool>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new TextField(context.Path, str, Field.Store.NO));
            AddField(new StringField(context.Path, str, Field.Store.NO));
            base.VisitBoolean(json, context);
        }

        protected override void VisitNull(JValue json, IDocumentBuilderContext context)
        {
            if (context.Strategy.VisitNull(AddField, json, context))
                return;

            AddField(new StringField(context.Path, "$$NULL$$", Field.Store.NO));
            base.VisitNull(json, context);
        }

        protected override void VisitUndefined(JValue json, IDocumentBuilderContext context)
        {
            if (context.Strategy.VisitUndefined(AddField, json, context))
                return;

            AddField(new StringField(context.Path, "$$UNDEFINED$$", Field.Store.NO));
            base.VisitUndefined(json, context);
        }

        protected override void VisitDate(JValue json, IDocumentBuilderContext context)
        {
            DateTime value = json.Value<DateTime>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            //Note: For sorting.
            AddField(new StringField(context.Path, value.ToString("s"), Field.Store.NO));
            AddField(new Int64Field(context.Path + ".@ticks", value.Ticks, Field.Store.NO));


            //TODO: It is likely that we can switch to a better format such as lucene it self uses, this is very short and should therefore probably
            //      perform even better.
            //
            //   Examples: 
            //      2014-09-10T11:00 => 0hzwfs800
            //      2014-09-10T13:00 => 0hzxzie7z
            AddField(new Int32Field(context.Path + ".@year", value.Year, Field.Store.NO));
            AddField(new Int32Field(context.Path + ".@month", value.Month, Field.Store.NO));
            AddField(new Int32Field(context.Path + ".@day", value.Day, Field.Store.NO));
            AddField(new Int32Field(context.Path + ".@hour", value.Hour, Field.Store.NO));
            AddField(new Int32Field(context.Path + ".@minute", value.Minute, Field.Store.NO));

            base.VisitDate(json, context);
        }

        protected override void VisitGuid(JValue json, IDocumentBuilderContext context)
        {
            Guid value = json.Value<Guid>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new TextField(context.Path, str, Field.Store.NO));
            AddField(new StringField(context.Path, str, Field.Store.NO));
            base.VisitGuid(json, context);
        }

        protected override void VisitUri(JValue json, IDocumentBuilderContext context)
        {
            Uri value = (Uri) json;
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            string str = json.ToString(CultureInfo.InvariantCulture);
            AddField(new TextField(context.Path, str, Field.Store.NO));
            AddField(new StringField(context.Path, str, Field.Store.NO));
            base.VisitGuid(json, context);
        }

        protected override void VisitTimeSpan(JValue json, IDocumentBuilderContext context)
        {
            TimeSpan value = json.Value<TimeSpan>();
            if (context.Strategy.Visit(AddField, json, value, context))
                return;

            //Note: For sorting.
            AddField(new StringField(context.Path, value.ToString("g"), Field.Store.NO));
            AddField(new Int64Field(context.Path + ".@ticks", value.Ticks, Field.Store.NO));

            AddField(new Int32Field(context.Path + ".@days", value.Days, Field.Store.NO));
            AddField(new Int32Field(context.Path + ".@hours", value.Hours, Field.Store.NO));
            AddField(new Int32Field(context.Path + ".@minutes", value.Minutes, Field.Store.NO));

            base.VisitDate(json, context);
        }
    }
}