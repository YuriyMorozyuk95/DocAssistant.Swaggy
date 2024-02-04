using System.Collections;
using DocAssistant.Ai.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Extensions;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger
{
    //TODO add test with error
    public class CurlExecutorTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly CurlExecutor _curlExecutor;

        public CurlExecutorTest(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _curlExecutor = factory.Services.GetRequiredService<ICurlExecutor>() as CurlExecutor;
        }

        [Fact]
        public void CanCheckApiResponseWhenStatusCode()
        {
            var response = 
                """
                {
                    "code": 400,
                    "message": "error while processing request",
                }
                """;

            var apiResponse = _curlExecutor.CreateApiResponseFromResult(response);
            
            Assert.Equal(400, apiResponse.Code);
            Assert.False(apiResponse.IsSuccess);
            Assert.Equal("error while processing request", apiResponse.Message);
            Assert.Equal(response, apiResponse.Result);
        }

        [Fact]
        public void CanCheckApiResponseWhenNotStatusCode()
        {
            var response = 
            """
            {
                "id": 10,
                "name": "Barsik",
                "photoUrls": [],
                "tags": [],
                "status": "available"
            }
            """;

            var apiResponse = _curlExecutor.CreateApiResponseFromResult(response);
            Assert.Equal(200, apiResponse.Code);
            Assert.True(apiResponse.IsSuccess);
            Assert.Equal(response, apiResponse.Message);
            Assert.Equal(response, apiResponse.Result);
        }

        [Fact]
        public async Task CanPutJsonToFile()
        {
            string curl =
                """
                curl -X PUT "https://petstore3.swagger.io/api/v3/pet" -H "Content-Type: application/json" -d '{
                  "id": 10,
                  "name": "Barsik",
                  "status": "available"
                }'
                """;

            string file = null;
            try
            {
                (curl, file) = await _curlExecutor.PutJsonToFile(curl);

                Assert.True(File.Exists(file));
                Assert.Equal($"curl -X PUT \"https://petstore3.swagger.io/api/v3/pet\" -H \"Content-Type: application/json\" -d @{file}", curl);
                Assert.Equal("{\r\n  \"id\": 10,\r\n  \"name\": \"Barsik\",\r\n  \"status\": \"available\"\r\n}", await File.ReadAllTextAsync(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public async Task CallCurlPut()
        {
            string curl = 
            """
            curl -X PUT "https://petstore3.swagger.io/api/v3/pet" -H "Content-Type: application/json" -d '{
              "id": 10,
              "name": "Barsik",
              "status": "available"
            }'
            """;
  
            var response = await _curlExecutor.ExecuteCurl(curl);
            _testOutputHelper.WriteLine("response: " + response.ToJson());
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
