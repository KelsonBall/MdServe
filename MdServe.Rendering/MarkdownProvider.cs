using Markdig;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MdServe.Rendering
{
    public class MarkdownProvider : IFileProvider, IDisposable
    {
        private readonly string path;        
        private PhysicalFileProvider source;        
        private readonly MarkdownPipeline converter;
        private readonly Dictionary<string, RenderedFileInfo> renderedPages = new Dictionary<string, RenderedFileInfo>();
        private readonly IFileInfo header;
        private readonly IFileInfo footer;
        private readonly IChangeToken changes;
        private readonly IDisposable callbackDisposer;

        public MarkdownProvider(string repository)
        {
            this.path = Path.Combine(repository, "content");
            this.source = new PhysicalFileProvider(path, ExclusionFilters.Sensitive);
            
            converter = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            changes = source.Watch("*.md");
            changes.RegisterChangeCallback(o => OnSourceChanged(), changes);
            header = source.GetFileInfo("header.html");
            footer = source.GetFileInfo("footer.html");
            lock (renderedPages)
            {
                renderDirectory(new DirectoryInfo(path)).GetAwaiter().GetResult();
            }
        }

        private async Task renderDirectory(DirectoryInfo root, DirectoryInfo current = null)
        {
            current = current ?? root;

            foreach (var file in current.EnumerateFiles())
            {
                if (file.Extension == ".md")
                {
                    string relPath = file.FullName.AsRelativePathFrom(path);
                    string newName = relPath.Replace(".md", ".html");
                    renderedPages[relPath.Replace(".md", ".html").Replace("\\", "/")] = 
                        new RenderedFileInfo(newName, await renderContent(source.GetFileInfo(relPath)));
                }
            }

            foreach (var dir in current.EnumerateDirectories())
                await renderDirectory(root, dir);
                 
        }

        private async Task<byte[]> renderContent(IFileInfo file)
        {
            long size = (header?.Length ?? 0) + file.Length + (footer?.Length ?? 0);
            using (var stream = new MemoryStream((int)size))            
            {                
                if (header != null)
                    using (var headerStream = header.CreateReadStream())
                        await headerStream.CopyToAsync(stream);
                using (var fileStream = file.CreateReadStream())
                using (var reader = new StreamReader(fileStream))
                using (var writer = new StreamWriter(stream, encoding: Encoding.UTF8, bufferSize: (int)file.Length, leaveOpen: true))
                    await writer.WriteAsync(Markdown.ToHtml(await reader.ReadToEndAsync(), pipeline: converter));
                if (footer != null)
                    using (var footerStream = footer.CreateReadStream())
                        await footerStream.CopyToAsync(stream);
                return stream.ToArray();
            }
        }

        public void OnSourceChanged()
        {
            lock (renderedPages)
            {
                renderedPages.Clear();                
                renderDirectory(new DirectoryInfo(path)).GetAwaiter().GetResult();
            }
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {            
            lock (renderedPages)
                return source.GetDirectoryContents(subpath);            
        }

        public IFileInfo GetFileInfo(string subpath)
        {            
            if (subpath.EndsWith(".html"))
            {
                lock (renderedPages)
                {
                    return renderedPages.ContainsKey(subpath) ? renderedPages[subpath] : null;
                }                
            }
            else
                return null;
        }

        public IChangeToken Watch(string filter)
        {
            return source.Watch(filter);
        }

        public void Dispose()
        {
            callbackDisposer.Dispose();
        }

        public class RenderedFileInfo : IFileInfo
        {
            private readonly string path;
            private readonly byte[] content;
            public RenderedFileInfo(string path, byte[] content)
            {
                this.path = path;
                this.content = content;
                LastModified = DateTimeOffset.Now;
            }

            public bool Exists => true;

            public long Length => content.Length;

            public string PhysicalPath => throw new NotImplementedException();

            public string Name => path;

            public DateTimeOffset LastModified { get; }

            public bool IsDirectory => false;

            public Stream CreateReadStream()
            {
                return new MemoryStream(content);
            }
        }
    }
}
