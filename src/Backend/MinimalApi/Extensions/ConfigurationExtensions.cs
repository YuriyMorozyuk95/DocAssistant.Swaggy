namespace MinimalApi.Extensions;

internal static class ConfigurationExtensions
{
    internal static string GetStorageAccountEndpoint(this IConfiguration config)
    {
        var endpoint = config["AzureStorageAccountEndpoint"];
        var folder = config["InputAzureStorageContainer"];
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

        return endpoint;
    }

    internal static string GetStorageAccountInputFolder(this IConfiguration config)
    {
        var folder = config["InputAzureStorageContainer"];
        ArgumentNullException.ThrowIfNullOrEmpty(folder);

        return folder;
    }

    internal static string ToCitationBaseUrl(this IConfiguration config)
    {
        var endpoint = config.GetStorageAccountEndpoint();
        var folder = config.GetStorageAccountInputFolder();

        var builder = new UriBuilder(endpoint)
        {
            Path = folder
        };

        return builder.Uri.AbsoluteUri;
    }
}
