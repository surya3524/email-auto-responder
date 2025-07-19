using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace EmailContentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PineconeOfficialController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PineconeOfficialController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Creates a serverless Pinecone index using the official SDK pattern
        /// </summary>
        /// <param name="request">Serverless index creation request</param>
        /// <returns>Status of the index creation</returns>
        [HttpPost("create-serverless")]
        public async Task<IActionResult> CreateServerlessIndex([FromBody] CreateServerlessIndexRequest request)
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
                // TODO: Replace with official SDK once available
                // var pinecone = new PineconeClient(apiKey);
                // var createIndexRequest = await pinecone.CreateIndexAsync(new CreateIndexRequest
                // {
                //     Name = request.Name,
                //     Dimension = request.Dimension,
                //     Metric = MetricType.Cosine,
                //     Spec = new ServerlessIndexSpec
                //     {
                //         Serverless = new ServerlessSpec
                //         {
                //             Cloud = ServerlessSpecCloud.Aws,
                //             Region = request.Region,
                //         }
                //     },
                //     DeletionProtection = DeletionProtection.Disabled
                // });

                // For now, using HTTP client with SDK-style structure
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var payload = new
                {
                    name = request.Name,
                    dimension = request.Dimension,
                    metric = "cosine",
                    spec = new
                    {
                        serverless = new
                        {
                            cloud = "aws",
                            region = request.Region
                        }
                    },
                    deletion_protection = "disabled"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://controller.{environment}.pinecone.io/databases";
                
                Console.WriteLine($"=== Creating Serverless Index (SDK Style) ===");
                Console.WriteLine($"URL: {url}");
                Console.WriteLine($"Payload: {json}");

                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { 
                        message = "Serverless index created successfully using SDK pattern!", 
                        details = responseBody,
                        sdkPattern = "PineconeClient.CreateIndexAsync with ServerlessIndexSpec"
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { 
                        message = "Failed to create serverless index", 
                        error = responseBody,
                        sdkPattern = "PineconeClient.CreateIndexAsync with ServerlessIndexSpec"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error creating serverless index", 
                    error = ex.Message,
                    sdkPattern = "PineconeClient.CreateIndexAsync with ServerlessIndexSpec"
                });
            }
        }

        /// <summary>
        /// Creates a pod-based Pinecone index using the official SDK pattern
        /// </summary>
        /// <param name="request">Pod-based index creation request</param>
        /// <returns>Status of the index creation</returns>
        [HttpPost("create-pod")]
        public async Task<IActionResult> CreatePodIndex([FromBody] CreatePodIndexRequest request)
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
                // TODO: Replace with official SDK once available
                // var pinecone = new PineconeClient(apiKey);
                // var createIndexRequest = await pinecone.CreateIndexAsync(new CreateIndexRequest
                // {
                //     Name = request.Name,
                //     Dimension = request.Dimension,
                //     Metric = MetricType.Cosine,
                //     Spec = new PodIndexSpec
                //     {
                //         Pod = new PodSpec
                //         {
                //             Environment = request.Environment,
                //             PodType = request.PodType,
                //             Pods = request.Pods,
                //         }
                //     },
                //     DeletionProtection = DeletionProtection.Disabled
                // });

                // For now, using HTTP client with SDK-style structure
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var payload = new
                {
                    name = request.Name,
                    dimension = request.Dimension,
                    metric = "cosine",
                    spec = new
                    {
                        pod = new
                        {
                            environment = request.Environment,
                            pod_type = request.PodType,
                            pods = request.Pods
                        }
                    },
                    deletion_protection = "disabled"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://controller.{environment}.pinecone.io/databases";
                
                Console.WriteLine($"=== Creating Pod Index (SDK Style) ===");
                Console.WriteLine($"URL: {url}");
                Console.WriteLine($"Payload: {json}");

                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { 
                        message = "Pod index created successfully using SDK pattern!", 
                        details = responseBody,
                        sdkPattern = "PineconeClient.CreateIndexAsync with PodIndexSpec"
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { 
                        message = "Failed to create pod index", 
                        error = responseBody,
                        sdkPattern = "PineconeClient.CreateIndexAsync with PodIndexSpec"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error creating pod index", 
                    error = ex.Message,
                    sdkPattern = "PineconeClient.CreateIndexAsync with PodIndexSpec"
                });
            }
        }

        /// <summary>
        /// Lists all indexes using the official SDK pattern
        /// </summary>
        /// <returns>List of all indexes</returns>
        [HttpGet("list")]
        public async Task<IActionResult> ListIndexes()
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
                // TODO: Replace with official SDK once available
                // var pinecone = new PineconeClient(apiKey);
                // var indexes = await pinecone.ListIndexesAsync();

                // For now, using HTTP client with SDK-style structure
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var response = await client.GetAsync($"https://controller.{environment}.pinecone.io/databases");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { 
                        message = "Indexes retrieved successfully using SDK pattern", 
                        indexes = responseBody,
                        sdkPattern = "PineconeClient.ListIndexesAsync"
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { 
                        message = "Failed to retrieve indexes", 
                        error = responseBody,
                        sdkPattern = "PineconeClient.ListIndexesAsync"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error retrieving indexes", 
                    error = ex.Message,
                    sdkPattern = "PineconeClient.ListIndexesAsync"
                });
            }
        }

        /// <summary>
        /// Describes a specific index using the official SDK pattern
        /// </summary>
        /// <param name="indexName">Name of the index to describe</param>
        /// <returns>Detailed information about the index</returns>
        [HttpGet("describe/{indexName}")]
        public async Task<IActionResult> DescribeIndex(string indexName)
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
                // TODO: Replace with official SDK once available
                // var pinecone = new PineconeClient(apiKey);
                // var index = await pinecone.DescribeIndexAsync(indexName);

                // For now, using HTTP client with SDK-style structure
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var response = await client.GetAsync($"https://controller.{environment}.pinecone.io/databases/{indexName}");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { 
                        message = "Index details retrieved successfully using SDK pattern", 
                        index = responseBody,
                        sdkPattern = "PineconeClient.DescribeIndexAsync"
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { 
                        message = "Failed to retrieve index details", 
                        error = responseBody,
                        sdkPattern = "PineconeClient.DescribeIndexAsync"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error retrieving index details", 
                    error = ex.Message,
                    sdkPattern = "PineconeClient.DescribeIndexAsync"
                });
            }
        }

        /// <summary>
        /// Deletes an index using the official SDK pattern
        /// </summary>
        /// <param name="indexName">Name of the index to delete</param>
        /// <returns>Status of the deletion</returns>
        [HttpDelete("delete/{indexName}")]
        public async Task<IActionResult> DeleteIndex(string indexName)
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
                // TODO: Replace with official SDK once available
                // var pinecone = new PineconeClient(apiKey);
                // await pinecone.DeleteIndexAsync(indexName);

                // For now, using HTTP client with SDK-style structure
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var response = await client.DeleteAsync($"https://controller.{environment}.pinecone.io/databases/{indexName}");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { 
                        message = $"Index '{indexName}' deleted successfully using SDK pattern", 
                        sdkPattern = "PineconeClient.DeleteIndexAsync"
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { 
                        message = "Failed to delete index", 
                        error = responseBody,
                        sdkPattern = "PineconeClient.DeleteIndexAsync"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error deleting index", 
                    error = ex.Message,
                    sdkPattern = "PineconeClient.DeleteIndexAsync"
                });
            }
        }
    }

    // Request models that match the official Pinecone SDK structure
    public class CreateServerlessIndexRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Dimension { get; set; }
        public string Region { get; set; } = "us-east-1";
    }

    public class CreatePodIndexRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Dimension { get; set; }
        public string Environment { get; set; } = "us-east1-gcp";
        public string PodType { get; set; } = "p1.x1";
        public int Pods { get; set; } = 1;
    }
} 