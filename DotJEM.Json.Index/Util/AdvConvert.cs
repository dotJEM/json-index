using System;
using System.Text.RegularExpressions;

namespace DotJEM.Json.Index.Util
{
    public class AdvConvert
    {
        private static readonly Regex timeSpanExpression = new Regex(@"((?'d'[0-9]+)\s?d(ay(s)?)?)?\s?" +
                                                                     @"((?'h'[0-9]+)\s?h(our(s)?)?)?\s?" +
                                                                     @"((?'m'[0-9]+)\s?m(in(ute(s)?)?)?)?\s?" +
                                                                     @"((?'s'[0-9]+)\s?s(ec(ond(s)?)?)?)?\s?" +
                                                                     @"((?'f'[0-9]+)\s?f(rac(tion(s)?)?)?|ms|millisecond(s)?)?\s?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex byteCountExpression = new Regex(@"((?'g'[0-9]+)\s?gb|gigabyte(s)?)?\s?" +
                                                                      @"((?'m'[0-9]+)\s?mb|megabyte(s)?)?\s?" +
                                                                      @"((?'k'[0-9]+)\s?kb|kilobyte(s)?)?\s?" +
                                                                      @"((?'b'[0-9]+)\s?b|byte(s)?)?\s?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Atempts to convert a string to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <remarks>
        /// The method first attempts to use the normal <see cref="TimeSpan.Parse"/> method, if that fails it then usesuses a range of wellknown formats
        /// to atempt a conversion of a string representing a <see cref="TimeSpan"/>.
        /// <p/>The order of which the values are defined must always be "Days, Hours, Minutes, Seconds and Fractions" But non of them are required,
        /// that means that a valid format could be '5 days 30 min' as well as '3h', and spaces are alowed between each value and it's unit definition.
        /// <p/>The folowing units are known.
        /// <table>
        /// <tr><td>Days</td><td>d, day, days</td></tr>
        /// <tr><td>Hours</td><td>h, hour, hours</td></tr>
        /// <tr><td>Minutes</td><td>m, min, minute, minutes</td></tr>
        /// <tr><td>Seconds</td><td>s, sec, second, seconds</td></tr>
        /// <tr><td>Fractions</td><td>f, frac, fraction. fractions, ms, millisecond, milliseconds</td></tr>
        /// </table>
        /// <p/>All Unit definitions ignores any casing.
        /// </remarks>
        /// <param name="input">A string representing a <see cref="TimeSpan"/>.</param>
        /// <returns>A TimeSpan from the given input.</returns>
        /// <example>
        /// This piece of code first parses the string "2m 30s" to a <see cref="TimeSpan"/> and then uses that <see cref="TimeSpan"/> to sleep for 2 minutes and 30 seconds.
        /// <code>
        /// public void SleepForSomeTime()
        /// {
        ///   //Two and a half minute.
        ///   TimeSpan sleep = Convert.ToTimeSpan("2m 30s");
        ///   Thread.Spleep(sleep);
        /// }
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Input was null</exception>
        /// <exception cref="FormatException">The given input could not be converted to a <see cref="TimeSpan"/> because the format was invalid.</exception>
        public static TimeSpan ConvertToTimeSpan(string input)
        {
            TimeSpan outPut;
            if (TryConvertToTimeSpan(input, out outPut))
                return outPut;

            throw new FormatException("Input string was not in a correct format.");
        }

        public static bool TryConvertToTimeSpan(string input, out TimeSpan value)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (TimeSpan.TryParse(input, out value))
                return true;

            Match match = timeSpanExpression.Match(input);
            if (!match.Success)
                return false;

            int days = ParseGroup(match.Groups["d"]);
            int hours = ParseGroup(match.Groups["h"]);
            int minutes = ParseGroup(match.Groups["m"]);
            int seconds = ParseGroup(match.Groups["s"]);
            int milliseconds = ParseGroup(match.Groups["f"]);
            value = new TimeSpan(days, hours, minutes, seconds, milliseconds);
            return true;
        }

        /// <summary>
        /// Atempts to convert a string to <see cref="long"/> value as a number of bytes.
        /// </summary>
        /// <remarks>
        /// The method usesuses a range of wellknown formats to atempt a conversion of a string representing a size in bytes.
        /// <p/>The order of which the values are defined must always be "Gigabytes, Megabytes, Kilobytes, and Bytes" But non of them are required,
        /// that means that a valid format could be '5 gigabytes 512 bytes' as well as '3kb', and spaces are alowed between each value and it's unit definition.
        /// <p/>The folowing units are known.
        /// <table>
        /// <tr><td>Gigabytes</td><td>gb, gigabyte, gigabytes</td></tr>
        /// <tr><td>Megabytes</td><td>mb, megabyte, megabytes</td></tr>
        /// <tr><td>Kilobytes</td><td>kb, kilobyte, kilobytes</td></tr>
        /// <tr><td>Bytes</td><td>b, byte, bytes</td></tr>
        /// </table>
        /// <p/>All Unit definitions ignores any casing.
        /// </remarks>
        /// <param name="input">A string representing a total number of bytes as Gigabytes, Megabytes, Kilobytes and Bytes.</param>
        /// <returns>A <see cref="long"/> calculated as the total number of bytes from the given input.</returns>
        /// <example>
        /// This piece of code first parses the string "25mb 512kb" to a long and then uses to write an empty file in the "C:\Temp" folder.
        /// <code>
        /// public void WriteSomeFile()
        /// {
        ///   long lenght = Convert.ToByteCount("25mb 512kb");
        ///   FileHelper.CreateTextFile("C:\temp", new byte[lenght], true);
        /// }
        /// </code>
        /// </example>
        /// <exception cref="FormatException">The given input could not be converted because the format was invalid.</exception>
        /// <exception cref="ArgumentNullException">Input was null</exception>
        public static long ConvertToByteCount(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            Match match = byteCountExpression.Match(input);
            if (match == null || !match.Success)
                throw new FormatException("Input string was not in a correct format.");

            long gigaBytes = ParseGroup(match.Groups["g"]);
            long megaBytes = ParseGroup(match.Groups["m"]); ;
            long kiloBytes = ParseGroup(match.Groups["k"]); ;
            long bytes = ParseGroup(match.Groups["b"]); ;
            return bytes + (1024L * (kiloBytes + (1024L * (megaBytes + (1024L * gigaBytes)))));
        }

        /// <summary>
        /// Parses an enum genericly.
        /// </summary>
        /// <remarks>
        /// See Enum.Parse for details on exception behavior etc.
        /// </remarks>
        /// <typeparam name="TEnumeration">Target enum type</typeparam>
        /// <param name="state">value to parse</param>
        /// <returns>Parsed value</returns>
        public static TEnumeration ConvertToEnum<TEnumeration>(string state)
        {
            return (TEnumeration)Enum.Parse(typeof(TEnumeration), state, true);
        }

        private static int ParseGroup(Group group)
        {
            return string.IsNullOrEmpty(@group?.Value) ? 0 : int.Parse(@group.Value);
        }
    }
}