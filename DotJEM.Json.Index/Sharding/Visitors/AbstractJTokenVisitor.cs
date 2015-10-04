using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Visitors
{
    public abstract class AbstractJTokenVisitor<TContext> : IJTokenVisitor<TContext>
    {
        public virtual void VisitJArray(JArray json, TContext context)
        {
            foreach (JToken token in json)
                Visit(token, context);
        }

        public virtual void VisitJObject(JObject json, TContext context)
        {
            foreach (JProperty property in json.Properties())
                Visit(property, context);
        }

        public virtual void VisitProperty(JProperty json, TContext context)
        {
            Visit(json.Value, context);
        }

        public virtual void VisitNone(JToken json, TContext context) { }
        public virtual void VisitConstructor(JConstructor json, TContext context) { }
        public virtual void VisitComment(JToken json, TContext context) { }
        public virtual void VisitInteger(JValue json, TContext context) { }
        public virtual void VisitFloat(JValue json, TContext context) { }
        public virtual void VisitString(JValue json, TContext context) { }
        public virtual void VisitBoolean(JValue json, TContext context) { }
        public virtual void VisitNull(JValue json, TContext context) { }
        public virtual void VisitUndefined(JValue json, TContext context) { }
        public virtual void VisitDate(JValue json, TContext context) { }
        public virtual void VisitRaw(JRaw json, TContext context) { }
        public virtual void VisitBytes(JValue json, TContext context) { }
        public virtual void VisitGuid(JValue json, TContext context) { }
        public virtual void VisitUri(JValue json, TContext context) { }
        public virtual void VisitTimeSpan(JValue json, TContext context) { }

        public void Visit(JToken json, TContext context)
        {
            switch (json.Type)
            {
                case JTokenType.None:
                    VisitNone(json, context);
                    break;
                case JTokenType.Object:
                    VisitJObject((JObject)json, context);
                    break;
                case JTokenType.Array:
                    VisitJArray((JArray)json, context);
                    break;
                case JTokenType.Constructor:
                    VisitConstructor((JConstructor)json, context);
                    break;
                case JTokenType.Property:
                    VisitProperty((JProperty)json, context);
                    break;
                case JTokenType.Comment:
                    VisitComment(json, context);
                    break;
                case JTokenType.Integer:
                    VisitInteger((JValue)json, context);
                    break;
                case JTokenType.Float:
                    VisitFloat((JValue)json, context);
                    break;
                case JTokenType.String:
                    VisitString((JValue)json, context);
                    break;
                case JTokenType.Boolean:
                    VisitBoolean((JValue)json, context);
                    break;
                case JTokenType.Null:
                    VisitNull((JValue)json, context);
                    break;
                case JTokenType.Undefined:
                    VisitUndefined((JValue)json, context);
                    break;
                case JTokenType.Date:
                    VisitDate((JValue)json, context);
                    break;
                case JTokenType.Raw:
                    VisitRaw((JRaw)json, context);
                    break;
                case JTokenType.Bytes:
                    VisitBytes((JValue)json, context);
                    break;
                case JTokenType.Guid:
                    VisitGuid((JValue)json, context);
                    break;
                case JTokenType.Uri:
                    VisitUri((JValue)json, context);
                    break;
                case JTokenType.TimeSpan:
                    VisitTimeSpan((JValue)json, context);
                    break;
            }
        }
    }
}