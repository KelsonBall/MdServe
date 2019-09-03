using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MdServeApp
{
    public readonly struct Configuration
    {
        public string Host { get; }
        public short Port { get; }

        public string WwwRoot { get; }

        public Configuration(string configFile)
        {
            Host = "https://localhost:5000";
            WwwRoot = "serve/";
            Port = 5000;
        }

        public async Task<Configuration> Use(Func<Configuration, Task> configAction)
        {
            await configAction(this);
            return this;
        }
    }
}
