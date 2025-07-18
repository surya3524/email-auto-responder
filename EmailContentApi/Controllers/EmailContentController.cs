using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmailContentApi.Data;
using EmailContentApi.Models;
using EmailContentApi.DTOs;

namespace EmailContentApi.Controllers
{
    /// <summary>
    /// Controller for managing email content operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EmailContentController : ControllerBase
    {
        private readonly EmailContentDbContext _context;

        public EmailContentController(EmailContentDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all email contents from the database
        /// </summary>
        /// <returns>A list of all email contents</returns>
        /// <response code="200">Returns the list of email contents</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<EmailContentResponseDto>>> GetEmailContents()
        {
            var emailContents = await _context.EmailContents
                .Select(e => new EmailContentResponseDto
                {
                    Id = e.Id,
                    Content = e.Content,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return Ok(emailContents);
        }

        /// <summary>
        /// Retrieves a specific email content by its ID
        /// </summary>
        /// <param name="id">The ID of the email content to retrieve</param>
        /// <returns>The email content with the specified ID</returns>
        /// <response code="200">Returns the requested email content</response>
        /// <response code="404">If the email content with the specified ID was not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmailContentResponseDto>> GetEmailContent(int id)
        {
            var emailContent = await _context.EmailContents.FindAsync(id);

            if (emailContent == null)
            {
                return NotFound();
            }

            var response = new EmailContentResponseDto
            {
                Id = emailContent.Id,
                Content = emailContent.Content,
                CreatedAt = emailContent.CreatedAt
            };

            return Ok(response);
        }

        /// <summary>
        /// Creates a new email content entry
        /// </summary>
        /// <param name="createDto">The email content data to create</param>
        /// <returns>The newly created email content</returns>
        /// <response code="201">Returns the newly created email content</response>
        /// <response code="400">If the content is null or empty</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmailContentResponseDto>> CreateEmailContent(CreateEmailContentDto createDto)
        {
            if (string.IsNullOrWhiteSpace(createDto.Content))
            {
                return BadRequest("Content cannot be empty");
            }

            var emailContent = new EmailContent
            {
                Content = createDto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailContents.Add(emailContent);
            await _context.SaveChangesAsync();

            var response = new EmailContentResponseDto
            {
                Id = emailContent.Id,
                Content = emailContent.Content,
                CreatedAt = emailContent.CreatedAt
            };

            return CreatedAtAction(nameof(GetEmailContent), new { id = emailContent.Id }, response);
        }

        /// <summary>
        /// Updates an existing email content entry
        /// </summary>
        /// <param name="id">The ID of the email content to update</param>
        /// <param name="updateDto">The updated email content data</param>
        /// <returns>No content on successful update</returns>
        /// <response code="204">If the email content was successfully updated</response>
        /// <response code="400">If the content is null or empty</response>
        /// <response code="404">If the email content with the specified ID was not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmailContent(int id, CreateEmailContentDto updateDto)
        {
            var emailContent = await _context.EmailContents.FindAsync(id);

            if (emailContent == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(updateDto.Content))
            {
                return BadRequest("Content cannot be empty");
            }

            emailContent.Content = updateDto.Content;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmailContentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes an email content entry
        /// </summary>
        /// <param name="id">The ID of the email content to delete</param>
        /// <returns>No content on successful deletion</returns>
        /// <response code="204">If the email content was successfully deleted</response>
        /// <response code="404">If the email content with the specified ID was not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEmailContent(int id)
        {
            var emailContent = await _context.EmailContents.FindAsync(id);
            if (emailContent == null)
            {
                return NotFound();
            }

            _context.EmailContents.Remove(emailContent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Manually triggers data seeding (for testing purposes)
        /// </summary>
        /// <returns>Status of the seeding operation</returns>
        /// <response code="200">If seeding was successful</response>
        /// <response code="500">If there was an error during seeding</response>
        [HttpPost("seed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                await EmailContentSeeder.SeedDataAsync(_context);
                return Ok(new { message = "Data seeding completed successfully", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error during data seeding: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets the count of email contents in the database
        /// </summary>
        /// <returns>The total count of email contents</returns>
        /// <response code="200">Returns the count</response>
        [HttpGet("count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> GetEmailContentCount()
        {
            var count = await _context.EmailContents.CountAsync();
            return Ok(new { count, timestamp = DateTime.UtcNow });
        }

        private bool EmailContentExists(int id)
        {
            return _context.EmailContents.Any(e => e.Id == id);
        }
    }
} 