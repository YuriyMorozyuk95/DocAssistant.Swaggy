using Azure.Storage.Blobs;
using ClientApp.MessageHandler;

using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

builder.Services.AddMsalAuthentication(options =>
{
    var scopes = builder.Configuration["AzureAd:Scopes"];

    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.DefaultAccessTokenScopes.Add(scopes);
});

builder.Services.Configure<AppSettings>(
builder.Configuration.GetSection(nameof(AppSettings)));

builder.Services.AddTransient<ApiClient>();
builder.Services.AddTransient<IUserApiClient, UserApiClient>();
builder.Services.AddTransient<IPermissionApiClient, PermissionApiClient>();

//TODO Remove
//builder.Services.AddSingleton(_ => new BlobServiceClient(builder.Configuration["AzureStorageAccountConnectionString"]));

builder.Services.AddScoped<OpenAIPromptQueue>();
builder.Services.AddTransient<AuthenticatedUserService>();
builder.Services.AddLocalStorageServices();
builder.Services.AddSessionStorageServices();
builder.Services.AddSpeechSynthesisServices();
builder.Services.AddSpeechRecognitionServices();
builder.Services.AddMudServices();
builder.Services.AddFluentUIComponents();  

builder.Services.AddCascadingAuthenticationState();

//TODO add when superusers group will be created
//@attribute [Authorize(Policy="superusers")]
//builder.Services.AddAuthorizationCore(option =>
//{
//    option.AddPolicy("superusers", policy =>
//    {
//       policy.RequireAssertion(context => context.User.HasClaim(x => x.Type == "groups" && x.Value.Contains("superusers-id"))); 
//    });
//});

await JSHost.ImportAsync(
    moduleName: nameof(JavaScriptModule),
    moduleUrl: $"../js/iframe.js?{Guid.NewGuid()}" /* cache bust */);

builder.Services.AddTransient<CustomAuthorizationMessageHandler>();
builder.Services.AddHttpClient("ServerAPI", client =>
{
    var baseUrl = builder.Configuration["AppSettings:BACKEND_URI"];
    client.BaseAddress = new Uri(baseUrl);
});/*.AddHttpMessageHandler<CustomAuthorizationMessageHandler>();*/

var host = builder.Build();
await host.RunAsync();
