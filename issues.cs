using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

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
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
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

            string urlBase = $"https://github.com/{repo}/issues";
            string query = $"{match.Groups["before"]}created:{match.Groups["operator"]}{dateString}{match.Groups["after"]}";
            string redirUrl = QueryHelpers.AddQueryString(urlBase, "q", query);

            StringValues stringValues;
            bool autoRedir = req.Query.TryGetValue("auto", out stringValues);
            autoRedir = autoRedir && stringValues.ToArray()[0] == "1";

            if (!autoRedir)
            {
                var parameters = req.Query.Keys.ToDictionary(k => k, v => req.Query[v].ToString());
                parameters["auto"] = "1";
                string bookmarkLink = QueryHelpers.AddQueryString(new Uri(req.GetDisplayUrl()).GetLeftPart(UriPartial.Path), parameters);

                var templatePath = Path.Combine(context.FunctionAppDirectory, "template.handlebars");
                var template = Handlebars.Compile(File.ReadAllText(templatePath));
                var resultText = template(new Dictionary<string, string>()
                {
                    {"dateString", dateString },
                    {"redirUrl", redirUrl },
                    {"bookmarkLink", bookmarkLink }
                });

                var toReturn = new ContentResult();
                toReturn.Content = resultText;
                toReturn.ContentType = "text/html; charset=utf-8";
                toReturn.StatusCode = StatusCodes.Status200OK;
                return toReturn;
            }

            return new RedirectResult(redirUrl, /* permanent */ false);
        }

        private async static Task<DateTime?> ParseDate(string dateString, ILogger log, IEnumerable<IDateParser> parsers)
        {
            foreach (var parser in parsers)
            {
                var parsedDate = parser.ParseDate(dateString, log);
                if (parsedDate != null)
                {
                    log.LogInformation($"Using date {parsedDate} from {parser.GetType().ToString()}");
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
