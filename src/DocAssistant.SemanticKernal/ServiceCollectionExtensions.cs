using DocAssistant.Ai.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DocAssistant.Ai;

public static class AiServiceCollectionExtensions
{
    public static void AddAiServices(this IServiceCollection services)
    {
        services.AddTransient<ICurlExecutor, CurlExecutor>();
        services.AddTransient<ISwaggerAiAssistantService, SwaggerAiAssistantService>();
    }
}
