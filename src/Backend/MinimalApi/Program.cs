using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
namespace MinimalApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //builder.Configuration.ConfigureAzureKeyVault();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddOutputCache();
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();
        builder.Services.AddCrossOriginResourceSharing();
        builder.Services.AddAzureServices();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<ITokenProvider, TokenProvider>();

        builder.Services.AddHttpClient();
		HttpClient.DefaultProxy = new WebProxy()
		{
			BypassProxyOnLocal = false,
			UseDefaultCredentials = true
		};

		builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(options =>
            {
                builder.Configuration.Bind("AzureAd", options);
                var tenantId = builder.Configuration["AzureAd:TenantId"];

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuers = new[]
                    {
                $"https://sts.windows.net/{tenantId}/",
                $"https://login.microsoftonline.com/{tenantId}/v2.0"
                    },
                };
            }, options => { builder.Configuration.Bind("AzureAd", options); });



        builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN-HEADER"; options.FormFieldName = "X-CSRF-TOKEN-FORM"; });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDistributedMemoryCache();
        }
        else
        {
#pragma warning disable CS8321 // Local function is declared but never used
            static string GetEnvVar(string key) => Environment.GetEnvironmentVariable(key);
#pragma warning restore CS8321 // Local function is declared but never used

            // set application telemetry
            //if (GetEnvVar("APPLICATIONINSIGHTS_CONNECTION_STRING") is string appInsightsConnectionString && !string.IsNullOrEmpty(appInsightsConnectionString))
            //{
            //    builder.Services.AddApplicationInsightsTelemetry((option) =>
            //    {
            //        option.ConnectionString = appInsightsConnectionString;
            //    });
            //}
        }

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseOutputCache();
        app.UseRouting();
        app.UseStaticFiles();
        app.UseCors();
        app.UseBlazorFrameworkFiles();
        app.UseAntiforgery();
        app.MapRazorPages();
        app.MapControllers();

        app.UseAuthentication();
        app.UseAuthorization();

        app.Use(next => context =>
        {
            var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append("XSRF-TOKEN", tokens?.RequestToken ?? string.Empty, new CookieOptions() { HttpOnly = false });
            return next(context);
        });
        app.MapFallbackToFile("index.html");

        app.MapApi();

        app.Run();
    }
}

