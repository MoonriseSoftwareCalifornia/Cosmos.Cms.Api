using BenjaminAbt.HCaptcha;
using Microsoft.AspNetCore.Mvc;

namespace Cosmos.Cms.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class VerifyController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] HCaptchaVerifyResponse hCaptcha)
        {
            if (hCaptcha.Success)
            {
                return Ok("hCaptcha verification passed.");
            }

            return BadRequest("hCaptcha verification failed.");
        }
    }

}
