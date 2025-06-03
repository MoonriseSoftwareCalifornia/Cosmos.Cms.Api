using BenjaminAbt.HCaptcha.AspNetCore;
using Cosmos.Common.Data;
using Cosmos.DynamicConfig;
using Cosmos.EmailServices;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// These three services have to appear before DB Context.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IDynamicConfigurationProvider, DynamicConfigurationProvider>();

// Add database context.
builder.Services.AddTransient((serviceProvider) =>
{
    return new ApplicationDbContext(serviceProvider);
});

// Add Email services
builder.Services.AddCosmosEmailServices(builder.Configuration);

// Add hCaptcha service
builder.Services.AddHCaptcha(builder.Configuration.GetSection("HCaptcha"));

// Add controllers with hCaptcha model binder.
builder.Services.AddControllers(options => options.AddHCaptchaModelBinder());

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Antiforgery service
// Anti-forgery token service
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN"; // Custom header name for the anti-forgery token
});

//  The following example permits 10 requests per minute by user (identity) or globally:
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
    
    var filePath = Path.Combine(app.Environment.WebRootPath, "index.html");
    var content = System.IO.File.ReadAllText(filePath);
    // Replace the existing app.MapGet("/", () => content); with the following:
    app.MapGet("/", (HttpContext context) =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        return Results.Text(content, "text/html; charset=utf-8");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
