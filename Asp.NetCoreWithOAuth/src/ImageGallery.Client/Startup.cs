using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
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
             * 
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
            // Add framework services.
            services.AddMvc();

            services
                .AddAuthentication(o =>
                {
                    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, o =>
                {
                    //The default mapping dictionary that's included will map claim types to their WS Security counterparts.
                    //The WS Security types come in handy when updating older apps (that rely on this), but for our app, it's not necessary.
                    //This way, the claims returned from IDP will remain as set/expected.
                    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

                    // this is the uri as specified on the IDP's Debug property page
                    o.Authority = "https://localhost:44305/";

                    o.RequireHttpsMetadata = true;

                    // this is set in IDP's Config class
                    o.ClientId = "imagegalleryclient";

                    //By default, OpenIdConnectOptions.Scope is "pre-loaded" with "openid" and "profile" scopes.
                    //Add'l scopes that we want included in the auth token (that eventually is requested from user info endpoint) must be added here
                    o.Scope.Add("address");

                    // this is one of the response types for flow (hybrid grant)
                    o.ResponseType = "code id_token";

                    // this apparently isn't needed (now; at some point while trying to get this to work, I added it. Not sure if it was ever needed?
                    // this should match the scheme used for authentication
                    //o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                    o.SaveTokens = true;
                    o.ClientSecret = "secret";

                    // this ensures that the middleware will call the UserInfo endpoint to get the info on the User
                    // instead of including the Claims in the Id token (which keeps the token smaller)
                    o.GetClaimsFromUserInfoEndpoint = true;

                    //Let's cleanup the unnecessary claims (keeps the cookie smaller)
                    o.Events = new OpenIdConnectEvents()
                    {
                        OnTokenValidated = context =>
                        {
                            //With AspNetCore v2, and with the hybrid flow (i.e., "code id_token"), the "sub" key is no longer returned via the "sub" claim in id_token
                            //and instead, is returned from the User Info endpoint.
                            //var subjectClaim = context.Principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                            //var newClaimsIdentity = new ClaimsIdentity(context.Scheme.Name, "given_name", "role");
                            //newClaimsIdentity.AddClaim(subjectClaim);

                            //context.Principal = new ClaimsPrincipal(newClaimsIdentity);
                            //context.Success();

                            return Task.FromResult(0);
                        },
                        OnUserInformationReceived = context =>
                        {
                            //we remove the address from the json object from the json response with the claims returned from the user info endpoint
                            //This does not manipulate the Claims identity here; we're removing the address before the middleware has a chance to add it to the Claims identity
                            //This allows us to not include unnecessary info (which can be obtained from the user endpoint later, as needed)
                            context.User.Remove("address");

                            return Task.FromResult(0);
                        }
                    };
                    // this is default, but it can be changed here or in the IDP's Config class's PostLogoutRedirectUris
                    //o.SignedOutCallbackPath = new PathString("signout-callback-oidc");

                    // this is default, but if you wanted to use a different redirect uri endpoint than what's set in IDP's Config class's RedirectUris, you can set it here
                    //o.CallbackPath = new PathString("...");
                });

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
