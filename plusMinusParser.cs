using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace tylerbutler
{
    public class PlusMinusParser : IDateParser
    {
        private static Regex _pattern = new Regex(@"today\s*(?<operator>[-\+]{1})\s*(?<number>\d*)", RegexOptions.Compiled);

        public DateTime? ParseDate(string dateString, ILogger log)
        {
            Match match = _pattern.Match(dateString);
            if (!match.Success)
            {
                return null;
            }
            switch (match.Groups["operator"].Value)
            {
                case "+":
                    return DateTime.UtcNow.AddDays(int.Parse(match.Groups["number"].Value));
                case "-":
                    return DateTime.UtcNow.AddDays(-1 * int.Parse(match.Groups["number"].Value));
                default:
                    return null;
            }
        }
    }
}