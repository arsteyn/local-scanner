using System.Web.Http;

namespace Scanner.Webhost
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            //GlobalConfiguration.Configuration.MessageHandlers.Insert(0, new ServerCompressionHandler(new GZipCompressor()/* ,new DeflateCompressor()*/));

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
               name: "DefaultApi",
               routeTemplate: "api/{bookmakerName}/{action}",
               defaults: new { controller = "Home", bookmakerName = RouteParameter.Optional }
           );

        }
    }
}
