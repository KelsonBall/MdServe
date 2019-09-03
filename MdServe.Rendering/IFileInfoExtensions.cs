using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MdServe.Rendering
{
    public static class IFileInfoExtensions
    {
        public static async Task<string> ReadTextAsync(this IFileInfo file)
        {
            using (var stream = file.CreateReadStream())
            using (var reader = new StreamReader(stream))
                return await reader.ReadToEndAsync();
        }
    }
}
