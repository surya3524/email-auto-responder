using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
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
            //var environment = _configuration["Pinecone:Environment"];
            //var projectId = _configuration["Pinecone:ProjectId"];

            //if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            //{
            //    return BadRequest("Pinecone API key or environment is not configured.");
            //}

            //try
            //{
            //    var client = _httpClientFactory.CreateClient("PineconeClient");
            //    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
            //    if (!string.IsNullOrWhiteSpace(projectId))
            //    {
            //        if (!client.DefaultRequestHeaders.Contains("x-project-id"))
            //            client.DefaultRequestHeaders.Add("x-project-id", projectId);
            //    }

            //    var payload = new
            //    {
            //        name = request.Name,
            //        dimension = request.Dimension,
            //        metric = request.Metric,
            //        spec = new
            //        {
            //            serverless = new
            //            {
            //                cloud = request.Spec.Serverless.Cloud,
            //                region = request.Spec.Serverless.Region
            //            }
            //        },
            //        deletion_protection = request.DeletionProtection
            //    };

            //    var json = JsonSerializer.Serialize(payload);
            //    var content = new StringContent(json, Encoding.UTF8, "application/json");

            //    var url = $"https://controller.{environment}.pinecone.io/databases";

            //    Console.WriteLine($"=== Creating Serverless Index ===");
            //    Console.WriteLine($"URL: {url}");
            //    Console.WriteLine($"Payload: {json}");

            //    var response = await client.PostAsync(url, content);
            //    var responseBody = await response.Content.ReadAsStringAsync();

            //    Console.WriteLine($"Response Status: {response.StatusCode}");
            //    Console.WriteLine($"Response Body: {responseBody}");

            //    if (response.IsSuccessStatusCode)
            //    {
            //        return Ok(new { message = "Serverless index created successfully!", details = responseBody });
            //    }
            //    else
            //    {
            //        return StatusCode((int)response.StatusCode, new { message = "Failed to create serverless index", error = responseBody });
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new { message = "Error creating serverless index", error = ex.Message });
            //}


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
        /// Creates a pod-based Pinecone index
        /// </summary>
        /// <param name="request">Pod-based index creation request</param>
        /// <returns>Status of the index creation</returns>
        [HttpPost("create-pod")]
        public async Task<IActionResult> CreatePodIndex([FromBody] PodIndexRequest request)
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
                    metric = request.Metric,
                    spec = new
                    {
                        pod = new
                        {
                            environment = request.Spec.Pod.Environment,
                            pod_type = request.Spec.Pod.PodType,
                            pods = request.Spec.Pod.Pods
                        }
                    },
                    deletion_protection = request.DeletionProtection
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://controller.{environment}.pinecone.io/databases";
                
                Console.WriteLine($"=== Creating Pod Index ===");
                Console.WriteLine($"URL: {url}");
                Console.WriteLine($"Payload: {json}");

                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "Pod index created successfully!", details = responseBody });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to create pod index", error = responseBody });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating pod index", error = ex.Message });
            }
        }

        /// <summary>
        /// Lists all indexes with detailed information
        /// </summary>
        /// <returns>List of all indexes with their specifications</returns>
        [HttpGet("list-detailed")]
        public async Task<IActionResult> ListIndexesDetailed()
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
                    return Ok(new { message = "Indexes retrieved successfully", indexes = responseBody });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to retrieve indexes", error = responseBody });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving indexes", error = ex.Message });
            }
        }

        /// <summary>
        /// Describes a specific index
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
                    return Ok(new { message = "Index details retrieved successfully", index = responseBody });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to retrieve index details", error = responseBody });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving index details", error = ex.Message });
            }
        }
    }

    // Request models that match the Pinecone SDK structure
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
} 