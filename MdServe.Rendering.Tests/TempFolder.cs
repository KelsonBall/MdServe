using System;
using System.IO;
using System.Threading.Tasks;

namespace MdServe.Rendering.Tests
{
    public class TempFolder : IAsyncDisposable
    {
        private readonly string name;        
        public string FullName { get; }

        public TempFolder()
        {            
            name = "temp_" + Guid.NewGuid().ToString().Substring(0, 8);
            FullName = Path.Combine(Directory.GetCurrentDirectory(), name);
            Directory.CreateDirectory(name);
        }

        public async Task CreateFile(string filename, string content)
        {
            string file = Path.Combine(name, filename);
            if (File.Exists(file))
                File.Delete(file);
            using var stream = File.CreateText(file);
            await stream.WriteAsync(content);
        }

        public async Task CreateFolder(string path)
        {
            Directory.CreateDirectory(Path.Combine(name, path));
        }

        
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            int attempts = 5;
            await Task.Run(async () =>
            {
                while (attempts > 0)
                {
                    try
                    {
                        Directory.Delete(name, true);
                        break;
                    }
                    catch (IOException)
                    {
                        attempts--;
                        await Task.Delay(1000);
                    }
                }
            });
        }
    }
}
