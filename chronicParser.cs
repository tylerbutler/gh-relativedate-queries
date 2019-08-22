using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace tylerbutler
{
    public class ChronicParser : IDateParser
    {
        private static Chronic.Parser _parser = new Chronic.Parser();

        public DateTime? ParseDate(string dateString, ILogger log)
        {
            var parsedValue = _parser.Parse(dateString);
            if (parsedValue != null)
            {
                return parsedValue.ToTime();
            }
            return null;
        }
    }
}