using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Cosmos.Cms.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScriptsController : ControllerBase
    {

        private readonly IAntiforgery antiforgery;
        private readonly IWebHostEnvironment env;

        public ScriptsController(
            IAntiforgery antiforgery,
            IWebHostEnvironment env)
        {
            this.antiforgery = antiforgery;
            this.env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetScript(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Script name cannot be null or empty.");
            }
            var filePath = Path.Combine(env.WebRootPath, "scripts", $"{name}.js");
            if (!System.IO.File.Exists(filePath))
            {
               return BadRequest(filePath + " does not exist.");
            }
            var tokens = antiforgery.GetAndStoreTokens(HttpContext);
            Response.Cookies.Append("X-XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                Domain = Request.Host.Host, // 🔒 Restrict to this domain only
                Path = "/",
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/javascript", name);
        }

    }
}
