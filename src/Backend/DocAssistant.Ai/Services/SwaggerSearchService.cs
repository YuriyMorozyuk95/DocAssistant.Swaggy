using Microsoft.KernelMemory;
using Shared.Models.Swagger;

namespace DocAssistant.Ai.Services
{
    public interface ISwaggerMemorySearchService
	{
		Task<SwaggerDocument> SearchDocument(string userPrompt);
	}

	public class SwaggerMemorySearchService : ISwaggerMemorySearchService
	{
		private readonly MemoryServerless _memory;

		public SwaggerMemorySearchService(MemoryServerless memory)
		{
			_memory = memory;
		}

		public async Task<SwaggerDocument> SearchDocument(string userPrompt)
		{
			var searchResult = await _memory.SearchAsync(userPrompt);

			var firstDoc = searchResult.Results.FirstOrDefault()?.Partitions.FirstOrDefault();

			if (firstDoc == null)
			{
				throw new Exception("Result not foud");
			}

			return new SwaggerDocument
			{
				SwaggerFileName = firstDoc.Tags[TagsKeys.SwaggerFile].FirstOrDefault(),
				Endpoint = firstDoc.Tags[TagsKeys.Endpoint].FirstOrDefault(),
				SwaggerContent = firstDoc.Text,
			};
		}	
	}
}
