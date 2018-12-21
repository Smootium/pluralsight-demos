using ImageGallery.Client.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Client
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            /*
             * In Auth 1.0, every auth scheme had its own middleware, which was set up in Configure method (below) such like:
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        AuthenticationScheme = "Cookies"
                    });
             * You also had to add the Auth middleware before MVC middleware, as the Auth middleware would allow/block requests
             * In Auth 2.0, there is now only a single Authentication middleware, and each authentication scheme is registered during ConfigureServices
             * See https://github.com/aspnet/Security/issues/1310
             * 
             * Yet TBD: Is it still the case that this has to be added before Mvc is added?
            */
            /*
             * OpenIdConnectAuthentication was like:
             * app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
             * {
             *      AuthenticationScheme="oidc",
             *      Authority = "",
             *      RequireHttpsMetadata = true,
             *      ClientId = "imagegalleryclient",
             *      Scope = {"openid", "profile" },  // this is set in IDP's Config class (GetClients.Client.AllowedScopes)
             *      ResponseType = "code id_token"
             *      CallbackPath = new PathString("...")
             * });
             */

            services
                .AddAuthentication(o =>
                {
                    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, o =>
                {
                    // this is the uri as specified on the IDP's Debug property page
                    o.Authority = "https://localhost:44305/";

                    o.RequireHttpsMetadata = true;

                    // this is set in IDP's Config class
                    o.ClientId = "imagegalleryclient";

                    // this is one of the response types for flow (hybrid grant)
                    o.ResponseType = "code id_token";

                    // this should match the scheme used for authentication
                    o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                    o.SaveTokens = true;
                    o.ClientSecret = "secret";
                    // this is default, but if you wanted to use a different redirect uri endpoint than what's set in IDP's Config class's RedirectUris, you can set it here
                    //o.CallbackPath = new PathString("...");
                });

            // Add framework services.
            services.AddMvc();

            // register an IHttpContextAccessor so we can access the current
            // HttpContext in services by injecting it
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // register an IImageGalleryHttpClient
            services.AddScoped<IImageGalleryHttpClient, ImageGalleryHttpClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
            }

            app.UseAuthentication();
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Gallery}/{action=Index}/{id?}");
            });
        }
    }
}
