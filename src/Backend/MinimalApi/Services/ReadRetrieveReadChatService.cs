namespace MinimalApi.Services;

public class ReadRetrieveReadChatService
{
    private readonly SearchClient _searchClient;
    private readonly Kernel _kernel;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReadRetrieveReadChatService> _logger;

    public ReadRetrieveReadChatService(
        SearchClient searchClient,
        OpenAIClient client,
        IConfiguration configuration,
        ILogger<ReadRetrieveReadChatService> logger)
    {
        _searchClient = searchClient;

        // Get the deployed model name from configuration  
        var deployedModelName = configuration["AzureOpenAiChatGptDeployment"];
        // ReSharper disable once AccessToStaticMemberViaDerivedType
        ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);

        // Build the kernel with Azure Chat Completion Service  
        var kernelBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(deployedModelName, client);

        // If embedding model name is provided in configuration, add Text Embedding Generation Service to the kernel  
        var embeddingModelName = configuration["AzureOpenAiEmbeddingDeployment"];
        if (!string.IsNullOrEmpty(embeddingModelName))
        {
            var (azureOpenAiServiceEndpoint, key) = (configuration["AzureOpenAiServiceEndpoint"], configuration["AzureOpenAiServiceEndpointKey"]);
            // ReSharper disable once AccessToStaticMemberViaDerivedType
            ArgumentNullException.ThrowIfNullOrWhiteSpace(azureOpenAiServiceEndpoint);

#pragma warning disable SKEXP0011
            kernelBuilder = kernelBuilder.AddOpenAITextEmbeddingGeneration(embeddingModelName, key);
#pragma warning restore SKEXP0011
        }

        _kernel = kernelBuilder.Build();
        _configuration = configuration;
        _logger = logger;
    }

    // This method generates a reply to a given chat history.  
    public async Task<ApproachResponse> ReplyAsync(
        ChatTurn[] history,
        SearchParameters searchParameters,
        CancellationToken cancellationToken = default)
    {
        var errorBuilder = new StringBuilder();
        try
        {
            // Get chat completion and text embedding generation services from the kernel  
            var chat = _kernel.GetRequiredService<IChatCompletionService>();
#pragma warning disable SKEXP0001
            var embedding = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001

            // If retrieval mode is not "Text" and embedding is not null, generate embeddings for the question 
            string question = GetQuestionFromHistory(history);

            float[] embeddings;
            try
            {
                embeddings = await GenerateEmbeddingsAsync(searchParameters, cancellationToken, embedding, question);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to generate embeddings", e);
            }

            // step 1
            // use llm to get query if retrieval mode is not vector
            // If retrieval mode is not "Vector", generate a search query using the chat completion service
            string query;
            try
            {
                query = await GenerateQueryAsync(searchParameters, cancellationToken, chat, question);
            }
            catch (Exception e)
            {
                errorBuilder.AppendLine($"Failed to generate query {e.Message}");
                _logger.LogError(e, "Failed to generate query");
                query = null;
            }

            // step 2
            // use query to search related docs
            // Use the search query to search related documents

            string documentContents;
            SupportingContent[] documentContentList = { };
            if (embeddings.Count() != 0 || query != null)
            {
                documentContentList = await _searchClient.QueryDocumentsAsync(searchParameters, query, embeddings, cancellationToken);
                documentContents = GetDocumentContents(documentContentList);
            }
            else
            {
                documentContents = string.Empty;
            }


            // step 3
            // put together related docs and conversation history to generate answer
            // Create a new chat to generate the answer
            ChatHistory answerChatHistory = null;
            try
            {
                answerChatHistory = CreateAnswerChat(history, chat, documentContents, errorBuilder);
            }
            catch (Exception e)
            {
                errorBuilder.AppendLine($"Failed to create answer chat {e.Message}");
                _logger.LogError(e, "Failed to create answer chat");
            }

            string answer = null;
            string thoughts = null;
            try
            {
                // get answer
                // Get chat completions to generate the answer  
                (answer, thoughts) = await GetAnswerAsync(cancellationToken, chat, answerChatHistory, errorBuilder, documentContents, history);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to get answer {e.Message}", e);
            }

            string[] questions = { };
            try
            {
                // step 4
                // add follow up questions if requested
                // If follow-up questions are requested, generate them  
                if (searchParameters?.SuggestFollowupQuestions is true)
                {
                    (answer, questions) = await UpdateAnswerWithFollowUpQuestionsAsync(cancellationToken, chat, answer);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to get follow up questions {e.Message}", e);
            }

            // Return the response  
            return new ApproachResponse(
                dataPoints: documentContentList,
                answer: answer,
                thoughts: thoughts,
                citationBaseUrl: _configuration.ToCitationBaseUrl(),
                questions: questions);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get answer");
            return new ApproachResponse(error: e.Message);
        }
    }

    private async Task<(string answer, string[] questions)> UpdateAnswerWithFollowUpQuestionsAsync(CancellationToken cancellationToken, IChatCompletionService chat, string answer)
    {
        var answerWithFollowUpQuestion = new string(answer);

        var systemFollowUp = PromptFileService.ReadPromptsFromFile(PromptFileNames.SystemFollowUp);
        var systemFollowContent = PromptFileService.ReadPromptsFromFile("system-follow-up-content.txt", new Dictionary<string, string>
        {
            { "{answer}", answer }
        });

        var followUpQuestionChat = new ChatHistory(systemFollowUp);
        _logger.LogInformation("system-follow-up: {x}", systemFollowUp);

        followUpQuestionChat.AddUserMessage(systemFollowContent);
        _logger.LogInformation("system-follow-up-content: {x}", systemFollowContent);


        // Get chat completions to generate the follow-up questions  
        var followUpQuestions = await chat.GetChatMessageContentAsync(
            followUpQuestionChat,
            cancellationToken: cancellationToken);

        // Extract the follow-up questions from the result and add them to the answer  
        var followUpQuestionsJson = followUpQuestions.Content;
        _logger.LogInformation("followUpQuestionsJson: {x}", followUpQuestionsJson);

        var followUpQuestionsObject = JsonSerializer.Deserialize<JsonElement>(followUpQuestionsJson);
        var followUpQuestionsList = followUpQuestionsObject.EnumerateArray().Select(x => x.GetString()).ToList();
        foreach (var followUpQuestion in followUpQuestionsList)
        {
            answerWithFollowUpQuestion += $" <<{followUpQuestion}>> ";
        }

        return (answer: answerWithFollowUpQuestion, questions: followUpQuestionsList.ToArray());
    }

    private async Task<(string answer, string thoughts)> GetAnswerAsync(
        CancellationToken cancellationToken,
        IChatCompletionService chat,
        ChatHistory answerChat,
        StringBuilder errorBuilder,
        string documentContents,
        ChatTurn[] chatTurns)
    {
        var answer = await chat.GetChatMessageContentAsync(
            answerChat,
            cancellationToken: cancellationToken);

        // Extract the answer and thoughts from the result  
        var answerJson = answer.Content;
        JsonElement answerObject;
        try
        {
            answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
        }
        catch (Exception e)
        {
            errorBuilder.AppendLine($"Failed to deserialize answer {answerJson}, one more try to update it");
            _logger.LogError(e, "Failed to deserialize answer");

            answerChat = CreateAnswerChat2(chatTurns, chat, documentContents);

            answer = await chat.GetChatMessageContentAsync(
                answerChat,
                cancellationToken: cancellationToken);

            // Extract the answer and thoughts from the result  
            answerJson = answer.Content;

            answerObject = JsonSerializer.Deserialize<JsonElement>(answerJson);
        }

        var ans = answerObject.GetProperty("answer").GetString() ?? throw new InvalidOperationException("Failed to get answer");
        var thoughts = answerObject.GetProperty("thoughts").GetString() ?? throw new InvalidOperationException("Failed to get thoughts");
        return (ans, thoughts);
    }

    private ChatHistory CreateAnswerChat2(ChatTurn[] history, IChatCompletionService chat, string documentContents)
    {
        var createAnswerPrompt = PromptFileService.ReadPromptsFromFile(PromptFileNames.CreateJsonPrompt2);
        _logger.LogInformation("create-answer-2: {x}", createAnswerPrompt);

        var answerChat = new ChatHistory(createAnswerPrompt);

        // add chat history
        foreach (var turn in history)
        {
            answerChat.AddUserMessage(turn.User);
            if (turn.Bot is { } botMessage)
            {
                answerChat.AddAssistantMessage(botMessage);
                _logger.LogInformation("history: {x}", botMessage);
            }
        }

        var createJsonPrompt = PromptFileService.ReadPromptsFromFile(PromptFileNames.CreateJsonPrompt, new Dictionary<string, string>
        {
            { "{documentContents}", documentContents }
        });
        _logger.LogInformation("create-json-prompt: {x}", createJsonPrompt);
        // format prompt
        // Add the document contents and the answer format to the chat  
        answerChat.AddUserMessage(createJsonPrompt);
        return answerChat;
    }

    private ChatHistory CreateAnswerChat(ChatTurn[] history, IChatCompletionService chat, string documentContents, StringBuilder stringBuilder)
    {
        var createAnswerPrompt = PromptFileService.ReadPromptsFromFile(PromptFileNames.CreateAnswer);
        _logger.LogInformation("create-answer: {x}", createAnswerPrompt);

        var answerChat = new ChatHistory(createAnswerPrompt);
        try
        {
            // add chat history
            foreach (var turn in history)
            {
                answerChat.AddUserMessage(turn.User);
                if (turn.Bot is { } botMessage)
                {
                    answerChat.AddAssistantMessage(botMessage);
                    _logger.LogInformation("history: {x}", botMessage);
                }
            }

            var createJsonPrompt = PromptFileService.ReadPromptsFromFile(PromptFileNames.CreateJsonPrompt, new Dictionary<string, string>
            {
                { "{documentContents}", documentContents }
            });
            _logger.LogInformation("create-json-prompt: {x}", createJsonPrompt);
            // format prompt
            // Add the document contents and the answer format to the chat  
            answerChat.AddUserMessage(createJsonPrompt);
            return answerChat;
        }
        catch (Exception e)
        {
            var message = $"Failed to create answer chat {e.Message}";
            stringBuilder.AppendLine(message);
            _logger.LogError(e, message);
            return answerChat;
        }
    }

    private string GetDocumentContents(SupportingContent[] documentContentList)
    {
        string documentContents =
            // Join document contents or set as "no source available" if no documents found  
            documentContentList.Length == 0 ? "no source available." : string.Join("\r", documentContentList.Select(x => $"{x.Title}:{x.Content}"));

        // Print document contents to the console  
        _logger.LogInformation(documentContents);
        return documentContents;
    }
    private async Task<string> GenerateQueryAsync(SearchParameters overrides, CancellationToken cancellationToken, IChatCompletionService chat, string question)
    {
        string query = null;
        if (overrides?.RetrievalMode != "Vector")
        {
            var searchPrompt = PromptFileService.ReadPromptsFromFile(PromptFileNames.SearchPrompt);
            // Create a new chat to generate the search query  

            var getQueryChat = new ChatHistory(searchPrompt);

            // Add the user question to the chat 
            getQueryChat.AddUserMessage(question);
            var result = await chat.GetChatMessageContentAsync(
                getQueryChat,
                cancellationToken: cancellationToken);
            _logger.LogInformation("searchPrompt: {x}", searchPrompt);
            _logger.LogInformation("question: {x}", question);

            // If no result is returned, throw an exception 
            if (result.Content != null)
            {
                throw new InvalidOperationException("Failed to get search query");
            }

            // Extract the search query from the result  
            query = result.Content;
            _logger.LogInformation("Query: {x}", query);
        }

        return query;
    }

#pragma warning disable SKEXP0001
    private async Task<float[]> GenerateEmbeddingsAsync(SearchParameters overrides, CancellationToken cancellationToken, ITextEmbeddingGenerationService embedding, string question)
#pragma warning restore SKEXP0001
    {
        float[] embeddings = null;
        if (overrides?.RetrievalMode != "Text" && embedding is not null)
        {
#pragma warning disable SKEXP0001
            embeddings = (await embedding.GenerateEmbeddingAsync(question, cancellationToken: cancellationToken)).ToArray();
#pragma warning restore SKEXP0001
        }

        return embeddings;
    }

    private string GetQuestionFromHistory(ChatTurn[] history)
    {
        var question = history.LastOrDefault()?.User is { } userQuestion
            ? userQuestion
            : throw new InvalidOperationException("Use question is null");
        return question;
    }
}