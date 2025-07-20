using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using Pinecone;
using EmailContentApi.Data;
using EmailContentApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailContentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PineconeSDKController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EmailContentDbContext _dbContext;

        public PineconeSDKController(IConfiguration configuration, IHttpClientFactory httpClientFactory, EmailContentDbContext dbContext)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
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
                        Fields = new[] { "category", "chunk_text", "email_id", "created_at", "chunk_index", "total_chunks" },
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
                    EmailId = p.AdditionalProperties.ContainsKey("email_id") ? p.AdditionalProperties["email_id"].ToString() ?? "" : "",
                    CreatedAt = p.AdditionalProperties.ContainsKey("created_at") ? p.AdditionalProperties["created_at"].ToString() ?? "" : "",
                    ChunkIndex = p.AdditionalProperties.ContainsKey("chunk_index") ? p.AdditionalProperties["chunk_index"].ToString() ?? "" : "",
                    TotalChunks = p.AdditionalProperties.ContainsKey("total_chunks") ? p.AdditionalProperties["total_chunks"].ToString() ?? "" : "",
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

        /// <summary>
        /// Upserts all email content from the database into the Pinecone index
        /// </summary>
        /// <returns>Status of the upsert operation</returns>
        [HttpPost("upsert-email-data")]
        public async Task<IActionResult> UpsertEmailData()
        {
            try
            {
                var apiKey = _configuration["Pinecone:ApiKey"];
                var indexHost = _configuration["Pinecone:IndexHost"];

                var pinecone = new PineconeClient(apiKey);
                var index = pinecone.Index(host: indexHost);

                // Get all email content from database
                var emailContents = await _dbContext.EmailContents.ToListAsync();
                
                if (!emailContents.Any())
                {
                    return BadRequest(new { error = "No email content found in database" });
                }

                var upsertRecords = new List<UpsertRecord>();
                var chunkCounter = 0;

                foreach (var email in emailContents)
                {
                    // Split email content into chunks (max 1000 characters per chunk)
                    var chunks = SplitIntoChunks(email.Content, 1000);
                    
                    foreach (var chunk in chunks)
                    {
                        chunkCounter++;
                        var recordId = $"email_{email.Id}_chunk_{chunkCounter}";
                        
                        upsertRecords.Add(new UpsertRecord
                        {
                            Id = recordId,
                            AdditionalProperties =
                            {
                                ["chunk_text"] = chunk,
                                ["email_id"] = email.Id.ToString(),
                                ["category"] = "email",
                                ["created_at"] = email.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                                ["chunk_index"] = chunkCounter.ToString(),
                                ["total_chunks"] = chunks.Count.ToString()
                            }
                        });
                    }
                }

                // Upsert records in batches (Pinecone recommends batches of 100)
                const int batchSize = 100;
                var totalUpserted = 0;

                for (int i = 0; i < upsertRecords.Count; i += batchSize)
                {
                    var batch = upsertRecords.Skip(i).Take(batchSize).ToList();
                    
                    await index.UpsertRecordsAsync("example-namespace", batch);
                    totalUpserted += batch.Count;
                    
                    Console.WriteLine($"Upserted batch {i / batchSize + 1}: {batch.Count} records");
                }

                return Ok(new 
                { 
                    message = "Email data upserted successfully", 
                    totalEmails = emailContents.Count,
                    totalChunks = chunkCounter,
                    totalUpserted = totalUpserted
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred during email data upsert", details = ex.Message });
            }
        }

        /// <summary>
        /// Upserts email data with configurable chunk size
        /// </summary>
        /// <param name="request">Upsert request with chunk size configuration</param>
        /// <returns>Status of the upsert operation</returns>
        [HttpPost("upsert-email-data-configurable")]
        public async Task<IActionResult> UpsertEmailDataConfigurable([FromBody] UpsertEmailDataRequest request)
        {
            try
            {
                var apiKey = _configuration["Pinecone:ApiKey"];
                var indexHost = _configuration["Pinecone:IndexHost"];

                var pinecone = new PineconeClient(apiKey);
                var index = pinecone.Index(host: indexHost);

                // Validate chunk size
                if (request.ChunkSize <= 0 || request.ChunkSize > 5000)
                {
                    return BadRequest(new { error = "Chunk size must be between 1 and 5000 characters" });
                }

                // Get all email content from database
                var emailContents = await _dbContext.EmailContents.ToListAsync();
                
                if (!emailContents.Any())
                {
                    return BadRequest(new { error = "No email content found in database" });
                }

                var upsertRecords = new List<UpsertRecord>();
                var chunkCounter = 0;
                var chunkSizeAnalysis = new List<ChunkAnalysis>();

                foreach (var email in emailContents)
                {
                    var chunks = SplitIntoChunks(email.Content, request.ChunkSize);
                    
                    foreach (var chunk in chunks)
                    {
                        chunkCounter++;
                        var recordId = $"email_{email.Id}_chunk_{chunkCounter}";
                        
                        // Analyze chunk characteristics
                        chunkSizeAnalysis.Add(new ChunkAnalysis
                        {
                            EmailId = email.Id,
                            ChunkIndex = chunkCounter,
                            ChunkSize = chunk.Length,
                            WordCount = chunk.Split(' ').Length,
                            SentenceCount = chunk.Split(new[] { '.', '!', '?' }).Length - 1
                        });
                        
                        upsertRecords.Add(new UpsertRecord
                        {
                            Id = recordId,
                            AdditionalProperties =
                            {
                                ["chunk_text"] = chunk,
                                ["email_id"] = email.Id.ToString(),
                                ["category"] = "email",
                                ["created_at"] = email.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                                ["chunk_index"] = chunkCounter.ToString(),
                                ["total_chunks"] = chunks.Count.ToString(),
                                ["chunk_size"] = chunk.Length.ToString()
                            }
                        });
                    }
                }

                // Upsert records in batches
                const int batchSize = 100;
                var totalUpserted = 0;

                for (int i = 0; i < upsertRecords.Count; i += batchSize)
                {
                    var batch = upsertRecords.Skip(i).Take(batchSize).ToList();
                    await index.UpsertRecordsAsync("example-namespace", batch);
                    totalUpserted += batch.Count;
                }

                // Calculate statistics
                var avgChunkSize = chunkSizeAnalysis.Average(c => c.ChunkSize);
                var avgWordCount = chunkSizeAnalysis.Average(c => c.WordCount);
                var avgSentenceCount = chunkSizeAnalysis.Average(c => c.SentenceCount);

                return Ok(new 
                { 
                    message = "Email data upserted successfully", 
                    totalEmails = emailContents.Count,
                    totalChunks = chunkCounter,
                    totalUpserted = totalUpserted,
                    chunkSize = request.ChunkSize,
                    statistics = new
                    {
                        averageChunkSize = Math.Round(avgChunkSize, 2),
                        averageWordCount = Math.Round(avgWordCount, 2),
                        averageSentenceCount = Math.Round(avgSentenceCount, 2),
                        minChunkSize = chunkSizeAnalysis.Min(c => c.ChunkSize),
                        maxChunkSize = chunkSizeAnalysis.Max(c => c.ChunkSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred during email data upsert", details = ex.Message });
            }
        }

        /// <summary>
        /// Splits text into chunks of specified size
        /// </summary>
        /// <param name="text">Text to split</param>
        /// <param name="maxChunkSize">Maximum size of each chunk</param>
        /// <returns>List of text chunks</returns>
        private List<string> SplitIntoChunks(string text, int maxChunkSize)
        {
            var chunks = new List<string>();
            
            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            // If text is shorter than max chunk size, return as single chunk
            if (text.Length <= maxChunkSize)
            {
                chunks.Add(text);
                return chunks;
            }

            // Split by sentences first, then by words if needed
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = "";

            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();
                if (string.IsNullOrWhiteSpace(trimmedSentence))
                    continue;

                // If adding this sentence would exceed the limit
                if (currentChunk.Length + trimmedSentence.Length + 1 > maxChunkSize)
                {
                    // If current chunk is not empty, add it to chunks
                    if (!string.IsNullOrWhiteSpace(currentChunk))
                    {
                        chunks.Add(currentChunk.Trim());
                        currentChunk = "";
                    }

                    // If single sentence is too long, split by words
                    if (trimmedSentence.Length > maxChunkSize)
                    {
                        var words = trimmedSentence.Split(' ');
                        var wordChunk = "";

                        foreach (var word in words)
                        {
                            if (wordChunk.Length + word.Length + 1 > maxChunkSize)
                            {
                                if (!string.IsNullOrWhiteSpace(wordChunk))
                                {
                                    chunks.Add(wordChunk.Trim());
                                    wordChunk = "";
                                }
                            }
                            wordChunk += (wordChunk.Length > 0 ? " " : "") + word;
                        }

                        if (!string.IsNullOrWhiteSpace(wordChunk))
                        {
                            currentChunk = wordChunk;
                        }
                    }
                    else
                    {
                        currentChunk = trimmedSentence;
                    }
                }
                else
                {
                    currentChunk += (currentChunk.Length > 0 ? ". " : "") + trimmedSentence;
                }
            }

            // Add the last chunk if it's not empty
            if (!string.IsNullOrWhiteSpace(currentChunk))
            {
                chunks.Add(currentChunk.Trim());
            }

            return chunks;
        }

        /// <summary>
        /// Demonstrates the impact of different chunk sizes
        /// </summary>
        /// <returns>Analysis of different chunk sizes</returns>
        [HttpGet("chunk-size-analysis")]
        public async Task<IActionResult> ChunkSizeAnalysis()
        {
            try
            {
                // Get a sample email for analysis
                var sampleEmail = await _dbContext.EmailContents.FirstOrDefaultAsync();
                
                if (sampleEmail == null)
                {
                    return BadRequest(new { error = "No email content found in database" });
                }

                var analysis = new List<object>();
                var chunkSizes = new[] { 500, 1000, 1500, 2000, 3000 };

                foreach (var chunkSize in chunkSizes)
                {
                    var chunks = SplitIntoChunks(sampleEmail.Content, chunkSize);
                    
                    analysis.Add(new
                    {
                        chunkSize = chunkSize,
                        totalChunks = chunks.Count,
                        averageChunkSize = chunks.Any() ? Math.Round(chunks.Average(c => c.Length), 2) : 0,
                        minChunkSize = chunks.Any() ? chunks.Min(c => c.Length) : 0,
                        maxChunkSize = chunks.Any() ? chunks.Max(c => c.Length) : 0,
                        sampleChunk = chunks.FirstOrDefault()?.Substring(0, Math.Min(200, chunks.FirstOrDefault()?.Length ?? 0)) + "...",
                        estimatedTokens = chunks.Any() ? chunks.Sum(c => c.Split(' ').Length * 1.3) : 0 // Rough token estimation
                    });
                }

                return Ok(new
                {
                    originalEmailLength = sampleEmail.Content.Length,
                    originalWordCount = sampleEmail.Content.Split(' ').Length,
                    analysis = analysis,
                    recommendations = new
                    {
                        optimalChunkSize = "1000 characters",
                        reasons = new[]
                        {
                            "Balances context preservation with search precision",
                            "Fits well within embedding model token limits",
                            "Provides good LLM prompt efficiency",
                            "Enables fast Pinecone similarity searches"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred during chunk size analysis", details = ex.Message });
            }
        }

        /// <summary>
        /// Demonstrates tokenization and explains token limits
        /// </summary>
        /// <returns>Token analysis and explanation</returns>
        [HttpGet("token-analysis")]
        public async Task<IActionResult> TokenAnalysis()
        {
            try
            {
                // Sample texts to demonstrate tokenization
                var sampleTexts = new[]
                {
                    "Hello world",
                    "Artificial Intelligence and Machine Learning",
                    "I'm going to the store to buy groceries.",
                    "The quick brown fox jumps over the lazy dog.",
                    "Email: john.doe@company.com, Phone: +1-555-123-4567",
                    "Meeting scheduled for 2024-01-15 at 2:30 PM EST.",
                    "Please review the attached document and provide feedback by EOD."
                };

                var tokenAnalysis = new List<object>();

                foreach (var text in sampleTexts)
                {
                    // Rough token estimation (actual tokenization varies by model)
                    var wordCount = text.Split(' ').Length;
                    var estimatedTokens = EstimateTokens(text);
                    
                    tokenAnalysis.Add(new
                    {
                        text = text,
                        characterCount = text.Length,
                        wordCount = wordCount,
                        estimatedTokens = estimatedTokens,
                        tokenToWordRatio = Math.Round((double)estimatedTokens / wordCount, 2),
                        tokenToCharRatio = Math.Round((double)estimatedTokens / text.Length, 3)
                    });
                }

                // Get a sample email for real-world analysis
                var sampleEmail = await _dbContext.EmailContents.FirstOrDefaultAsync();
                var emailTokenAnalysis = new object();

                if (sampleEmail != null)
                {
                    var emailTokens = EstimateTokens(sampleEmail.Content);
                    var emailWords = sampleEmail.Content.Split(' ').Length;
                    
                    emailTokenAnalysis = new
                    {
                        emailLength = sampleEmail.Content.Length,
                        wordCount = emailWords,
                        estimatedTokens = emailTokens,
                        tokenToWordRatio = Math.Round((double)emailTokens / emailWords, 2),
                        tokenToCharRatio = Math.Round((double)emailTokens / sampleEmail.Content.Length, 3)
                    };
                }

                return Ok(new
                {
                    explanation = new
                    {
                        whatAreTokens = "Tokens are the basic units of text that AI models process. They can be words, parts of words, punctuation, or special characters.",
                        tokenizationExamples = new
                        {
                            simpleWords = "Hello world = 2 tokens",
                            compoundWords = "Artificial Intelligence = 2 tokens",
                            punctuation = "Hello, world! = 3 tokens (comma and exclamation are separate)",
                            specialContent = "2024-01-15 = 1 token (date as single unit)",
                            emailAddress = "john.doe@company.com = 1 token"
                        }
                    },
                    tokenLimits = new
                    {
                        embeddingModels = new
                        {
                            textEmbedding3Small = "8192 tokens",
                            textEmbedding3Large = "8192 tokens",
                            ada002 = "8192 tokens"
                        },
                        languageModels = new
                        {
                            gpt35Turbo = "4096 tokens",
                            gpt4 = "8192 tokens",
                            gpt4Turbo = "128000 tokens"
                        },
                        pineconeLimits = new
                        {
                            metadataSize = "40KB per vector",
                            vectorDimensions = "Up to 4096 dimensions"
                        }
                    },
                    sampleAnalysis = tokenAnalysis,
                    emailAnalysis = emailTokenAnalysis,
                    recommendations = new
                    {
                        chunkSize = "1000 characters ≈ 150-200 tokens",
                        reasoning = new[]
                        {
                            "Fits comfortably within embedding model limits (8192 tokens)",
                            "Leaves room for metadata and system overhead",
                            "Allows multiple chunks in LLM context window",
                            "Provides good balance of context and precision"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred during token analysis", details = ex.Message });
            }
        }

        /// <summary>
        /// Estimates token count for text (rough approximation)
        /// </summary>
        /// <param name="text">Text to estimate tokens for</param>
        /// <returns>Estimated token count</returns>
        private int EstimateTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // This is a rough estimation - actual tokenization varies by model
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var baseTokens = words.Length;

            // Add tokens for punctuation and special characters
            var punctuationTokens = text.Count(c => ".,!?;:()[]{}'\"".Contains(c));
            
            // Add tokens for special patterns (emails, dates, numbers, etc.)
            var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
            var datePattern = @"\d{4}-\d{2}-\d{2}";
            var phonePattern = @"\+\d{1,3}-\d{3}-\d{3}-\d{4}";
            
            var emailMatches = System.Text.RegularExpressions.Regex.Matches(text, emailPattern).Count;
            var dateMatches = System.Text.RegularExpressions.Regex.Matches(text, datePattern).Count;
            var phoneMatches = System.Text.RegularExpressions.Regex.Matches(text, phonePattern).Count;

            // Special patterns often count as single tokens
            var specialTokens = emailMatches + dateMatches + phoneMatches;
            
            // Subtract some tokens since special patterns reduce word count
            var adjustedTokens = baseTokens + punctuationTokens + specialTokens - (specialTokens * 0.5);

            return Math.Max(1, (int)Math.Round(adjustedTokens));
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
        public string EmailId { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string ChunkIndex { get; set; } = string.Empty;
        public string TotalChunks { get; set; } = string.Empty;
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

    public class UpsertEmailDataRequest
    {
        public int ChunkSize { get; set; }
    }

    public class ChunkAnalysis
    {
        public int EmailId { get; set; }
        public int ChunkIndex { get; set; }
        public int ChunkSize { get; set; }
        public int WordCount { get; set; }
        public int SentenceCount { get; set; }
    }
} 