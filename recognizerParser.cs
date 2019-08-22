using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;

namespace tylerbutler
{
    public class RecognizerParser : IDateParser
    {
        public DateTime? ParseDate(string dateString, ILogger log)
        {
            var model = DateTimeRecognizer.RecognizeDateTime(dateString, Culture.English)[0].Resolution;

            if (model.Count != 1)
            {
                return null; // new BadRequestObjectResult($"parsed {model.Count} dates, expected 1");
            }

            // DateTime dateTime;
            var d = (model["values"] as List<Dictionary<string, string>>)[0];
            if (d["type"] == "daterange")
            {
                return DateTime.Parse(d["start"]); // $"{d["start"]}..{d["end"]}";
            }

            if (d.ContainsKey("value"))
            {
                return DateTime.Parse(d["value"]);
            }

            return null;
        }
    }
}