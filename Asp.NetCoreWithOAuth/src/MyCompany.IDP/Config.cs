using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;

namespace MyCompany.IDP
{
    public static class Config
    {
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "D7022502-84B8-4371-9B55-AD040580E319",
                    Username = "George",
                    Password = "George",

                    Claims = new List<Claim>
                    {
                        new Claim("given_name","George"),
                        new Claim("family_name","Monkey")
                    }
                },
                new TestUser
                {
                    SubjectId = "61F635E1-40A8-413C-AD2B-334485A1D179",
                    Username = "YellowHat",
                    Password = "YellowHat",

                    Claims = new List<Claim>
                    {
                        new Claim("given_name","YellowHat"),
                        new Claim("family_name","Man")
                    }
                }
            };
        }

        //IdentityResource map to scopes that give access to Identity-related info
        //ApiResource map to scope that give access to Api resources
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                /*OpenId scope ensures the user identifier (aka, SubjectId) is included
                  I.e., if the client requests the OpenId scope, the Subject claim is returned
                  The profile scope maps to profile-related claims.
                  I.e., if the client requests the Profile scope, the given_name and familiy_name claims are returned.
                */
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientName = "Image Gallery",
                    ClientId = "imagegalleryclient",
                    Enabled = true,
                    AllowedGrantTypes = GrantTypes.Hybrid,
                    RedirectUris = new List<string>()
                    {
                        "https://localhost:44367/signin-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile
                    },
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    PostLogoutRedirectUris =
                    {
                        "https://localhost:44367/signout-callback-oidc"
                    }
                }
            };
        }
    }
}
