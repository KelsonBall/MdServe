using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MdServeApp
{
    public class WebhookRoute : ApiRoute<string>
    {
        public WebhookRoute() : base("/api.*") { }

        protected override Task handle(string message, HttpContext context)
        {
            throw new NotImplementedException();
        }
        
        protected override Option<string> predicate(HttpContext context)
        {
            if (context.Request.Method == "POST"
             && context.Request.ContentType == "application/json")
            {
                using var reader = new StreamReader(context.Request.Body);
                return reader.ReadToEnd();
            }
            else
                return false;
        }
    }
}
