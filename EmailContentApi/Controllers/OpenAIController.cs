using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace EmailContentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OpenAIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public OpenAIController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Test OpenAI API key connectivity
        /// </summary>
        /// <returns>API key status and test result</returns>
        [HttpGet("test")]
        public async Task<IActionResult> TestOpenAI()
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "text-embedding-3-small";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return BadRequest("OpenAI API key is not configured.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    input = "Hello world",
                    model = model
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                Console.WriteLine($"Testing API Key: {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");

                var response = await client.PostAsync("https://api.openai.com/v1/embeddings", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                return Ok(new
                {
                    message = "OpenAI API test completed",
                    statusCode = response.StatusCode,
                    isSuccess = response.IsSuccessStatusCode,
                    apiKeyPrefix = apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "...",
                    model = model,
                    response = responseBody
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error testing OpenAI API", 
                    error = ex.Message,
                    apiKeyPrefix = apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "..."
                });
            }
        }

        /// <summary>
        /// Generate embeddings for a single text input
        /// </summary>
        /// <param name="request">The embedding request</param>
        /// <returns>The embedding vector</returns>
        [HttpPost("embed")]
        public async Task<IActionResult> GenerateEmbedding([FromBody] EmbeddingRequest request)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "text-embedding-3-small";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return BadRequest("OpenAI API key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Text input is required.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    input = "Surya need discpline",
                    model = model
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                // Debug information
                Console.WriteLine($"API Key (first 10 chars): {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
                Console.WriteLine($"Model: {model}");
                Console.WriteLine($"Request URL: https://api.openai.com/v1/embeddings");
                Console.WriteLine($"Request Payload: {jsonPayload}");

                var response = await client.PostAsync("https://api.openai.com/v1/embeddings", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody);
                    return Ok(new
                    {
                        message = "Embedding generated successfully",
                        model = model,
                        embedding = embeddingResponse?.data?.FirstOrDefault()?.embedding,
                        usage = embeddingResponse?.usage
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { 
                        message = "Failed to generate embedding", 
                        error = responseBody,
                        statusCode = response.StatusCode,
                        apiKeyPrefix = apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "..."
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating embedding", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate embeddings for all email contents in the database
        /// </summary>
        /// <returns>List of email contents with their embeddings</returns>
        [HttpPost("embed-all-emails")]
        public async Task<IActionResult> GenerateEmbeddingsForAllEmails()
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "text-embedding-3-small";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return BadRequest("OpenAI API key is not configured.");
            }

            try
            {
                // Get all email contents from database
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EmailContentApi.Data.EmailContentDbContext>();
                
                var emailContents = await dbContext.EmailContents.Take(10).ToListAsync(); // Limit to 10 for demo
                
                var results = new List<object>();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                foreach (var email in emailContents)
                {
                    try
                    {
                        var payload = new
                        {
                            input = email.Content,
                            model = model
                        };

                        var jsonPayload = JsonSerializer.Serialize(payload);
                        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                        var response = await client.PostAsync("https://api.openai.com/v1/embeddings", content);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody);
                            results.Add(new
                            {
                                emailId = email.Id,
                                content = email.Content.Substring(0, Math.Min(100, email.Content.Length)) + "...",
                                embedding = embeddingResponse?.data?.FirstOrDefault()?.embedding,
                                usage = embeddingResponse?.usage
                            });
                        }
                        else
                        {
                            results.Add(new
                            {
                                emailId = email.Id,
                                error = $"Failed to generate embedding: {responseBody}"
                            });
                        }

                        // Add a small delay to avoid rate limiting
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            emailId = email.Id,
                            error = $"Exception: {ex.Message}"
                        });
                    }
                }

                return Ok(new
                {
                    message = $"Generated embeddings for {results.Count} emails",
                    model = model,
                    results = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating embeddings", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate embeddings for a specific email by ID
        /// </summary>
        /// <param name="id">The email ID</param>
        /// <returns>The email content with its embedding</returns>
        [HttpPost("embed-email/{id}")]
        public async Task<IActionResult> GenerateEmbeddingForEmail(int id)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "text-embedding-3-small";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return BadRequest("OpenAI API key is not configured.");
            }

            try
            {
                // Get the specific email from database
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EmailContentApi.Data.EmailContentDbContext>();
                
                var email = await dbContext.EmailContents.FindAsync(id);
                if (email == null)
                {
                    return NotFound($"Email with ID {id} not found.");
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    input = email.Content,
                    model = model
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.openai.com/v1/embeddings", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody);
                    return Ok(new
                    {
                        message = "Embedding generated successfully",
                        emailId = email.Id,
                        content = email.Content,
                        model = model,
                        embedding = embeddingResponse?.data?.FirstOrDefault()?.embedding,
                        usage = embeddingResponse?.usage
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to generate embedding", error = responseBody });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating embedding", error = ex.Message });
            }
        }
    }

    public class EmbeddingRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class EmbeddingResponse
    {
        public List<EmbeddingData> data { get; set; } = new();
        public Usage usage { get; set; } = new();
    }

    public class EmbeddingData
    {
        public List<float> embedding { get; set; } = new();
        public int index { get; set; }
        public string object_type { get; set; } = string.Empty;
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int total_tokens { get; set; }
    }
} 