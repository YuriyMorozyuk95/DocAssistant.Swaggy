using DocAssistant.Ai.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace MinimalApi.Tests.Swagger
{
    public class SwaggerSplitterTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private string _swaggerFile;

        public SwaggerSplitterTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            string swaggerFilePath = "Assets/petstore-swagger-full.json";
            _swaggerFile = File.ReadAllText(swaggerFilePath);
        }

        [Fact]
        public void CanSplit()
        {
            var res = new SwaggerSplitter().SplitJson(_swaggerFile);

            foreach (var re in res.ToList())
            {
                PrintResult(re.Item2, string.Empty);
                var combine = Path.Combine(Environment.CurrentDirectory, "files",  re.Item1.Replace('/', '_') + ".json");
                File.WriteAllText(combine, re.Item2);
            }
        }

        private void PrintResult(string content, string metadata)
        {
            _testOutputHelper.WriteLine("result: " + content);

            _testOutputHelper.WriteLine("metadata: " + metadata);
        }
    }
}
