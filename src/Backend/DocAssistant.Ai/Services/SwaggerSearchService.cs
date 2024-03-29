﻿using Microsoft.KernelMemory;
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

			var partitions = searchResult.Results.FirstOrDefault()?.Partitions.Take(3).ToList();
            
            if (partitions == null && partitions.Count != 0)
			{
				throw new Exception("Result not found");
			}

            var mergedDocument = SwaggerSplitter.MergeSwagger(partitions);

            return new SwaggerDocument
			{
                Endpoints = mergedDocument.paths,
				SwaggerContent = mergedDocument.document,
				ApiToken = mergedDocument.apiKey
			};
		}	
	}
}
