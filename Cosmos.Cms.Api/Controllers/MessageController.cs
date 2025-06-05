using BenjaminAbt.HCaptcha;
using Cosmos.Cms.Api.Models;
using Cosmos.Common.Data;
using Cosmos.Common.Services;
using Cosmos.DynamicConfig;
using Cosmos.EmailServices;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace Cosmos.Cms.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {

        private readonly IAntiforgery antiforgery;
        private readonly ApplicationDbContext dbContext;
        private readonly DynamicConfigDbContext dynamicConfigDbContext;
        private readonly ICosmosEmailSender emailSender;
        private readonly ILogger<MessageController> logger;
        private readonly IOptions<HCaptchaOptions> captchaOptions;
        private readonly IConfiguration configuration;

        public MessageController(
            ILogger<MessageController> logger,
            IAntiforgery antiforgery,
            ApplicationDbContext dbContext,
            DynamicConfigDbContext dynamicConfigDbContext,
            IEmailSender emailSender,
            IOptions<HCaptchaOptions> captchaOptions,
            IConfiguration configuration)
        {
            this.logger = logger;
            this.antiforgery = antiforgery;
            this.dbContext = dbContext;
            this.dynamicConfigDbContext = dynamicConfigDbContext;
            this.emailSender = (ICosmosEmailSender) emailSender;
            this.captchaOptions = captchaOptions;
            this.configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult> PostMessage([FromBody] MessageViewModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            if (!await antiforgery.IsRequestValidAsync(HttpContext))
            {
                return BadRequest("Invalid antiforgery token.");
            }

            if (!await hCaptchaUtilities.VerifyHCaptchaAsync(captchaOptions, Request.Headers["h-captcha-response"].ToString()))
            {
                return BadRequest("hCaptcha verification failed.");
            }

            var domainName = DynamicConfig.DynamicConfigurationProvider.GetTenantDomainNameFromCookieOrHost(configuration, HttpContext);
            var validateDomain = await DynamicConfig.DynamicConfigurationProvider.ValidateDomainName(configuration, domainName);
            var connection = await dynamicConfigDbContext.Connections.FirstOrDefaultAsync(c => c.DomainNames.Contains(domainName));

            if (connection == null)
            {
                return BadRequest("No connection found for the provided domain.");
            }

            if (!validateDomain)
            {
                return BadRequest("Invalid domain name.");
            }

            model.Id = Guid.NewGuid();
            model.Created = DateTimeOffset.UtcNow;
            model.Updated = DateTimeOffset.UtcNow;
            bool.TryParse(model.JoinMailingList, out bool join);
            if (join)
            {
                var contactService = new ContactManagementService(dbContext, emailSender, logger, this.HttpContext);
                var result = await contactService.AddContactAsync(model);
            }

            var messageContext = new StringBuilder();

            messageContext.AppendLine($"Message received from:<br /><br />{model.FirstName} {model.LastName}");
            messageContext.AppendLine($"<br />Email address: {model.Email}");
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                messageContext.AppendLine($"<br />Phone: {model.Phone}");
            }
            if (join)
            {
                messageContext.AppendLine($"<br />Note: User joined the contacts list.");
            }
            messageContext.AppendLine($"<br />");
            if (!string.IsNullOrWhiteSpace(model.Subject))
            {
                messageContext.AppendLine($"<br />Subject: {model.Subject}");
            }
            messageContext.AppendLine($"<br />Message:<br />{model.Message}");

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(messageContext.ToString());

            if (!string.IsNullOrWhiteSpace(connection.OwnerEmail))
            {
                // Send email to the owner of the connection
                await emailSender.SendEmailAsync(connection.OwnerEmail,
                    string.IsNullOrWhiteSpace(model.Subject) ? "New Website Message" : model.Subject,
                    htmlDoc.DocumentNode.InnerText, htmlDoc.DocumentNode.OuterHtml, "support@moonrise.net");
            }

            // In a real application, you would save the forecast to a database or other storage.
            // For demonstration, just return the received object.
            return Ok("Message sent.");
        }
    }
}
