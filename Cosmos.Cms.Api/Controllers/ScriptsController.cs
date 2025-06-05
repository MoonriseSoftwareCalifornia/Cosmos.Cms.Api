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
        private readonly ILogger<ScriptsController> logger;
        private readonly IConfiguration configuration;

        public ScriptsController(
            IAntiforgery antiforgery,
            IWebHostEnvironment env,
            ILogger<ScriptsController> logger,
            IConfiguration configuration)
        {
            this.antiforgery = antiforgery;
            this.env = env;
            this.logger = logger;
            this.configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetScript(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                logger.LogError("Script name is null or empty.");
                return BadRequest("Script name cannot be null or empty.");
            }

            var domainName = DynamicConfig.DynamicConfigurationProvider.GetTenantDomainNameFromCookieOrHost(configuration, HttpContext);
            var validateDomain = await DynamicConfig.DynamicConfigurationProvider.ValidateDomainName(configuration, domainName);

            if (!validateDomain)
            {
                logger.LogError("Invalid domain name: {DomainName}", domainName);
                return BadRequest("Invalid domain name.");
            }

            var filePath = Path.Combine(env.WebRootPath, "scripts", $"{name}.js");
            if (!System.IO.File.Exists(filePath))
            {
                logger.LogError("Script file not found: {FilePath}", filePath);
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
