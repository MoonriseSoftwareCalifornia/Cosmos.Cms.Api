using BenjaminAbt.HCaptcha;
using Microsoft.Extensions.Options;

namespace Cosmos.Cms.Api
{
    public static class hCaptchaUtilities
    {
        public static async Task<bool> VerifyHCaptchaAsync(IOptions<HCaptchaOptions> captchaOptions, string token)
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
