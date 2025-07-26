# Email Content API

A .NET Web API for managing email content with Pinecone vector search and OpenAI integration.

## Features

- Email content management with Entity Framework Core
- Pinecone vector search integration
- OpenAI augmented search with context retrieval
- Semantic search with relevance filtering

## Configuration

### ⚠️ **SECURITY WARNING**
**Never store API keys in source code or configuration files!** 

For enterprise security, use:
- **Azure Key Vault** (recommended for Azure environments)
- **AWS Secrets Manager** (for AWS environments)
- **Environment Variables** (for development)
- **User Secrets** (for local development)

See [SECURITY.md](./SECURITY.md) for detailed enterprise security guidelines.

### Development Configuration

For local development, you can use environment variables or user secrets:

```bash
# Environment Variables
export OpenAI__ApiKey="your-openai-api-key"
export Pinecone__ApiKey="your-pinecone-api-key"

# Or User Secrets
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key"
dotnet user-secrets set "Pinecone:ApiKey" "your-pinecone-api-key"
```

### Production Configuration

The application requires the following configuration in `appsettings.json`:

```json
{
  "KeyVault": {
    "VaultUrl": "https://your-keyvault-name.vault.azure.net/"
  },
  "Pinecone": {
    "Environment": "us-east1-gcp",
    "ProjectId": "your-project-id",
    "IndexHost": "your-index-host"
  },
  "OpenAI": {
    "Model": "text-embedding-3-small"
  }
}
```

API keys are stored securely in Azure Key Vault with the following secret names:
- `OpenAI--ApiKey`
- `Pinecone--ApiKey`
- `ConnectionStrings--DefaultConnection`

## API Endpoints

### Pinecone Operations

#### Create Serverless Index
```
POST /api/PineconeSDK/create-serverless
```

#### Upsert Records
```
POST /api/PineconeSDK/upsert-records
```

#### Upsert Email Data (NEW)
```
POST /api/PineconeSDK/upsert-email-data
```
Upserts all email content from the database into the Pinecone index with intelligent chunking (1000 character chunks).

**Response:**
```json
{
  "message": "Email data upserted successfully",
  "totalEmails": 100,
  "totalChunks": 250,
  "totalUpserted": 250
}
```

#### Upsert Email Data (Configurable Chunk Size)
```
POST /api/PineconeSDK/upsert-email-data-configurable
Content-Type: application/json

{
  "chunkSize": 1000
}
```
Upserts email data with custom chunk size (1-5000 characters) and provides detailed statistics.

**Response:**
```json
{
  "message": "Email data upserted successfully",
  "totalEmails": 100,
  "totalChunks": 250,
  "totalUpserted": 250,
  "chunkSize": 1000,
  "statistics": {
    "averageChunkSize": 856.5,
    "averageWordCount": 142.3,
    "averageSentenceCount": 8.7,
    "minChunkSize": 45,
    "maxChunkSize": 1000
  }
}
```

#### Chunk Size Analysis
```
GET /api/PineconeSDK/chunk-size-analysis
```
Demonstrates the impact of different chunk sizes on text processing and provides recommendations.

#### Token Analysis
```
GET /api/PineconeSDK/token-analysis
```
Explains what tokens are, shows token limits for different models, and demonstrates tokenization with examples.

#### Email Relationship Analysis
```
GET /api/PineconeSDK/email-relationship-analysis
```
Analyzes the email database to identify related emails, potential threads, common topics, and sender/recipient patterns.

**Response:**
```json
{
  "totalEmails": 100,
  "dateRange": {
    "earliest": "2024-01-01T00:00:00Z",
    "latest": "2024-06-30T00:00:00Z",
    "span": 180
  },
  "emailTypes": {
    "categories": [
      { "Meeting/Scheduling": 25 },
      { "Customer Support": 20 },
      { "Project Updates": 15 }
    ],
    "commonSubjects": [
      { "Meeting Request - Q4 Planning Discussion": 10 },
      { "Issue with Order #12345": 8 }
    ]
  },
  "potentialThreads": {
    "totalThreads": 5,
    "averageThreadSize": 3.2,
    "threads": [...]
  },
  "commonTopics": [
    { "meeting": 30 },
    { "project": 25 },
    { "support": 20 }
  ],
  "senderRecipientPatterns": {
    "topSenders": [...],
    "topRecipients": [...],
    "uniqueSenders": 15,
    "uniqueRecipients": 8
  }
}
```

#### Semantic Search
```
POST /api/PineconeSDK/semantic-search
Content-Type: application/json

"your search query"
```

#### Augmented Search (NEW)
```
POST /api/PineconeSDK/augmented-search
Content-Type: application/json

{
  "query": "What are AAPL's plans for Q3 and Q4?",
  "topK": 10,
  "scoreThreshold": 0.35,
  "maxPassages": 5,
  "includePrompt": false
}
```

**Response:**
```json
{
  "answer": "Based on the provided context...",
  "sourcePassages": [
    {
      "id": "rec1",
      "text": "AAPL reported a year-over-year revenue increase...",
      "category": "technology",
      "score": 0.85
    }
  ],
  "searchScore": 0.85,
  "hasRelevantContext": true,
  "promptUsed": null
}
```

#### Test Augmented Search
```
GET /api/PineconeSDK/test-augmented-search
```

### Email Content Operations

#### Get All Email Content
```
GET /api/EmailContent
```

#### Get Email Content by ID
```
GET /api/EmailContent/{id}
```

#### Create Email Content
```
POST /api/EmailContent
```

#### Update Email Content
```
PUT /api/EmailContent/{id}
```

#### Delete Email Content
```
DELETE /api/EmailContent/{id}
```

### Enron Email Operations

#### Insert Enron Emails
```
POST /api/EnronEmail/insert-enron-emails
```
Fetches and inserts 1000 emails from the Enron dataset into the database. If API calls are successful, uses real data as base and generates additional emails to reach 1000 total. Otherwise, generates 1000 realistic Enron-style emails.

**Response:**
```json
{
  "message": "Enron emails inserted successfully",
  "totalInserted": 1000,
  "emailsFromApi": 50,
  "additionalEmailsGenerated": 950,
  "emails": [
    {
      "id": 101,
      "subject": "Q4 Trading Results - Natural Gas",
      "createdAt": "2024-01-15T10:30:00Z",
      "contentPreview": "From: jeff.skilling@enron.com\nTo: ken.lay@enron.com\nSubject: Q4 Trading Results - Natural Gas..."
    }
  ]
}
```

**Features:**
- **API Integration**: Attempts to fetch real Enron emails from public datasets
- **Smart Expansion**: If API emails are available, generates variations to reach 1000 total
- **Fallback Generation**: If no API data, creates 1000 realistic Enron-style emails
- **Batch Processing**: Inserts emails in batches of 100 for optimal performance
- **Progress Logging**: Shows detailed progress during insertion process

#### Insert 1000 Enron Emails (NEW)
```
POST /api/EnronEmail/insert-1000-enron-emails
```
Fetches and inserts exactly 1000 emails from the Enron dataset into the database. If API calls are successful, uses real data as base and generates additional emails to reach 1000 total. Otherwise, generates 1000 realistic Enron-style emails.

**Response:**
```json
{
  "message": "1000 Enron emails inserted successfully",
  "totalInserted": 1000,
  "emailsFromApi": 50,
  "additionalEmailsGenerated": 950,
  "emails": [
    {
      "id": 101,
      "subject": "Q4 Trading Results - Natural Gas",
      "createdAt": "2024-01-15T10:30:00Z",
      "contentPreview": "From: jeff.skilling@enron.com\nTo: ken.lay@enron.com\nSubject: Q4 Trading Results - Natural Gas..."
    }
  ]
}
```

**Features:**
- **Guaranteed 1000 Records**: Always inserts exactly 1000 emails regardless of API success
- **API Integration**: Attempts to fetch real Enron emails from public datasets
- **Smart Expansion**: If API emails are available, generates variations to reach 1000 total
- **Fallback Generation**: If no API data, creates 1000 realistic Enron-style emails
- **Batch Processing**: Inserts emails in batches of 100 for optimal performance
- **Progress Logging**: Shows detailed progress during insertion process

#### Analyze Enron Emails
```
GET /api/EnronEmail/analyze-enron-emails
```
Analyzes Enron emails in the database to identify patterns, relationships, and common topics.

**Response:**
```json
{
  "totalEnronEmails": 10,
  "dateRange": {
    "earliest": "2024-01-01T00:00:00Z",
    "latest": "2024-12-31T00:00:00Z",
    "span": 365
  },
  "emailTypes": {
    "categories": [
      { "Energy Trading": 3 },
      { "Financial Reporting": 2 },
      { "Employee Communication": 2 }
    ]
  },
  "senderRecipientPatterns": {
    "topSenders": [
      { "Jeff Skilling": 3 },
      { "Ken Lay": 2 }
    ],
    "topRecipients": [
      { "ken.lay@enron.com": 3 },
      { "jeff.skilling@enron.com": 2 }
    ]
  },
  "commonTopics": [
    { "trading": 5 },
    { "enron": 10 },
    { "earnings": 3 }
  ],
  "potentialThreads": {
    "totalThreads": 2,
    "averageThreadSize": 3.5,
    "threads": [...]
  }
}
```

#### Get Enron Emails
```
GET /api/EnronEmail/get-enron-emails
```
Retrieves all Enron emails from the database with sender, recipient, and subject information.

**Response:**
```json
{
  "totalEmails": 10,
  "emails": [
    {
      "id": 101,
      "subject": "Q4 Trading Results - Natural Gas",
      "sender": "jeff.skilling@enron.com",
      "recipient": "ken.lay@enron.com",
      "createdAt": "2024-01-15T10:30:00Z",
      "contentPreview": "From: jeff.skilling@enron.com\nTo: ken.lay@enron.com\nSubject: Q4 Trading Results - Natural Gas..."
    }
  ]
}
```

## Augmented Search Features

The augmented search functionality implements a complete RAG (Retrieval-Augmented Generation) pipeline:

1. **Semantic Search**: Retrieves relevant passages from Pinecone vector database
2. **Context Filtering**: Filters results by relevance score and limits the number of passages
3. **Prompt Construction**: Builds a structured prompt with retrieved context
4. **LLM Generation**: Uses OpenAI GPT-3.5-turbo to generate responses based on context
5. **Fallback Handling**: Returns appropriate message when no relevant context is found

## Understanding Tokens and Token Limits

### What are Tokens?

**Tokens** are the basic units of text that AI models process. They're like the "words" that the model understands, but more nuanced:

#### Token Examples:
- **"Hello world"** = 2 tokens
- **"Artificial Intelligence"** = 2 tokens  
- **"I'm going to the store"** = 6 tokens
- **"Hello, world!"** = 3 tokens (comma and exclamation are separate tokens)
- **"john.doe@company.com"** = 1 token (email as single unit)
- **"2024-01-15"** = 1 token (date as single unit)

#### How Tokenization Works:
1. **Words**: Most words become individual tokens
2. **Punctuation**: Commas, periods, exclamation marks are separate tokens
3. **Special Content**: Emails, dates, phone numbers often become single tokens
4. **Subwords**: Long words might be split into multiple tokens

### Why Token Limits Matter

#### 1. **Model Processing Capacity**
- **Memory Constraints**: Models can only process a limited amount of text at once
- **Computational Limits**: More tokens = more processing time and cost
- **Quality Degradation**: Beyond limits, model performance decreases

#### 2. **Cost Implications**
- **API Pricing**: Most AI APIs charge per token
- **Embedding Costs**: Vector embeddings cost per token
- **Storage Costs**: More tokens = more storage in vector databases

#### 3. **Performance Impact**
- **Response Time**: Fewer tokens = faster processing
- **Accuracy**: Optimal token ranges provide best results
- **Reliability**: Staying within limits prevents errors

### Token Limits by Model

| Model Type | Model Name | Token Limit | Use Case |
|------------|------------|-------------|----------|
| **Embedding** | text-embedding-3-small | 8,192 tokens | Vector creation |
| **Embedding** | text-embedding-3-large | 8,192 tokens | High-quality vectors |
| **Language** | GPT-3.5-turbo | 4,096 tokens | Text generation |
| **Language** | GPT-4 | 8,192 tokens | Advanced generation |
| **Language** | GPT-4-turbo | 128,000 tokens | Long context |

## Chunk Size Considerations

### Why 1000 Characters?

The 1000-character chunk size is carefully chosen based on several factors:

#### 1. **Token Budget Management**
- **1000 chars ≈ 150-200 tokens** (rough estimation)
- **Embedding Models**: Well within 8,192 token limit
- **LLM Context**: Allows multiple chunks in 4,096 token window
- **Cost Efficiency**: Optimizes token usage

#### 2. **Embedding Model Limitations**
- **Token Limits**: Most embedding models have 512-8192 token limits
- **Optimal Performance**: Models perform best with chunks of reasonable size
- **Memory Efficiency**: Smaller chunks require less memory for processing

#### 3. **Pinecone Index Constraints**
- **Metadata Size Limits**: Pinecone has limits on metadata field sizes
- **Query Performance**: Smaller chunks enable faster similarity searches
- **Storage Efficiency**: More granular chunks allow for better indexing

#### 4. **LLM Prompt Engineering**
- **Token Budget**: GPT-3.5 has 4096 token context window
- **Prompt Efficiency**: Need space for system instructions, user query, and multiple chunks
- **Response Quality**: Too much context can overwhelm the model

#### 5. **Semantic Search Quality**
- **Precision**: Smaller chunks provide more focused, relevant results
- **Context Clarity**: Each chunk contains a coherent piece of information
- **Reduced Noise**: Avoids mixing unrelated content in single chunks

### Impact of Different Chunk Sizes

| Chunk Size | Estimated Tokens | Pros | Cons |
|------------|------------------|------|------|
| **500 chars** | ~75-100 tokens | Very precise results, fast searches | May lose context, more chunks to manage |
| **1000 chars** | ~150-200 tokens | **Optimal balance** | **Recommended default** |
| **1500 chars** | ~225-300 tokens | More context per chunk | Slightly slower searches |
| **2000+ chars** | ~300+ tokens | Maximum context preservation | Risk of token limits, slower performance |

### Prompt Structure

The system constructs prompts in the following format:

```
System: Use the passages below to answer the query. Respond ONLY using this information. If the information is not sufficient to answer the question, say 'I don't have enough information to answer this question based on the provided context.'

Context:
---
[Passage 1]
AAPL reported a year-over-year revenue increase...
---
[Passage 2]
AAPL may consider healthcare integrations in Q4...
---

User Query: What are AAPL's plans for Q3 and Q4?
```

## Running the Application

1. Ensure you have .NET 9.0 installed
2. Update the configuration in `appsettings.json`
3. Run the application:
   ```bash
   dotnet run
   ```
4. Access the API at `https://localhost:7001` or `http://localhost:5001`
5. View the Swagger documentation at `/swagger`

## Dependencies

- .NET 9.0
- Entity Framework Core
- Pinecone SDK
- OpenAI API
- Swagger/OpenAPI 