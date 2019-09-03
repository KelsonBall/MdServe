using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;

namespace Kelson.Common.Web
{
    public class ServerBuilder
    {
        internal readonly string[] urls;        
        internal readonly List<RoutePathBuilder> get = new List<RoutePathBuilder>();
        internal readonly List<RoutePathBuilder> post = new List<RoutePathBuilder>();
        internal readonly List<RoutePathBuilder> delete = new List<RoutePathBuilder>();
        internal readonly List<RoutePathBuilder> head = new List<RoutePathBuilder>();
        internal readonly List<RoutePathBuilder> put = new List<RoutePathBuilder>();

        internal Action<IServiceCollection> servicesCallback;
        internal Action<IApplicationBuilder> configurationCallback;

        public ServerBuilder(params string[] urls) => (this.urls, servicesCallback, configurationCallback) = (urls, configureServices, configureApplication);


        private void configureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        private void configureApplication(IApplicationBuilder app)
        {
            var rb = new RouteBuilder(app);

            foreach (var path in this.get)
                if (path.does is Some<RouteActionBuilder> action)
                    rb.MapGet(path.path, createAspNetHandler(action.Message));

            foreach (var path in this.post)
                if (path.does is Some<RouteActionBuilder> action)
                    rb.MapPost(path.path, createAspNetHandler(action.Message));

            foreach (var path in this.delete)
                if (path.does is Some<RouteActionBuilder> action)
                    rb.MapDelete(path.path, createAspNetHandler(action.Message));

            foreach (var path in this.put)
                if (path.does is Some<RouteActionBuilder> action)
                    rb.MapPut(path.path, createAspNetHandler(action.Message));

            app.UseRouter(rb.Build());
        }


        public ServerBuilder ConfigureServices(Action<IServiceCollection> next)
        {
            Action<IServiceCollection> previous = servicesCallback;
            servicesCallback = services =>
            {
                previous(services);
                next(services);
            };
            return this;
        }

        public ServerBuilder ConfigureApplication(Action<IApplicationBuilder> next)
        {
            Action<IApplicationBuilder> previous = configurationCallback;
            configurationCallback = app =>
            {
                previous(app);
                next(app);
            };
            return this;
        }

        private static Func<HttpRequest, HttpResponse, RouteData, Task> createAspNetHandler(RouteActionBuilder actionBuilder)
        {
            return async (req, res, route) =>
            {
                var context = new WebContext(req, res, route);
                var result = await actionBuilder.execute(context);
                await result.ExecuteResultAsync(new ActionContext(context, route, actionBuilder.descriptor));
                await context.Response.Body.FlushAsync();                
            };
        }


        public struct RoutePathBuilder
        {
            internal readonly ServerBuilder server;
            internal readonly HttpMethod method;
            internal readonly string path;
            internal Option<RouteParamBuilder> @params;
            internal Option<RouteActionBuilder> does;

            internal delegate void AddDelegate(RoutePathBuilder value);
            internal readonly AddDelegate addToDispatchList;

            public RoutePathBuilder(ServerBuilder server, HttpMethod method, string[] path)
            {
                this.server = server;
                this.method = method;
                this.path = string.Join("/", path);
                @params = false;
                does = false;
                addToDispatchList = method switch
                {
                    HttpMethod.Get => (AddDelegate)server.get.Add,
                    HttpMethod.Post => (AddDelegate)server.post.Add,
                    HttpMethod.Delete => (AddDelegate)server.delete.Add,
                    HttpMethod.Put => (AddDelegate)server.put.Add,
                    HttpMethod.Head => (AddDelegate)server.head.Add,
                    _ => throw new NotSupportedException()
                };
            }

            public RoutePathBuilder WithParams(Func<RouteParamBuilder, RouteParamBuilder> paramDelegate)
            {
                @params = paramDelegate(new RouteParamBuilder(this));
                return this;
            }

            public RouteBodyBuilder<T> WithBodyAs<T>(Func<StreamReader, Task<T>> parse) => new RouteBodyBuilder<T>(this, parse);

            public ServerBuilder Do(Func<WebContext, Task<IActionResult>> action)
            {
                does = new RouteActionBuilder(this, action);
                addToDispatchList(this);
                return server;
            }

            public ServerBuilder Do(Func<WebContext, IActionResult> action)
            {
                return Do(c => new ValueTask<IActionResult>(action(c)).AsTask());
            }
        }

        public struct RouteParamBuilder
        {
            internal RoutePathBuilder path;

            public RouteParamBuilder(RoutePathBuilder path) => this.path = path;

            public class RouteParamConverterBuilder
            {
                internal readonly RouteParamBuilder param;

                public RouteParamConverterBuilder(RouteParamBuilder param, string name) => this.param = param;

                public RouteParamBuilder As<T>(Func<string, T> conversion) => param;
            }

            public RouteParamConverterBuilder Named(string name) => new RouteParamConverterBuilder(this, name);

            public RouteActionOnBodyBuilder<T> WithBodyAs<T>(Func<StreamReader, Task<T>> parse) => new RouteActionOnBodyBuilder<T>(this.path, parse);

            public ServerBuilder Do(Func<WebContext, Task<IActionResult>> action)
            {
                path.does = new RouteActionBuilder(this.path, action);
                return path.server;
            }

            public ServerBuilder Do(Func<WebContext, IActionResult> action)
            {
                return Do(c => new ValueTask<IActionResult>(action(c)).AsTask());
            }
        }

        public struct RouteBodyBuilder<T>
        {
            internal RoutePathBuilder path;
            internal Func<StreamReader, Task<T>> read;

            public RouteBodyBuilder(RoutePathBuilder path, Func<StreamReader, Task<T>> parse) => (this.path, this.read) = (path, parse);

            public ServerBuilder Do(Func<WebContext, T, Task<IActionResult>> action)
            {
                Func<StreamReader, Task<T>> parse = read;
                async Task<IActionResult> parseAndPass(WebContext context)
                {
                    using var reader = new StreamReader(context.Request.Body);
                    return await action(context, await parse(reader));
                }

                path.does = new RouteActionBuilder(path, parseAndPass);
                return path.server;
            }

            public ServerBuilder Do(Func<WebContext, T, IActionResult> action)
            {
                return Do((c, t) => new ValueTask<IActionResult>(action(c, t)).AsTask());
            }
        }

        public struct RouteActionBuilder
        {
            internal readonly ActionDescriptor descriptor;
            internal readonly RoutePathBuilder path;
            internal Func<WebContext, Task<IActionResult>> action;

            internal RouteActionBuilder(RoutePathBuilder path, Func<WebContext, Task<IActionResult>> action) => (this.path, this.action, descriptor) = (path, action, new ActionDescriptor());

            public async Task<IActionResult> execute(WebContext context) => await action(context);

        }

        public struct RouteActionOnBodyBuilder<T>
        {
            internal RoutePathBuilder path;
            internal readonly Func<StreamReader, Task<T>> read;

            internal RouteActionOnBodyBuilder(RoutePathBuilder path, Func<StreamReader, Task<T>> parse) => (this.path, this.read) = (path, parse);

            public ServerBuilder Do(Func<WebContext, T, Task<IActionResult>> action)
            {
                Func<StreamReader, Task<T>> parse = read;
                async Task<IActionResult> parseAndPass(WebContext context)
                {
                    using var reader = new StreamReader(context.Request.Body);
                    return await action(context, await parse(reader));
                }

                path.does = new RouteActionBuilder(path, parseAndPass);
                return path.server;
            }

            public ServerBuilder Do(Func<WebContext, T, IActionResult> action)
            {
                return Do((c, t) => new ValueTask<IActionResult>(action(c, t)).AsTask());
            }
        }

        public IWebHost Build() =>
                WebHost.CreateDefaultBuilder()
                    .UseUrls(urls)
                    .ConfigureServices(servicesCallback)
                    .Configure(configurationCallback)               
                    .Build();        

        public RoutePathBuilder OnGet(params string[] path) => new RoutePathBuilder(this, HttpMethod.Get, path);
        
        public RoutePathBuilder OnPost(params string[] path) => new RoutePathBuilder(this, HttpMethod.Post, path);

        public RoutePathBuilder DELETE(params string[] path) => new RoutePathBuilder(this, HttpMethod.Delete, path);

        public RoutePathBuilder HEAD(params string[] path) => new RoutePathBuilder(this, HttpMethod.Head, path);

        public RoutePathBuilder PUT(params string[] path) => new RoutePathBuilder(this, HttpMethod.Put, path);

    }
}
