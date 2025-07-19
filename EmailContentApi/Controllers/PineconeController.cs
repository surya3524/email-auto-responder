using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EmailContentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PineconeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PineconeController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Creates a new index in Pinecone
        /// </summary>
        /// <param name="request">The index creation request</param>
        /// <returns>Status of the index creation</returns>
        [HttpPost("create-index")]
        public async Task<IActionResult> CreateIndex([FromBody] PineconeIndexRequest request)
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            var pineconeUrl = $"https://controller.{environment}.pinecone.io/databases";
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = JsonSerializer.Serialize(request);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(pineconeUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { message = "Index created successfully", response = responseBody });
            }
            else
            {
                return StatusCode((int)response.StatusCode, new { message = "Failed to create index", response = responseBody });
            }
        }
    }

    //public class PineconeIndexRequest
    //{
    //    public string name { get; set; } = string.Empty;
    //    public int dimension { get; set; }
    //    public string metric { get; set; } = "cosine"; // or "euclidean", "dotproduct"
    //    public int replicas { get; set; } = 1;
    //    public int shards { get; set; } = 1;
    //    public int pods { get; set; } = 1;
    //    public string pod_type { get; set; } = "p1.x1";
    //}
} 