using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Kelson.Common.Web
{
    public class WebContext : HttpContext
    {
        private readonly HttpContext _context;        
        
        public RouteData Route { get; private set; }

        public WebContext(RouteContext context) => (_context, Route) = (context.HttpContext, context.RouteData);
        

        public WebContext(HttpRequest request, HttpResponse response, RouteData data) => (_context, Route) = (request.HttpContext, data);


        public override IFeatureCollection Features => _context.Features;

        public override HttpRequest Request => _context.Request;

        public override HttpResponse Response => _context.Response;

        public override ConnectionInfo Connection => _context.Connection;

        public override WebSocketManager WebSockets => _context.WebSockets;

        public override AuthenticationManager Authentication => throw new NotImplementedException();

        public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IDictionary<object, object> Items { get => _context.Items; set => _context.Items = value; }
        public override IServiceProvider RequestServices { get => _context.RequestServices; set => _context.RequestServices = value; }
        public override CancellationToken RequestAborted { get => _context.RequestAborted; set => _context.RequestAborted = value; }
        public override string TraceIdentifier { get => _context.TraceIdentifier; set => _context.TraceIdentifier = value; }
        public override ISession Session { get => _context.Session; set => _context.Session = value; }

        public override void Abort() => _context.Abort();
        
    }
}
