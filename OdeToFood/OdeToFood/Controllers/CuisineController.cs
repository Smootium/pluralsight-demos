using System.Web.Mvc;

namespace OdeToFood.Controllers
{
    public class CuisineController : Controller
    {
        // GET: Cuisine
        public ActionResult Search(string name = "french")
        {
            //Since we're returning Content, we need to make sure to encode the string, to prevent someone from supplying a malicious script
            //Razor view engine would do this by default
            //HtmlEncode will simply return text
            var message = Server.HtmlEncode(name);

            return Content(name);
        }
    }
}