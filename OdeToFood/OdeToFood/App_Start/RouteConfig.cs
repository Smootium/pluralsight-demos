using System.Web.Mvc;
using System.Web.Routing;

namespace OdeToFood
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            //order of routes matter. The routing engine will use the first route it finds that matches the request

            //if 'name' was not made optional (either through name = "something" or UrlParameter.Optional), then any request that didn't specify the name would not FAD
            //i.e., 'cuisine/french' will route to the Cuisine controller, but 'cuisine' would not
            //Can also specify the default value in the Controller itself

            //The engine will also 'find' the name in a query string, such as:
            // /cuisine?name=italian
            //will use 'italian' as the name in the parameter
            routes.MapRoute(
                name: "Cuisine",
                url: "cuisine/{name}",
                defaults: new { controller = "Cuisine", action = "Search", name = UrlParameter.Optional }
                );

            //This is greedy and will match just about any request

            // host/Home/Index  --> Home is controller; Index is action
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "BIndex", id = UrlParameter.Optional }
            );
        }
    }
}
