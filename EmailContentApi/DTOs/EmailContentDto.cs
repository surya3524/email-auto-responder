namespace EmailContentApi.DTOs
{
    public class CreateEmailContentDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class EmailContentResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
} 