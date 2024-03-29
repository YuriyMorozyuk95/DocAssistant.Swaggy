﻿namespace Shared.Models.Swagger;

public class ApiResponse
{
    public string Request { get; set; }

    public int Code { get; set; }
    public string Message { get; set; }

    public bool IsSuccess { get; set; }
    public string Result { get; set; }
}