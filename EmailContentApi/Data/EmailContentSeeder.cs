using EmailContentApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailContentApi.Data
{
    public static class EmailContentSeeder
    {
        public static async Task SeedDataAsync(EmailContentDbContext context)
        {
            // Check if data already exists
            if (await context.EmailContents.AnyAsync())
            {
                return; // Data already seeded
            }

            var emailContents = GenerateEmailConversations();
            await context.EmailContents.AddRangeAsync(emailContents);
            await context.SaveChangesAsync();
        }

        private static List<EmailContent> GenerateEmailConversations()
        {
            var conversations = new List<EmailContent>();
            var random = new Random();

            // Sample email conversation templates
            var conversationTemplates = new[]
            {
                // Business meeting scheduling
                @"Subject: Meeting Request - Q4 Planning Discussion

Hi Sarah,

I hope this email finds you well. I wanted to schedule a meeting to discuss our Q4 planning and strategy for the upcoming quarter.

Could we arrange a 30-minute call sometime this week? I'm available on Tuesday and Thursday between 2-4 PM EST.

Please let me know what works best for you.

Best regards,
Michael Johnson
Senior Project Manager",

                // Customer support conversation
                @"Subject: Re: Issue with Order #12345

Dear Customer Support Team,

I'm writing regarding my recent order (#12345) that was delivered on Monday. Unfortunately, one of the items arrived damaged.

The product was the wireless headphones, and the right earpiece is completely non-functional. I've attached photos of the damage for your reference.

I would appreciate if you could help me with a replacement or refund. I'm available to discuss this further at your convenience.

Thank you for your time.

Best regards,
Jennifer Smith
jennifer.smith@email.com",

                // Team collaboration
                @"Subject: Project Update - Marketing Campaign

Hi Team,

I wanted to provide an update on our current marketing campaign progress:

✅ Website redesign - 90% complete
✅ Social media content - Ready for review
⏳ Email campaign - In progress
⏳ Analytics setup - Pending

We're on track to launch by the end of this month. Please review the attached materials and provide feedback by Friday.

Questions or concerns? Let's discuss in our next team meeting.

Thanks,
Alex Chen
Marketing Lead",

                // Sales inquiry
                @"Subject: Product Information Request

Hello Sales Team,

I'm interested in learning more about your enterprise software solution. Our company is looking to upgrade our current system and would like to understand:

1. Pricing structure for 50+ users
2. Implementation timeline
3. Training and support options
4. Integration capabilities with existing tools

Could someone from your team reach out to schedule a demo? I'm available next week Tuesday-Thursday.

Thanks,
Robert Wilson
IT Director
TechCorp Inc.",

                // Internal communication
                @"Subject: Office Policy Update

Dear All,

I wanted to inform everyone about some updates to our office policies effective next month:

1. Flexible working hours (9 AM - 5 PM core hours)
2. Updated dress code for client meetings
3. New parking arrangements
4. Updated expense reporting procedures

Please review the attached document for complete details. If you have any questions, feel free to reach out to HR.

Best regards,
Lisa Thompson
HR Manager",

                // Client feedback
                @"Subject: Thank You - Excellent Service

Dear [Company Name] Team,

I wanted to take a moment to express my gratitude for the outstanding service we received during our recent project.

The team was professional, responsive, and delivered beyond our expectations. The final product exceeded our requirements, and the implementation process was smooth.

We look forward to working with you again on future projects.

Best regards,
David Rodriguez
CEO
Innovation Labs",

                // Technical discussion
                @"Subject: API Integration Questions

Hi Development Team,

I'm working on integrating your API into our system and have a few technical questions:

1. What's the rate limiting for the REST endpoints?
2. Are there any specific authentication requirements?
3. How do we handle error responses?
4. Is there a sandbox environment for testing?

I've reviewed the documentation but would appreciate some clarification on these points.

Thanks,
Maria Garcia
Senior Developer
WebTech Solutions",

                // Event invitation
                @"Subject: Invitation - Annual Tech Conference 2024

Dear Valued Partner,

You're cordially invited to attend our Annual Tech Conference 2024, taking place on March 15-17 in San Francisco.

This year's event will feature:
• Keynote speakers from leading tech companies
• Networking opportunities
• Product demonstrations
• Workshops and training sessions

Early bird registration is available until February 15th. Please RSVP by clicking the link below.

We look forward to seeing you there!

Best regards,
Conference Team
TechEvents Inc.",

                // Newsletter content
                @"Subject: Monthly Newsletter - January 2024

Hello Subscribers,

Welcome to our January 2024 newsletter! Here's what's new this month:

📈 Industry Trends
- AI adoption in small businesses
- Cloud security best practices
- Remote work productivity tips

🎯 Company Updates
- New product launches
- Team member spotlights
- Upcoming events

💡 Tips & Tricks
- How to optimize your workflow
- Time management strategies
- Professional development resources

Stay tuned for more updates next month!

Best regards,
Newsletter Team",

                // Follow-up email
                @"Subject: Follow-up - Our Discussion Last Week

Hi [Name],

I hope you're doing well. I wanted to follow up on our conversation from last week regarding the potential collaboration opportunity.

As discussed, I've prepared a brief proposal outlining how we could work together. I've attached the document for your review.

Please let me know if you have any questions or if you'd like to schedule another call to discuss the details further.

Looking forward to hearing from you.

Best regards,
[Your Name]
[Your Title]"
            };

            // Generate 100 email conversations
            for (int i = 0; i < 100; i++)
            {
                var template = conversationTemplates[random.Next(conversationTemplates.Length)];
                
                // Add some variation to make each email unique
                var variations = new[]
                {
                    "I hope this email finds you well.",
                    "I trust you're having a great day.",
                    "I hope you're doing well.",
                    "Greetings!",
                    "Hello there,"
                };

                var randomVariation = variations[random.Next(variations.Length)];
                var modifiedContent = template.Replace("Hi Sarah,", randomVariation);

                // Add random timestamps within the last 6 months
                var randomDaysAgo = random.Next(1, 180);
                var createdAt = DateTime.UtcNow.AddDays(-randomDaysAgo);

                conversations.Add(new EmailContent
                {
                    Content = modifiedContent,
                    CreatedAt = createdAt
                });
            }

            return conversations;
        }
    }
} 