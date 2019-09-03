using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Kelson.Common.Web.Sample
{
    class Program
    {
        private static IFileProvider www;

        static void Main(string[] args)
        {
            www = new PhysicalFileProvider(Directory.GetCurrentDirectory());            
            new ServerBuilder("http://localhost:8000", "https://localhost:8080")
                .OnGet("/")
                    .Do(c => new HtmlResult("<h1>Hello World</h1>"))
                .OnGet("/numbers/{id:int}")
                    .Do(GetNumber)
                .OnGet("/blog/{name?}")
                    .Do(GetWebpage)
                .OnPost("/api", "push")
                    .WithBodyAs(async r => JsonConvert.DeserializeObject<PushEvent>(await r.ReadToEndAsync()))
                    .Do((c, p) => new StatusCodeResult(StatusCodes.Status500InternalServerError))
                .Build()
                .Run();
        }

        public static IActionResult GetNumber(WebContext context) =>
                new HtmlResult($"<h1>{context.Route.Values["id"]}</h1>");

        public static async Task<IActionResult> GetWebpage(WebContext context)
        {            
            string filename = (string)(context.Route.Values["name"] ?? "index.html");
            if (!filename.Contains("."))
                filename += ".html";
            if (filename.EndsWith(".html"))
            {
                await using var stream = www.GetFileInfo(filename).CreateReadStream();
                using var reader = new StreamReader(stream);
                return new HtmlResult(await reader.ReadToEndAsync());
            }
            else
            {
                return new StatusCodeResult(404);
            }
        }
            
                
        
        public class PushEvent
        {
            public string Branch { get; set; }
        }
    }
}
