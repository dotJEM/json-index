using System;
using System.Globalization;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Util;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Configuration.FieldStrategies.Querying
{
    public class DateRangeFieldQueryFactory : IRangeFieldQueryFactory
    {
        private DateTime? ParseOptionalDateTime(string value, DateTime now)
        {
            if (value == null || value == "*")
                return null;

            if (value.Equals("NULL", StringComparison.InvariantCultureIgnoreCase))
                return null;

            if (value.Equals("NOW", StringComparison.InvariantCultureIgnoreCase))
                return now;

            if (value.Equals("TODAY", StringComparison.InvariantCultureIgnoreCase))
                return now.Date;

            if (value.StartsWith("+"))
                return now.Add(AdvConvert.ConvertToTimeSpan(value.Substring(1)));

            if (value.StartsWith("-"))
                return now.Subtract(AdvConvert.ConvertToTimeSpan(value.Substring(1)));

            if (value.StartsWith("NOW+", StringComparison.InvariantCultureIgnoreCase))
                return now.Add(AdvConvert.ConvertToTimeSpan(value.Substring(4)));

            if (value.StartsWith("NOW-", StringComparison.InvariantCultureIgnoreCase))
                return now.Subtract(AdvConvert.ConvertToTimeSpan(value.Substring(4)));

            if (value.StartsWith("TODAY+", StringComparison.InvariantCultureIgnoreCase))
                return now.Date.Add(AdvConvert.ConvertToTimeSpan(value.Substring(6)));

            if (value.StartsWith("TODAY-", StringComparison.InvariantCultureIgnoreCase))
                return now.Date.Subtract(AdvConvert.ConvertToTimeSpan(value.Substring(6)));

            return DateTime.Parse(value, CultureInfo.InvariantCulture);
        }

        public Query Create(string field, CallContext call, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            DateTime now = DateTime.Now;
            DateTime? lower = ParseOptionalDateTime(part1, now);
            DateTime? upper = ParseOptionalDateTime(part2, now);

            Query absoluteRange = NumericRangeQuery.NewInt64Range(field + ".@ticks", lower?.Ticks, upper?.Ticks, startInclusive, endInclusive);
            BooleanQuery decomp = new BooleanQuery();

            if (lower == null || upper == null)
            {
                //Note: If either is null, it's an open range and therefor it only makes sense to append year to the query.
                return decomp
                    .Append(NumericRangeQuery.NewInt32Range(field + ".@year", lower?.Year, upper?.Year, true, true))
                    .Append(absoluteRange);
            }

            decomp = decomp.Append(NumericRangeQuery.NewInt32Range(field + ".@year", lower?.Year, upper?.Year, true, true));
            if (lower.Value.Year != upper.Value.Year)
                return decomp.Append(absoluteRange);

            decomp = decomp.Append(NumericRangeQuery.NewInt32Range(field + ".@month", lower?.Month, upper?.Month, true, true));
            if (lower.Value.Month != upper.Value.Month)
                return decomp.Append(absoluteRange);

            return decomp
                .Append(NumericRangeQuery.NewInt32Range(field + ".@day", lower?.Day, upper?.Day, true, true))
                .Append(absoluteRange);
        }
    }

    public static class BooleanQueryExt
    {
        public static BooleanQuery Append(this BooleanQuery self, Query absoluteRange, Occur occur = Occur.MUST)
        {
            self.Add(absoluteRange, occur);
            return self;
        }

    }
}