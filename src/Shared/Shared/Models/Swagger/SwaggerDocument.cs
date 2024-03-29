﻿namespace Shared.Models.Swagger;

public class SwaggerDocument
{
    public string[] Endpoints { get; set; }
    public string SwaggerContent { get; set; }
    public string ApiToken { get; set; }
    public string SwaggerContentUrl { get; set; }
}