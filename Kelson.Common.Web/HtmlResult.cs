using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Kelson.Common.Web
{
    public class HtmlResult : IActionResult
    {
        private readonly ReadOnlyMemory<byte> content;

        public HtmlResult(string content)
        {
            this.content = Encoding.UTF8.GetBytes(content).AsMemory();
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = 200;
            response.ContentLength = content.Length;
            response.ContentType = "text/html";
            await context.HttpContext.Response.Body.WriteAsync(content);
        }
    }
}
