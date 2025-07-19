using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace EmailContentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PineconeAuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PineconeAuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Test Pinecone authentication with API Key (Api-Key header)
        /// </summary>
        /// <returns>Authentication test result</returns>
        [HttpGet("test-api-key")]
        public async Task<IActionResult> TestApiKeyAuth()
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("PineconeClient");
                
                // Method 1: API Key authentication
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                
                // Add project ID if available
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var response = await client.GetAsync($"https://controller.{environment}.pinecone.io/databases");
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"=== API Key Authentication Test ===");
                Console.WriteLine($"URL: https://controller.{environment}.pinecone.io/databases");
                Console.WriteLine($"Authorization: Api-Key {apiKey.Substring(0, 10)}...");
                Console.WriteLine($"x-project-id: {projectId}");
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");

                return Ok(new { 
                    method = "API Key Authentication",
                    statusCode = (int)response.StatusCode,
                    success = response.IsSuccessStatusCode,
                    response = responseBody,
                    headers = new {
                        authorization = "Api-Key [REDACTED]",
                        projectId = projectId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    method = "API Key Authentication",
                    error = ex.Message,
                    success = false
                });
            }
        }

        /// <summary>
        /// Test Pinecone authentication with JWT Bearer token
        /// </summary>
        /// <returns>Authentication test result</returns>
        [HttpGet("test-jwt")]
        public async Task<IActionResult> TestJwtAuth()
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("PineconeClient");
                
                // Method 2: JWT Bearer authentication
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                
                // Add project ID if available
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var response = await client.GetAsync($"https://controller.{environment}.pinecone.io/databases");
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"=== JWT Bearer Authentication Test ===");
                Console.WriteLine($"URL: https://controller.{environment}.pinecone.io/databases");
                Console.WriteLine($"Authorization: Bearer {apiKey.Substring(0, 10)}...");
                Console.WriteLine($"x-project-id: {projectId}");
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");

                return Ok(new { 
                    method = "JWT Bearer Authentication",
                    statusCode = (int)response.StatusCode,
                    success = response.IsSuccessStatusCode,
                    response = responseBody,
                    headers = new {
                        authorization = "Bearer [REDACTED]",
                        projectId = projectId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    method = "JWT Bearer Authentication",
                    error = ex.Message,
                    success = false
                });
            }
        }

        /// <summary>
        /// Test Pinecone authentication with API Key in header (no Authorization header)
        /// </summary>
        /// <returns>Authentication test result</returns>
        [HttpGet("test-api-key-header")]
        public async Task<IActionResult> TestApiKeyHeader()
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("PineconeClient");
                
                // Method 3: API Key as custom header
                if (!client.DefaultRequestHeaders.Contains("Api-Key"))
                    client.DefaultRequestHeaders.Add("Api-Key", apiKey);
                
                // Add project ID if available
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var response = await client.GetAsync($"https://controller.{environment}.pinecone.io/databases");
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"=== API Key Header Test ===");
                Console.WriteLine($"URL: https://controller.{environment}.pinecone.io/databases");
                Console.WriteLine($"Api-Key: {apiKey.Substring(0, 10)}...");
                Console.WriteLine($"x-project-id: {projectId}");
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");

                return Ok(new { 
                    method = "API Key Header",
                    statusCode = (int)response.StatusCode,
                    success = response.IsSuccessStatusCode,
                    response = responseBody,
                    headers = new {
                        apiKey = "Api-Key [REDACTED]",
                        projectId = projectId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    method = "API Key Header",
                    error = ex.Message,
                    success = false
                });
            }
        }

        /// <summary>
        /// Get authentication configuration details
        /// </summary>
        /// <returns>Current authentication configuration</returns>
        [HttpGet("config")]
        public IActionResult GetAuthConfig()
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            return Ok(new { 
                hasApiKey = !string.IsNullOrWhiteSpace(apiKey),
                apiKeyPrefix = !string.IsNullOrWhiteSpace(apiKey) ? apiKey.Substring(0, Math.Min(10, apiKey.Length)) : null,
                environment = environment,
                hasProjectId = !string.IsNullOrWhiteSpace(projectId),
                projectId = projectId,
                suggestions = new[] {
                    "If API key starts with 'pcsk_', use Api-Key authentication",
                    "If you have a JWT token, use Bearer authentication",
                    "Project ID is required for JWT authentication",
                    "Check your Pinecone project settings for authentication mode"
                }
            });
        }

        /// <summary>
        /// Test all authentication methods and return results
        /// </summary>
        /// <returns>Results from all authentication tests</returns>
        [HttpGet("test-all")]
        public async Task<IActionResult> TestAllAuthMethods()
        {
            var results = new List<object>();

            // Test API Key authentication
            try
            {
                var apiKeyResult = await TestApiKeyAuth();
                if (apiKeyResult is OkObjectResult okResult)
                {
                    results.Add(okResult.Value);
                }
            }
            catch (Exception ex)
            {
                results.Add(new { method = "API Key", error = ex.Message, success = false });
            }

            // Test JWT authentication
            try
            {
                var jwtResult = await TestJwtAuth();
                if (jwtResult is OkObjectResult okResult)
                {
                    results.Add(okResult.Value);
                }
            }
            catch (Exception ex)
            {
                results.Add(new { method = "JWT", error = ex.Message, success = false });
            }

            // Test API Key header
            try
            {
                var headerResult = await TestApiKeyHeader();
                if (headerResult is OkObjectResult okResult)
                {
                    results.Add(okResult.Value);
                }
            }
            catch (Exception ex)
            {
                results.Add(new { method = "API Key Header", error = ex.Message, success = false });
            }

            return Ok(new { 
                message = "Authentication test results",
                results = results,
                recommendation = "Use the method that returns success = true"
            });
        }
    }
} 