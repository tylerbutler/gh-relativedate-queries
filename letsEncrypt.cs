using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace tylerbutler
{
    public static class LetsEncrypt
    {
        [FunctionName("LetsEncrypt")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "letsencrypt/{rest}")]
            HttpRequestMessage req,
            string rest,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request. {rest}");

            var content = File.ReadAllText($@"D:\home\site\letsencrypt\.well-known\acme-challenge\{rest}");
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = new StringContent(content, System.Text.Encoding.UTF8, "text/plain");
            return resp;
        }
    }
}
