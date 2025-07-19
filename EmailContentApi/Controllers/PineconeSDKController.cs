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
            return Ok(new { message = "Serverless index creation initiated", details = "Check console for response details." });

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

    public class UpsertRecordRequest
    {
        public string Id { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    }
} 