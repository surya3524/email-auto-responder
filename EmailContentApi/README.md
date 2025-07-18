# Email Content API

A .NET 8 Web API project that manages email content using Entity Framework Core with AWS database integration.

## Features

- CRUD operations for email content
- Auto-incrementing ID field
- Content storage with validation
- AWS database integration
- RESTful API endpoints

## Database Schema

The `EmailContent` table contains:
- `Id` (int, Primary Key, Auto-increment)
- `Content` (nvarchar(10000), Required)
- `CreatedAt` (datetime2, Default: UTC Now)

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
    "DefaultConnection": "Server=your-aws-rds-endpoint.amazonaws.com,1433;Database=EmailContentDb;User Id=your-username;Password=your-password;TrustServerCertificate=true;"
  }
}
```

Replace the following placeholders:
- `your-aws-rds-endpoint.amazonaws.com` - Your AWS RDS endpoint
- `your-username` - Database username
- `your-password` - Database password

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

## API Endpoints

### GET /api/EmailContent
Retrieve all email contents.

**Response:**
```json
[
  {
    "id": 1,
    "content": "Sample email content",
    "createdAt": "2024-01-01T12:00:00Z"
  }
]
```

### GET /api/EmailContent/{id}
Retrieve a specific email content by ID.

**Response:**
```json
{
  "id": 1,
  "content": "Sample email content",
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
  "id": 2,
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

## Testing the API

You can test the API using the provided `EmailContentApi.http` file in Visual Studio Code with the REST Client extension, or use tools like Postman or curl.

### Example curl commands:

```bash
# Get all email contents
curl -X GET "https://localhost:7001/api/EmailContent"

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
```

## Project Structure

```
EmailContentApi/
├── Controllers/
│   └── EmailContentController.cs
├── Data/
│   └── EmailContentDbContext.cs
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

## License

This project is provided as-is for educational and development purposes. 