using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(x =>
{
    x.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BmwCarApi",
        Version = "v1",
        Description =
        """
        The "Web API for BMW Car" is a hypothetical digital platform designed to facilitate remote interaction and control over various functionalities of a BMW Car. It is designed as a RESTful web service and would communicate over HTTPS for security.
        This project aims to provide an interface for users to interact with their BMW cars remotely via the internet. The primary functionality includes retrieving car status, remotely controlling certain aspects of the Car, and interacting with the car's built-in navigation system.
        """

    });

    x.AddServer(new OpenApiServer
    {
        Url = "https://localhost:5001",
        Description = "Local server"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/Car/status", () => "Mock Car Status")
    .WithName("GetCarStatus")
    .WithOpenApi(c => new(c)
    {
        OperationId = "GetCarStatus",
        Tags = new List<OpenApiTag> { new() { Name = "Car Status" } },
        Summary = "Get current status of the Car",
        Description = "Provides status information of the Car like location, speed, fuel level etc."
    });

app.MapPost("/api/Car/start", () => "Mock Car Started")
    .WithName("StartCar")
    .WithOpenApi(c => new(c)
    {
        OperationId = "StartCar",
        Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },
        Summary = "Start the Car remotely",
        Description = "This endpoint starts the Car remotely."
    });

app.MapPost("/api/Car/stop", () => "Mock Car Stopped")
.WithName("StopCar")
.WithOpenApi(c => new(c)
{
    OperationId = "StopCar",
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },
    Summary = "Stop the Car remotely",
    Description = "This endpoint stops the Car remotely."
});

app.MapPost("/api/Car/lock", () => "Mock Car Locked")
.WithName("LockCar")
.WithOpenApi(c => new(c)
{
    OperationId = "LockCar",
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },
    Summary = "Lock the Car remotely",
    Description = "This endpoint locks the Car remotely."
});

app.MapPost("/api/Car/unlock", () => "Mock Car Unlocked")
.WithName("UnlockCar")
.WithOpenApi(c => new(c)
{
    OperationId = "UnlockCar",
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },
    Summary = "Unlock the Car remotely",
    Description = "This endpoint unlocks the Car remotely."
});

app.MapPost("/api/Car/lights", () => "Mock Car Lights Controlled")
.WithName("ControlCarLights")
.WithOpenApi(c => new(c)
{
    OperationId = "ControlCarLights",
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },
    Summary = "Control the Car's lights remotely",
    Description = "This endpoint controls the Car's lights, turning them on or off remotely."
});

app.MapPost("/api/Car/climateControl", () => "Mock Car Climate Controlled")
.WithName("ControlCarClimate")
.WithOpenApi(c => new(c)
{
    OperationId = "ControlCarClimate",
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },
    Summary = "Control the Car's climate remotely",
    Description = "This endpoint controls the Car's climate control system, allowing for remote temperature adjustments."
});

app.MapPost("/api/Car/navigation/destination", () => "Mock Navigation Destination Set")
.WithName("SetNavigationDestination")
.WithOpenApi(c => new(c)
{
    OperationId = "SetNavigationDestination",
    Tags = new List<OpenApiTag> { new() { Name = "Car Navigation" } },
    Summary = "Set a destination for the Car's navigation system",
    Description = "This endpoint sets a destination for the Car's navigation system."
});

app.Run();
