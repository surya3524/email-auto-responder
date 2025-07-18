using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmailContentApi.Data;
using EmailContentApi.Models;
using EmailContentApi.DTOs;

namespace EmailContentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailContentController : ControllerBase
    {
        private readonly EmailContentDbContext _context;

        public EmailContentController(EmailContentDbContext context)
        {
            _context = context;
        }

        // GET: api/EmailContent
        [HttpGet]
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

        // GET: api/EmailContent/5
        [HttpGet("{id}")]
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

        // POST: api/EmailContent
        [HttpPost]
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

        // PUT: api/EmailContent/5
        [HttpPut("{id}")]
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

        // DELETE: api/EmailContent/5
        [HttpDelete("{id}")]
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

        private bool EmailContentExists(int id)
        {
            return _context.EmailContents.Any(e => e.Id == id);
        }
    }
} 