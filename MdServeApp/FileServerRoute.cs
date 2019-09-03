using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace MdServeApp
{
    public class FileServerRoute : ApiRoute<PathString>
    {
        private static string _wwwRoute;
        public static string WwwRoute
        {
            get => _wwwRoute;
            set
            {
                _wwwRoute = value;
                if (directory != null)
                    directory.Dispose();
                directory = new PhysicalFileProvider(value);
            }
        }

        private static PhysicalFileProvider directory;

        public FileServerRoute() : base("/.*") { }

        protected override async Task handle(PathString path, HttpContext context)
        {            
            await context.Response.SendFileAsync(directory.GetFileInfo(path.Value));         
        }

        protected override Option<PathString> predicate(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("api"))
                return (PathString)WwwRoute;
            else
                return false;
        }
    }
}
