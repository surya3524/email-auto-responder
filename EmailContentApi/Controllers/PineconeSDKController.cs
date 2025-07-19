using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using Pinecone;

namespace EmailContentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PineconeSDKController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PineconeSDKController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Creates a serverless Pinecone index
        /// </summary>
        /// <param name="request">Serverless index creation request</param>
        /// <returns>Status of the index creation</returns>
        [HttpPost("create-serverless")]
        public async Task<IActionResult> CreateServerlessIndex([FromBody] ServerlessIndexRequest request)
        {
            var apiKey = _configuration["Pinecone:ApiKey"];

            var pinecone = new PineconeClient(apiKey);

            var createIndexRequest = await pinecone.CreateIndexForModelAsync(
                new CreateIndexForModelRequest
                {
                    Name = "integrated-dense-dotnet",
                    Cloud = CreateIndexForModelRequestCloud.Aws,
                    Region = "us-east-1",
                    Embed = new CreateIndexForModelRequestEmbed
                    {
                        Model = "llama-text-embed-v2",
                        FieldMap = new Dictionary<string, object?>() { { "text", "chunk_text" } },
                    },
                    DeletionProtection = DeletionProtection.Disabled,
                    Tags = new Dictionary<string, string>
                    {
                        { "environment", "development" }
                    }
                }
            );

            // Check and log the response
            if (createIndexRequest != null)
            {
                Console.WriteLine("=== Create Index Response ===");
                Console.WriteLine($"Status: {createIndexRequest.Status}");
                //Console.WriteLine($"Message: {createIndexRequest.Message}");
                Console.WriteLine($"Details: {JsonSerializer.Serialize(createIndexRequest)}");

                //if (createIndexRequest.Status == "Success")
                //{
                //    Console.WriteLine("Index created successfully.");
                //}
                //else if (createIndexRequest.Status == "Error")
                //{
                //    Console.WriteLine("Error occurred while creating the index.");
                //}
                //else
                //{
                //    Console.WriteLine($"Unexpected status: {createIndexRequest.Status}");
                //}
            }
            else
            {
                Console.WriteLine("CreateIndexForModelAsync returned a null response.");
            }

            return Ok(new { message = "Serverless index creation initiated", details = "Check console for response details." });

        }
        /// <summary>
        /// Adds records to an existing Pinecone index
        /// </summary>
        /// <param name="records">List of records to upsert</param>
        /// <returns>Status of the upsert operation</returns>
        [HttpPost("upsert-records")]
        public async Task<IActionResult> UpsertRecords([FromBody] List<UpsertRecordRequest> records)
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var indexHost = _configuration["Pinecone:IndexHost"];

            var pinecone = new PineconeClient(apiKey);
            var index = pinecone.Index(host: indexHost);

            await index.UpsertRecordsAsync(
    "example-namespace",
    [
        new UpsertRecord
        {
            Id = "rec1",
            AdditionalProperties =
            {
                ["chunk_text"] = "AAPL reported a year-over-year revenue increase, expecting stronger Q3 demand for its flagship phones.",
                ["category"] = "technology",
                ["quarter"] = "Q3",
            },
        },
        new UpsertRecord
        {
            Id = "rec2",
            AdditionalProperties =
            {
                ["chunk_text"] = "AAPL may consider healthcare integrations in Q4 to compete with tech rivals entering the consumer wellness space.",
                ["category"] = "technology",
                ["quarter"] = "Q4",
            },
        },
        new UpsertRecord
        {
            Id = "rec3",
            AdditionalProperties =
            {
                ["chunk_text"] = "AAPL may consider healthcare integrations in Q4 to compete with tech rivals entering the consumer wellness space.",
                ["category"] = "technology",
                ["quarter"] = "Q4",
            },
        },
        new UpsertRecord
        {
            Id = "rec4",
            AdditionalProperties =
            {
                ["chunk_text"] = "AAPL's strategic Q3 partnerships with semiconductor suppliers could mitigate component risks and stabilize iPhone production",
                ["category"] = "technology",
                ["quarter"] = "Q3",
            },
        },
    ]
);
            return Ok(new { message = "Upsert Records succesfully", details = "Check console for response details." });

        }
    
        // Replace the semantic search logic with threshold-based filtering:
        [HttpPost("semantic-search")]
        public async Task<IActionResult> SemanticSearch([FromBody] string queryText)
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var indexHost = _configuration["Pinecone:IndexHost"];

            var pinecone = new PineconeClient(apiKey);
            var index = pinecone.Index(host: indexHost);

            // Request more results than needed, then filter by score
            var response = await index.SearchRecordsAsync(
                "example-namespace",
                new SearchRecordsRequest
                {
                    Query = new SearchRecordsRequestQuery
                    {
                        TopK = 10, // Get more results to allow filtering
                        Inputs = new Dictionary<string, object?> { { "text", queryText } },
                    },
                    Fields = new[] { "category", "chunk_text" },
                }
            );

            var closeMatches = response?.Result.Hits?
                .Where(r => r.Score >= 0.35)
                .ToList();
            return Ok(new { message = "Semantic search completed", results = closeMatches });
        }

        /// <summary>
        /// Performs semantic search and generates an augmented LLM response
        /// </summary>
        /// <param name="request">Query request with optional parameters</param>
        /// <returns>Generated response based on retrieved context</returns>
        [HttpPost("augmented-search")]
        public async Task<IActionResult> AugmentedSearch([FromBody] AugmentedSearchRequest request)
        {
            try
            {
                var apiKey = _configuration["Pinecone:ApiKey"];
                var indexHost = _configuration["Pinecone:IndexHost"];
                var openAiApiKey = _configuration["OpenAI:ApiKey"];

                if (string.IsNullOrEmpty(openAiApiKey))
                {
                    return BadRequest(new { error = "OpenAI API key not configured" });
                }

                var pinecone = new PineconeClient(apiKey);
                var index = pinecone.Index(host: indexHost);

                // Step 1: Perform semantic search
                var searchResponse = await index.SearchRecordsAsync(
                    "example-namespace",
                    new SearchRecordsRequest
                    {
                        Query = new SearchRecordsRequestQuery
                        {
                            TopK = request.TopK ?? 10,
                            Inputs = new Dictionary<string, object?> { { "text", request.Query } },
                        },
                        Fields = new[] { "category", "chunk_text" },
                    }
                );

                // Step 2: Filter and order results by relevance
                var relevantPassages = searchResponse?.Result.Hits?
                    .Where(r => r.Score >= (request.ScoreThreshold ?? 0.35))
                    .OrderByDescending(r => r.Score)
                    .Take(request.MaxPassages ?? 5)
                    .ToList();

                if (relevantPassages == null || !relevantPassages.Any())
                {
                    return Ok(new AugmentedSearchResponse
                    {
                        Answer = "I don't have enough relevant information to answer your question based on the provided context.",
                        SourcePassages = new List<PassageInfo>(),
                        SearchScore = 0.0,
                        HasRelevantContext = false
                    });
                }

                // Step 3: Construct the augmented prompt
                var augmentedPrompt = ConstructAugmentedPrompt(relevantPassages, request.Query);

                // Step 4: Generate LLM response
                var llmResponse = await GenerateLLMResponse(augmentedPrompt, openAiApiKey);

                // Step 5: Prepare response with source passages
                var sourcePassages = relevantPassages.Select(p => new PassageInfo
                {
                    Id = p.Id,

                    Text = p.AdditionalProperties.ContainsKey("chunk_text") ? p.AdditionalProperties["chunk_text"].ToString() ?? "" : "",
                    Category = p.AdditionalProperties.ContainsKey("category") ? p.AdditionalProperties["category"].ToString() ?? "" : "",
                    Score = p.Score
                }).ToList();

                return Ok(new AugmentedSearchResponse
                {
                    Answer = llmResponse,
                    SourcePassages = sourcePassages,
                    SearchScore = relevantPassages.First().Score,
                    HasRelevantContext = true,
                    PromptUsed = request.IncludePrompt ? augmentedPrompt : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred during augmented search", details = ex.Message });
            }
        }

        /// <summary>
        /// Constructs an augmented prompt with retrieved context
        /// </summary>
        private string ConstructAugmentedPrompt(List<Pinecone.Hit> passages, string query)
        {
            var promptBuilder = new StringBuilder();

            // System instruction
            promptBuilder.AppendLine("System: Use the passages below to answer the query. Respond ONLY using this information. If the information is not sufficient to answer the question, say 'I don't have enough information to answer this question based on the provided context.'");
            promptBuilder.AppendLine();

            // Context section
            if (passages != null && passages.Any())
            {
                promptBuilder.AppendLine("Context:");
                for (int i = 0; i < passages.Count; i++)
                {
                    var passage = passages[i];
                    if (passage.Fields != null && passage.Fields.TryGetValue("chunk_text", out var chunkText) && chunkText != null)
                    {
                        var text = chunkText.ToString() ?? "";

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            promptBuilder.AppendLine("---");
                            promptBuilder.AppendLine($"[Passage {i + 1}]");
                            promptBuilder.AppendLine(text);
                            promptBuilder.AppendLine("---");
                            promptBuilder.AppendLine();
                        }
                    }
                }
            }
            else
            {
                promptBuilder.AppendLine("No relevant passages were found.");
            }

            // User query
            promptBuilder.AppendLine($"User Query: {query}");

            return promptBuilder.ToString();
        }

        /// <summary>
        /// Generates LLM response using OpenAI API
        /// </summary>
        private async Task<string> GenerateLLMResponse(string prompt, string apiKey)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 500,
                temperature = 0.3
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"OpenAI Response: {responseContent}");
                
                try
                {
                    // Use JsonDocument for more flexible parsing
                    using var document = JsonDocument.Parse(responseContent);
                    var root = document.RootElement;
                    
                    if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
                    {
                        var firstChoice = choices.EnumerateArray().FirstOrDefault();
                        if (firstChoice.ValueKind != JsonValueKind.Undefined)
                        {
                            if (firstChoice.TryGetProperty("message", out var message) && 
                                message.TryGetProperty("content", out var contentElement))
                            {
                                var responseFromLLM = contentElement.GetString() ?? "No response generated";
                                Console.WriteLine($"Extracted content: {responseFromLLM}");
                                return responseFromLLM;
                            }
                        }
                    }
                    
                    Console.WriteLine("Could not extract content using JsonDocument, trying model deserialization...");
                    
                    // Fallback: try to deserialize with our model
                    var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    var fallbackContent = responseObject?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response generated";
                    Console.WriteLine($"Fallback content: {fallbackContent}");
                    return fallbackContent;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                    Console.WriteLine($"Response content: {responseContent}");
                    return "Error parsing OpenAI response";
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API error: {response.StatusCode} - {errorContent}");
            }
        }

        /// <summary>
        /// Test endpoint to verify augmented search functionality
        /// </summary>
        /// <returns>Test response</returns>
        [HttpGet("test-augmented-search")]
        public async Task<IActionResult> TestAugmentedSearch()
        {
            try
            {
                var testRequest = new AugmentedSearchRequest
                {
                    Query = "What are AAPL's plans for Q3 and Q4?",
                    TopK = 5,
                    ScoreThreshold = 0.3,
                    MaxPassages = 3,
                    IncludePrompt = true
                };

                // Call the augmented search method
                var result = await AugmentedSearch(testRequest);
                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Test failed", details = ex.Message });
            }
        }
    }

    // Request/Response DTOs for augmented search
    public class AugmentedSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int? TopK { get; set; } = 10;
        public double? ScoreThreshold { get; set; } = 0.35;
        public int? MaxPassages { get; set; } = 5;
        public bool IncludePrompt { get; set; } = false;
    }

    public class AugmentedSearchResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<PassageInfo> SourcePassages { get; set; } = new();
        public double SearchScore { get; set; }
        public bool HasRelevantContext { get; set; }
        public string? PromptUsed { get; set; }
    }

    public class PassageInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    // OpenAI API response models
    public class OpenAIResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("object")]
        public string? Object { get; set; }
        
        [JsonPropertyName("created")]
        public long? Created { get; set; }
        
        [JsonPropertyName("model")]
        public string? Model { get; set; }
        
        [JsonPropertyName("choices")]
        public List<OpenAIChoice>? Choices { get; set; }
        
        [JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }
        
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }
        
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class OpenAIUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }
        
        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }
        
        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }
    }

public class ServerlessIndexRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Dimension { get; set; }
        public string Metric { get; set; } = "cosine";
        public ServerlessIndexSpec Spec { get; set; } = new();
        public string DeletionProtection { get; set; } = "disabled";
    }

    public class ServerlessIndexSpec
    {
        public ServerlessSpec Serverless { get; set; } = new();
    }

    public class ServerlessSpec
    {
        public string Cloud { get; set; } = "aws";
        public string Region { get; set; } = "us-east-1";
    }

    public class PodIndexRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Dimension { get; set; }
        public string Metric { get; set; } = "cosine";
        public PodIndexSpec Spec { get; set; } = new();
        public string DeletionProtection { get; set; } = "disabled";
    }

    public class PodIndexSpec
    {
        public PodSpec Pod { get; set; } = new();
    }

    public class PodSpec
    {
        public string Environment { get; set; } = "us-east1-gcp";
        public string PodType { get; set; } = "p1.x1";
        public int Pods { get; set; } = 1;
    }

    public class UpsertRecordRequest
    {
        public string Id { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    }
} 