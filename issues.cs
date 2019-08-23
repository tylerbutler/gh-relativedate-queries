using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace tylerbutler
{
    interface IDateParser
    {
        DateTime? ParseDate(string dateString, ILogger log);
    }

    public static class issues
    {
        private static IDateParser[] _parsers = new IDateParser[] {
            new PlusMinusParser(),
            new ChronicParser(),
            new RecognizerParser(),
        };
        private static Regex _pattern = new Regex(@"(?<before>.*?)created:(?<operator>[<=>]{1,2})(?<created>.+)(?<after>[\s\+]?.*)", RegexOptions.Compiled);

        [FunctionName("issues")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{acct}/{repo}/issues")]
            HttpRequest req,
            string acct,
            string repo,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            // Regex exp = new Regex(@"(?<before>.*?)created:(?<operator>[<=>]{1,2})(?<created>.+)(?<after>[\s\+]?.*)");
            // Regex exp = new Regex("creted: ");

            // https://github.com/Microsoft/prague/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+created%3A%3E2019-08-01
            // http://localhost:7071/microsoft/prague/issues?q=is%3Aissue+is%3Aopen+created%3A%3Etoday-7
            repo = string.Join('/', acct, repo);
            if (string.IsNullOrEmpty(repo))
            {
                return new BadRequestObjectResult("repo is required");
            }

            string searchQuery = req.Query["q"];
            MatchCollection matches = _pattern.Matches(searchQuery);
            if (matches.Count == 0)
            {
                return new BadRequestObjectResult("no 'created' param in search query");
            }
            else if (matches.Count > 1)
            {
                return new BadRequestObjectResult("too many 'created' params in search query");
            }

            var match = matches[0];
            string created = match.Groups["created"].Value;
            DateTime? date = await ParseDate(created, log, _parsers);
            if (!date.HasValue)
            {
                return new BadRequestObjectResult($"couldn't parse a date from {created}");
            }
            string dateString = date.Value.ToString("yyyy-MM-dd");

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // var d = DateTime.Parse((results[0].Resolution["values"] as Dictionary<string, string>).ToString());
            string urlBase = $"https://github.com/{repo}/issues";
            string query = $"{match.Groups["before"]}created:{match.Groups["operator"]}{dateString}{match.Groups["after"]}";
            string redirUrl = QueryHelpers.AddQueryString(urlBase, "q", query);
            return (ActionResult)new OkObjectResult($"Parsed date: {dateString}\n\nRedirect to {redirUrl}");
        }

        private async static Task<DateTime?> ParseDate(string dateString, ILogger log, IEnumerable<IDateParser> parsers)
        {
            foreach (var parser in parsers)
            {
                var parsedDate = parser.ParseDate(dateString, log);
                if (parsedDate != null)
                {
                    return parsedDate;
                }
                else
                {
                    log.LogInformation($"{parser.GetType().ToString()} found no valid date");
                }
            }
            log.LogError("All parsers failed to find dates");
            return null;
        }
    }
}
