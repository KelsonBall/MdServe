using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MdServeApp
{
    public class Program
    {
        public static readonly ManualResetEvent shutdown = new ManualResetEvent(false);

        private static volatile string 

        public static async Task Main(string[] args) =>
            await new Configuration("config.json")
                .Use(async config =>
                {
                    await using var server = new Server(config.Host,
                        new ApiRoute("/"));

                    shutdown.WaitOne();
                });

        public static async Task HandleFiles(HttpContext context)
        {
            context.Request.BodyPipe
        }
    }
}
