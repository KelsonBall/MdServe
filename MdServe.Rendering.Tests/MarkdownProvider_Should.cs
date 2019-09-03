using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.IO;
using System.Text.RegularExpressions;

namespace MdServe.Rendering.Tests
{
    public class MarkdownProvider_Should
    {        
        private async Task<TempFolder> getDemoState()
        {
            var directory = new TempFolder();
            await directory.CreateFolder("content");
            await directory.CreateFile("content/first.md", "# Hello World");
            await directory.CreateFile("content/header.html", "<html>\n<body>\n");
            await directory.CreateFile("content/footer.html", "</body>\n</html>");
            await directory.CreateFolder("content/path");
            await directory.CreateFile("content/path/second.md", "# Foo\n## Bar\n### Baz");
            return directory;
        }


        [Fact]
        public async Task RenderStartingContent()
        {
            await using (var directory = await getDemoState())
            {
                var markdown = new MarkdownProvider(directory.FullName);
                string textFirst = await markdown.GetFileInfo("first.html").ReadTextAsync();
                textFirst.Should().BeEquivalentTo("<html>\n<body>\n<h1 id=\"hello-world\">Hello World</h1>\n</body>\n</html>");
                string textSecond = await markdown.GetFileInfo("path/second.html").ReadTextAsync();
                textSecond.Should().BeEquivalentTo("<html>\n<body>\n<h1 id=\"foo\">Foo</h1>\n<h2 id=\"bar\">Bar</h2>\n<h3 id=\"baz\">Baz</h3>\n</body>\n</html>");
            }
        }

        [Fact]
        public async Task RerenderChangedFiles()
        {
            await using (var directory = await getDemoState())
            {
                var markdown = new MarkdownProvider(directory.FullName);
                string textFirst = await markdown.GetFileInfo("first.html").ReadTextAsync();
                textFirst.Should().BeEquivalentTo("<html>\n<body>\n<h1 id=\"hello-world\">Hello World</h1>\n</body>\n</html>");
                await directory.CreateFile("content/first.md", "# Foo");
                //markdown.OnSourceChanged();   
                await Task.Delay(1000);
                textFirst = await markdown.GetFileInfo("first.html").ReadTextAsync();
                textFirst.Should().BeEquivalentTo("<html>\n<body>\n<h1 id=\"foo\">Foo</h1>\n</body>\n</html>");                
            }
        }

        [Fact]
        public async Task RenderNewFiles()
        {
            await using (var directory = await getDemoState())
            {
                var markdown = new MarkdownProvider(directory.FullName);
                markdown.GetFileInfo("third.html").Should().BeNull();
                
                await directory.CreateFile("content/third.md", "hello");
                
                await Task.Delay(1000);
                string textThird = await markdown.GetFileInfo("third.html").ReadTextAsync();
                textThird.Should().BeEquivalentTo("<html>\n<body>\n<p>hello</p>\n</body>\n</html>");
            }
        }

        // Clean up temp folders of previous failed tests before each run
        static MarkdownProvider_Should()
        {
            var temp_files = new Regex("temp_[a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9][a-z0-9]");
            foreach (var dir in new DirectoryInfo(Directory.GetCurrentDirectory()).EnumerateDirectories())
            {
                if (temp_files.IsMatch(dir.Name))
                {
                    try
                    {
                        Directory.Delete(dir.FullName, true);
                    }
                    catch (IOException)
                    {

                    }
                }
            }
        }
    }
}
