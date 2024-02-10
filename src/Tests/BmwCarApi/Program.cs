using Microsoft.OpenApi.Any;
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
        Url = "https://bmwcarwebapi.azurewebsites.net/",
        Description = "Azure server for control car"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapGet("/api/Car/status", () => "Car status retrieval was successful")  
    .WithName("GetCarStatus")  
    .WithOpenApi(c => new(c)  
    {  
        OperationId = "GetCarStatus",  
        Tags = new List<OpenApiTag> { new() { Name = "Car Status" } },  
        Summary = "Get current status of the Car",  
        Description = "Provides status information of the Car like location, speed, fuel level etc."  
    });  
  
app.MapPost("/api/Car/start", () => "Car start operation was successful")  
    .WithName("StartCar")  
    .WithOpenApi(c => new(c)  
    {  
        OperationId = "StartCar",  
        Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },  
        Summary = "Start the Car remotely",  
        Description = "This endpoint starts the Car remotely."  
    });  
  
app.MapPost("/api/Car/stop", () => "Car stop operation was successful")  
.WithName("StopCar")  
.WithOpenApi(c => new(c)  
{  
    OperationId = "StopCar",  
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },  
    Summary = "Stop the Car remotely",  
    Description = "This endpoint stops the Car remotely."  
});  
  
app.MapPost("/api/Car/lock", () => "Car lock operation was successful")  
.WithName("LockCar")  
.WithOpenApi(c => new(c)  
{  
    OperationId = "LockCar",  
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },  
    Summary = "Lock the Car remotely",  
    Description = "This endpoint locks the Car remotely."  
});  
  
app.MapPost("/api/Car/unlock", () => "Car unlock operation was successful")  
.WithName("UnlockCar")  
.WithOpenApi(c => new(c)  
{  
    OperationId = "UnlockCar",  
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },  
    Summary = "Unlock the Car remotely",  
    Description = "This endpoint unlocks the Car remotely."  
});  
  
app.MapPost("/api/Car/navigation/destination", () => "Navigation destination set operation was successful")  
.WithName("SetNavigationDestination")  
.WithOpenApi(c => new(c)  
{  
    OperationId = "SetNavigationDestination",  
    Tags = new List<OpenApiTag> { new() { Name = "Car Navigation" } },  
    Summary = "Set a destination for the Car's navigation system",  
    Description = "This endpoint sets a destination for the Car's navigation system."  
});  



/////////////////////////

app.MapPost("/api/Car/lights", (string onOrOff) =>  $"Car lights {onOrOff} operation was successful")    
.WithName("TurnCarLights")    
.WithOpenApi(c => new(c)    
{    
    Parameters = new List<OpenApiParameter>
    {
        new()
        {
            Name = "onOrOff",    
            In = ParameterLocation.Query,    
            Required = true,    
            Schema = new OpenApiSchema
            {
                Type = "string",    
                Enum = new List<IOpenApiAny>
                {
                    new OpenApiString("on"),    
                    new OpenApiString("off")    
                }    
            },    
            Description = "The desired state of the Car's lights"    
        }    
    },
    OperationId = "TurnCarLights",    
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },    
    Summary = "Turn the Car's lights on or off",    
    Description = "This endpoint controls the Car's lights, turning them on or off remotely."    
});    
  
app.MapPost("/api/Car/color",  (string redGreenBlue) => $"Car lights color was change to {redGreenBlue} successful")    
.WithName("ChangeCarColor")    
.WithOpenApi(c => new(c)    
{
    Parameters = new List<OpenApiParameter>
    {
        new()
        {
            Name = "redGreenBlue",    
            In = ParameterLocation.Query,    
            Required = true,    
            Schema = new OpenApiSchema
            {
                Type = "string",    
                Description = "The desired color of the Car's lights to red, green or blue"    
            }    
        }    
    },
    OperationId = "ChangeCarColor",    
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },    
    Summary = "Change the Car's color",    
    Description = "This endpoint changes the Car's color remotely."    
});    
  
app.MapPost("/api/Car/condition", () => "Car condition operation was successful")    
.WithName("TurnCarCondition")    
.WithOpenApi(c => new(c)    
{    
    OperationId = "TurnCarCondition",    
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },    
    Summary = "Turn the Car's condition on or off",    
    Description = "This endpoint controls the Car's condition remotely."    
});    
  
app.MapPost("/api/Car/heating", () => "Car heating operation was successful")    
.WithName("TurnCarHeating")    
.WithOpenApi(c => new(c)    
{    
    OperationId = "TurnCarHeating",    
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },    
    Summary = "Turn the Car's heating on or off",    
    Description = "This endpoint controls the Car's heating remotely."    
});    
  
app.MapPost("/api/Car/windows", (string downOrUp) => $"Car windows was put {downOrUp} operation was successful")    
.WithName("PutWindowsDownOrUp")    
.WithOpenApi(c => new(c)    
{
    Parameters = new List<OpenApiParameter>
    {
        new()
        {
            Name = "downOrUp",    
            In = ParameterLocation.Query,    
            Required = true,    
            Schema = new OpenApiSchema
            {
                Type = "string",    
                Enum = new List<IOpenApiAny>
                {
                    new OpenApiString("down"),    
                    new OpenApiString("up")    
                }    
            },    
            Description = "The desired state of the Car's windows"    
        }    
    },
    OperationId = "PutWindowsDown",    
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },    
    Summary = "Put the Car's windows down",    
    Description = "This endpoint puts the Car's windows down remotely."    
});    
  
app.MapPost("/api/Car/radio", () => "Car radio was turned on")    
.WithName("TurnTheRadio")    
.WithOpenApi(c => new(c)    
{    
    OperationId = "TurnTheRadio",    
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },    
    Summary = "Turn the Car's radio on or off",    
    Description = "This endpoint controls the Car's radio remotely."    
});    
  
app.MapPost("/api/Car/volume", (int percentage) => $"Music volume set {percentage} operation was successful")    
.WithName("SetMusicVolume")    
.WithOpenApi(c => new(c)    
{
    Parameters = new List<OpenApiParameter>
    {
        new()
        {
            Name = "percentage",    
            In = ParameterLocation.Query,    
            Required = true,    
            Schema = new OpenApiSchema
            {
                Type = "integer",    
                Minimum = 0,    
                Maximum = 100,    
                Description = "The desired volume of the Car's music"    
            }    
        }    
    },
    OperationId = "SetMusicVolume",    
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },    
    Summary = "Set the Car's music volume",    
    Description = "This endpoint sets the Car's music volume remotely."    
});    
  
app.MapPost("/api/Car/emergency", () => "Hazard warning lights were turned on successfuly")    
.WithName("TurnEmergencyCar")    
.WithOpenApi(c => new(c)    
{    
    OperationId = "TurnEmergencyCar",    
    Tags = new List<OpenApiTag> { new() { Name = "Car Control" } },    
    Summary = "Turn the Car's emergency on or off",    
    Description = "This endpoint controls the Car's emergency remotely."    
});    



app.Run();
