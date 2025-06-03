using BenjaminAbt.HCaptcha;
using Cosmos.Common.Data;
using Cosmos.Common.Models;
using Cosmos.Common.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cosmos.Cms.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactsController : ControllerBase
    {

        private readonly IAntiforgery antiforgery;
        private readonly ApplicationDbContext dbContext;
        private readonly IEmailSender emailSender;
        private readonly ILogger<ContactsController> logger;
        private readonly IOptions<HCaptchaOptions> captchaOptions;
        private readonly IConfiguration configuration;

        public ContactsController(
            ILogger<ContactsController> logger,
            IAntiforgery antiforgery,
            ApplicationDbContext dbContext,
            IEmailSender emailSender,
            IOptions<HCaptchaOptions> captchaOptions,
            IConfiguration configuration)
        {
            this.logger = logger;
            this.antiforgery = antiforgery;
            this.dbContext = dbContext;
            this.emailSender = emailSender;
            this.captchaOptions = captchaOptions;
            this.configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult> PostContact([FromBody] ContactViewModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            if (!await antiforgery.IsRequestValidAsync(HttpContext))
            {
                return BadRequest("Invalid antiforgery token.");
            }

            if (!await VerifyHCaptchaAsync(Request.Headers["h-captcha-response"].ToString()))
            {
                return BadRequest("hCaptcha verification failed.");
            }


            var domainName = DynamicConfig.DynamicConfigurationProvider.GetTenantDomainNameFromCookieOrHost(configuration, HttpContext);
            var validateDomain = await DynamicConfig.DynamicConfigurationProvider.ValidateDomainName(configuration, domainName);

            if (!validateDomain)
            {
                return BadRequest("Invalid domain name.");
            }

            model.Id = Guid.NewGuid();
            model.Created = DateTimeOffset.UtcNow;
            model.Updated = DateTimeOffset.UtcNow;

            var contactService = new ContactManagementService(dbContext, emailSender, logger, this.HttpContext);

            var result = await contactService.AddContactAsync(model);
            // In a real application, you would save the forecast to a database or other storage.
            // For demonstration, just return the received object.
            return Ok("Contact saved.");
        }

        private async Task<bool> VerifyHCaptchaAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var secret = captchaOptions.Value.Secret; // Store securely!
            using var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secret),
                new KeyValuePair<string, string>("response", token)
            });

            var response = await httpClient.PostAsync("https://hcaptcha.com/siteverify", content);
            var json = await response.Content.ReadAsStringAsync();

            // You can create a class to deserialize the response
            var result = System.Text.Json.JsonSerializer.Deserialize<HCaptchaVerifyResponse>(json);

            return result != null && result.Success;
        }

    }
}
