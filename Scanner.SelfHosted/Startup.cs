using System.Web.Http;
using Owin;

namespace Scanner.SelfHosted
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{bookmakerName}/{action}",
                defaults: new { controller = "Home", bookmakerName = RouteParameter.Optional }
            ); 

            app.UseWebApi(config);
        }
    }
}
