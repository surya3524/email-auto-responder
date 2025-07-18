# Email Content API

A .NET 8 Web API project that manages email content using Entity Framework Core with AWS database integration.

## Features

- CRUD operations for email content
- Auto-incrementing ID field
- Content storage with validation
- AWS database integration
- RESTful API endpoints
- **Automatic data seeding with 100 realistic email conversations**
- Swagger/OpenAPI documentation

## Database Schema

The `EmailContent` table contains:
- `Id` (int, Primary Key, Auto-increment)
- `Content` (nvarchar(10000), Required)
- `CreatedAt` (datetime2, Default: UTC Now)

## Data Seeding

The application automatically seeds the database with approximately 100 realistic email conversations on startup. The seeded data includes:

- Business meeting scheduling emails
- Customer support conversations
- Team collaboration messages
- Sales inquiries
- Internal communications
- Client feedback
- Technical discussions
- Event invitations
- Newsletter content
- Follow-up emails

Each email has:
- Realistic subject lines and content
- Professional formatting
- Varied timestamps (within the last 6 months)
- Different greeting variations

## Prerequisites

- .NET 8 SDK
- AWS RDS SQL Server instance
- Visual Studio 2022 or VS Code

## Setup Instructions

### 1. Database Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=database-1.czkscw4sa0w8.us-east-2.rds.amazonaws.com,1433;Database=EmailContentDb;User Id=admin;Password=testadmin;TrustServerCertificate=true;"
  }
}
```

### 2. Database Migration

Run the following commands to create and apply the database schema:

```bash
# Apply the migration to create the database tables
dotnet ef database update
```

### 3. Run the Application

```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

The API will be available at `https://localhost:7001` (or the configured port).
**Swagger UI will be available at the root URL** (`https://localhost:7001`).

## API Endpoints

### GET /api/EmailContent
Retrieve all email contents.

**Response:**
```json
[
  {
    "id": 1,
    "content": "Subject: Meeting Request - Q4 Planning Discussion\n\nHi Sarah,\n\nI hope this email finds you well...",
    "createdAt": "2024-01-01T12:00:00Z"
  }
]
```

### GET /api/EmailContent/count
Get the total count of email contents in the database.

**Response:**
```json
{
  "count": 100,
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### GET /api/EmailContent/{id}
Retrieve a specific email content by ID.

**Response:**
```json
{
  "id": 1,
  "content": "Subject: Meeting Request - Q4 Planning Discussion\n\nHi Sarah,\n\nI hope this email finds you well...",
  "createdAt": "2024-01-01T12:00:00Z"
}
```

### POST /api/EmailContent
Create a new email content entry.

**Request Body:**
```json
{
  "content": "New email content to store"
}
```

**Response:**
```json
{
  "id": 101,
  "content": "New email content to store",
  "createdAt": "2024-01-01T12:00:00Z"
}
```

### PUT /api/EmailContent/{id}
Update an existing email content entry.

**Request Body:**
```json
{
  "content": "Updated email content"
}
```

### DELETE /api/EmailContent/{id}
Delete an email content entry.

### POST /api/EmailContent/seed
Manually trigger data seeding (useful for testing).

**Response:**
```json
{
  "message": "Data seeding completed successfully",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## Testing the API

### Using Swagger UI
1. Navigate to `https://localhost:7001` in your browser
2. Use the interactive Swagger UI to test all endpoints
3. View detailed API documentation and response schemas

### Using HTTP Client
You can test the API using the provided `EmailContentApi.http` file in Visual Studio Code with the REST Client extension.

### Example curl commands:

```bash
# Get all email contents
curl -X GET "https://localhost:7001/api/EmailContent"

# Get email count
curl -X GET "https://localhost:7001/api/EmailContent/count"

# Create new email content
curl -X POST "https://localhost:7001/api/EmailContent" \
  -H "Content-Type: application/json" \
  -d '{"content": "Test email content"}'

# Get specific email content
curl -X GET "https://localhost:7001/api/EmailContent/1"

# Update email content
curl -X PUT "https://localhost:7001/api/EmailContent/1" \
  -H "Content-Type: application/json" \
  -d '{"content": "Updated content"}'

# Delete email content
curl -X DELETE "https://localhost:7001/api/EmailContent/1"

# Manually trigger seeding
curl -X POST "https://localhost:7001/api/EmailContent/seed"
```

## Project Structure

```
EmailContentApi/
├── Controllers/
│   └── EmailContentController.cs
├── Data/
│   ├── EmailContentDbContext.cs
│   └── EmailContentSeeder.cs
├── DTOs/
│   └── EmailContentDto.cs
├── Models/
│   └── EmailContent.cs
├── Migrations/
│   └── [Migration files]
├── Program.cs
├── appsettings.json
└── README.md
```

## Data Seeding Details

The `EmailContentSeeder` class generates realistic email conversations with:

- **10 different email templates** covering various business scenarios
- **Random variations** in greetings and content
- **Distributed timestamps** over the last 6 months
- **Professional formatting** with proper email structure
- **Realistic subject lines** and content

The seeder runs automatically on application startup and only seeds data if the database is empty.

## Security Considerations

- Store database credentials securely (consider using AWS Secrets Manager)
- Enable HTTPS in production
- Implement proper authentication and authorization
- Validate input data
- Use connection pooling for better performance

## Troubleshooting

1. **Connection Issues**: Ensure your AWS RDS instance is accessible and the security groups allow connections from your application's IP.

2. **Migration Errors**: If you encounter migration issues, you can remove and recreate migrations:
   ```bash
   dotnet ef migrations remove
   dotnet ef migrations add InitialCreate
   ```

3. **SSL Certificate Issues**: The connection string includes `TrustServerCertificate=true` for development. In production, use proper SSL certificates.

4. **Data Seeding Issues**: If seeding fails, you can manually trigger it using the `/api/EmailContent/seed` endpoint.

## License

This project is provided as-is for educational and development purposes. 