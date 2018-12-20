using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace MyCompany.IDP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();
            //add '.well-known/openid-configuration' at the end of the URL to see a JSON file with the various endpoints, scopes, claims, etc.
        }
    }
}
