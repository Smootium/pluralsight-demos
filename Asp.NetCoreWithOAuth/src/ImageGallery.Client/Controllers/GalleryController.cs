using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using ImageGallery.Client.Services;
using ImageGallery.Client.ViewModels;
using ImageGallery.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace ImageGallery.Client.Controllers
{
    [Authorize()]
    public class GalleryController : Controller
    {
        private readonly IImageGalleryHttpClient _imageGalleryHttpClient;
        private readonly IHttpContextAccessor _accessor;

        public GalleryController(IImageGalleryHttpClient imageGalleryHttpClient, IHttpContextAccessor accessor)
        {
            _imageGalleryHttpClient = imageGalleryHttpClient;
            _accessor = accessor;
        }

        public async Task<IActionResult> Index()
        {
            await WriteOutIdentityInformation(_accessor);
            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.GetAsync("api/images").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var imagesAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                var galleryIndexViewModel = new GalleryIndexViewModel(
                    JsonConvert.DeserializeObject<IList<Image>>(imagesAsString).ToList());

                return View(galleryIndexViewModel);
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public async Task<IActionResult> OrderFrame()
        {
            //create a uri to call user info endpoint with
            //use discovery client to read metadata instead
            //Obsolete soon:
            //var discoveryClient = new DiscoveryClient("https://localhost:44305/");
            //var metadataResponse = await discoveryClient.GetAsync();
            //var userInfoClient = new UserInfoClient(metadataResponse.UserInfoEndpoint);

            var client = new HttpClient();
            var discoveryClient = await client.GetDiscoveryDocumentAsync("https://localhost:44305/");
            if (discoveryClient.IsError)
                throw new Exception(discoveryClient.Error);
            var userInfoEndpoint = discoveryClient.UserInfoEndpoint;

            //Get access token
            var a_Token = await _accessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            var response = await client.GetUserInfoAsync(new UserInfoRequest
            {
                Address = userInfoEndpoint,
                Token = a_Token
            });

            if (response.IsError)
                throw new Exception("Problem accessing the UserInfo endpoint", response.Exception);

            var address = response.Claims.FirstOrDefault(c => c.Type == "address")?.Value;

            return View(new OrderFrameViewModel(address));
        }

        public async Task Logout()
        {
            //Signing out this way will sign out of the Web Client, not the IDP; need a redirect
            //The scheme we pass in must match the scheme name when configuring the cookie authentication middleware
            await AuthenticationHttpContextExtensions.SignOutAsync(_accessor.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme);

            //this will also logout of the IDC, but without setting a logout redirect either in the web client (i.e., ImageGallery.Client.Startup) or the IDP (i.e., MyCompany.IDP.Config)
            //the user has to then click Logout on the IDP, which isn't great UX.
            //Debug output shows "Invalid post logout URI" and that default is '...:44367/signout-callback-oidc')

            //The scheme we pass in must match the scheme name when configuring the OpenId middleware (in Startup.cs)
            await AuthenticationHttpContextExtensions.SignOutAsync(_accessor.HttpContext, OpenIdConnectDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> EditImage(Guid id)
        {
            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.GetAsync($"api/images/{id}").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var imageAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserializedImage = JsonConvert.DeserializeObject<Image>(imageAsString);

                var editImageViewModel = new EditImageViewModel()
                {
                    Id = deserializedImage.Id,
                    Title = deserializedImage.Title
                };

                return View(editImageViewModel);
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImage(EditImageViewModel editImageViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // create an ImageForUpdate instance
            var imageForUpdate = new ImageForUpdate()
            { Title = editImageViewModel.Title };

            // serialize it
            var serializedImageForUpdate = JsonConvert.SerializeObject(imageForUpdate);

            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.PutAsync(
                $"api/images/{editImageViewModel.Id}",
                new StringContent(serializedImageForUpdate, System.Text.Encoding.Unicode, "application/json"))
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public async Task<IActionResult> DeleteImage(Guid id)
        {
            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.DeleteAsync($"api/images/{id}").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public IActionResult AddImage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImage(AddImageViewModel addImageViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // create an ImageForCreation instance
            var imageForCreation = new ImageForCreation()
            { Title = addImageViewModel.Title };

            // take the first (only) file in the Files list
            var imageFile = addImageViewModel.Files.First();

            if (imageFile.Length > 0)
            {
                using (var fileStream = imageFile.OpenReadStream())
                using (var ms = new MemoryStream())
                {
                    fileStream.CopyTo(ms);
                    imageForCreation.Bytes = ms.ToArray();
                }
            }

            // serialize it
            var serializedImageForCreation = JsonConvert.SerializeObject(imageForCreation);

            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.PostAsync(
                $"api/images",
                new StringContent(serializedImageForCreation, System.Text.Encoding.Unicode, "application/json"))
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        //Just some debug/helper code
        private async Task WriteOutIdentityInformation(IHttpContextAccessor accessor)
        {
            //get the saved identity token
            // used to be 'await HttpContext.Authentication.GetTokenAsync(OpenIdConnectParameterNames.IdToken);' but was changed in 2.0
            var identityToken = await AuthenticationHttpContextExtensions.GetTokenAsync(accessor.HttpContext, OpenIdConnectParameterNames.IdToken);

            Debug.WriteLine($"Identity token: {identityToken}");

            foreach (var claim in User.Claims)
            {
                Debug.WriteLine($"Claim type: {claim.Type} - Claim value: {claim.Value}");
            }
        }
    }
}
