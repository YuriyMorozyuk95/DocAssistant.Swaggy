﻿using System.Collections;
using DocAssistant.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger
{
    //TODO add test with error
    public class CurlExecutorTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ICurlExecutor _curlExecutor;

        public CurlExecutorTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _curlExecutor = factory.Services.GetRequiredService<ICurlExecutor>();
        }

        [Theory]
        [ClassData(typeof(CurlTestData))]
        public async Task CallCurl(string curl)
        {
            var response = await _curlExecutor.ExecuteCurl(curl);
            _testOutputHelper.WriteLine("response: " + response);
        }
    }

    public class CurlTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/pet/2\" -H \"accept: application/json\"" };
            yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/pet/8\" -H \"accept: application/json\"" };
            yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/store/order/3\"" };
            yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/store/order/6\"" };
            yield return new object[] { "curl -X GET \"https://petstore3.swagger.io/api/v3/store/order/9\"" };
            yield return new object[] { "curl -X PUT \"https://petstore3.swagger.io/api/v3/pet\" -H \"Content-Type: application/json\" -d '{\n  \"id\": 1,\n  \"name\": \"doggie 1\"\n}'" }; //Error
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
