using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index.Schema
{
    public static class JSchemaExtensions
    {
        public static JSchema Merge(this JSchema self, JSchema other)
        {
            if (other == null)
                return self;

            //Note: Create a new to avoid that we modifies one that is being used atm.
            JSchema merged = new JSchema(self.Type, self.ExtendedType)
            {
                Type = self.Type | other.Type,
                ExtendedType = self.ExtendedType | other.ExtendedType,
                Indexed = self.Indexed || other.Indexed,
                Required = self.Required || other.Required,
                Title = MostQualifying(self.Title, other.Title),
                Description = MostQualifying(self.Description, other.Description),
                Area = MostQualifying(self.Area, other.Area),
                ContentType = MostQualifying(self.ContentType, other.ContentType),
                Field = MostQualifying(self.Field, other.Field),
                Items = self.Items != null ? self.Items.Merge(other.Items) : other.Items
            };

            merged.MergeExtensions(self);
            merged.MergeExtensions(other);

            if (other.Properties == null)
            {
                return merged.EnsureValidObject();
            }

            if (self.Properties == null)
            {
                merged.Properties = other.Properties;
                return merged.EnsureValidObject();
            }

            merged.Properties = self.Properties.Aggregate(new JSchemaProperties(), (map, kp) =>
            {
                map.Add(kp.Key, kp.Value);
                return map;
            });
            foreach (KeyValuePair<string, JSchema> pair in other.Properties)
            {
                merged.Properties[pair.Key] = 
                    self.Properties.ContainsKey(pair.Key) ? self.Properties[pair.Key].Merge(pair.Value) : pair.Value;
            }
            return merged;
        }

        private static JSchema EnsureValidObject(this JSchema self)
        {
            if (self.Type.HasFlag(JsonSchemaType.Object) && self.Properties == null)
                self.Properties = new JSchemaProperties();

            //if (Type.HasFlag(JsonSchemaType.Array) && Items == null)
            //    Items = new JSchema();

            return self;
        }

        private static string MostQualifying(string self, string other)
        {
            return string.IsNullOrEmpty(other) ? (self ?? other) : other;
        }

        public static JsonSchemaExtendedType LookupExtentedType(this JSchema self, string field)
        {
            try
            {
                if (self.Field == null || !field.StartsWith(self.Field))
                    return JsonSchemaExtendedType.None;

                if (self.Field == field)
                    return self.ExtendedType;

                JsonSchemaExtendedType extendedTypes = self.Items != null
                    ? self.Items.LookupExtentedType(field) : JsonSchemaExtendedType.None;

                if (self.Properties != null)
                {
                    extendedTypes = extendedTypes | self.Properties.Aggregate(JsonSchemaExtendedType.None,
                        (types, next) => LookupExtentedType(next.Value, field) | types);
                }

                return extendedTypes;
            }
            catch (NullReferenceException ex)
            {
                throw BuildException(self, ex);
            }
            catch (ArgumentNullException ex)
            {
                throw BuildException(self, ex);
            }
        }

        public static IEnumerable<JSchema> Traverse(this JSchema self)
        {
            if (self == null)
                throw new ArgumentNullException("self", "Cannot itterate a null schema.");

            try
            {
                IEnumerable<JSchema> all = Enumerable.Empty<JSchema>().Union(new[] { self });
                if (self.Items != null)
                {
                    all = all.Union(self.Items.Traverse());
                }

                if (self.Properties != null)
                {
                    all = all.Union(self.Properties.Values
                        //TODO: This is "Defensive", we still need to figure out WHY this happens.
                        //      But we skip these here.
                        .Where(p => p != null)
                        .SelectMany(property => property.Traverse()));
                }

                return all.ToList();
            }
            catch (NullReferenceException ex)
            {
                throw BuildException(self, ex);
            }
            catch (ArgumentNullException ex)
            {
                throw BuildException(self, ex);
            }
        }

        private static NullReferenceException BuildException(JSchema self, Exception ex)
        {
            //NOTE: This is defensive... 
            if (self.Properties == null)
                return new NullReferenceException(ex.Message, ex);

            var kv = self.Properties.Where(p => p.Key == null || p.Value == null).ToArray();
            if (kv.Length <= 0)
                return new NullReferenceException(ex.Message, ex);

            string message = "Found " + kv.Length + " propertie(s) where either the key or value was null in '"
                             + self.ContentType + ":" + self.Field + "'.\n\r"
                             + string.Join("\n\r", kv.Select(v => v.Key + " : " + v.Value));
            return new NullReferenceException(message, ex);
        }
    }
}