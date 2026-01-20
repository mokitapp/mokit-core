using Microsoft.AspNetCore.Components;

namespace Mokit.Web.Components.Pages;

public partial class Variables
{
    [Inject] public Mokit.Web.Services.IToastService ToastService { get; set; } = default!;

    private string searchTerm = "";
    
    private List<VariableCategory> categories = new()
    {
        new VariableCategory
        {
            Name = "Personal Information",
            Icon = "👤",
            Description = "Name, email and personal information",
            Variables = new List<VariableInfo>
            {
                new("{{faker.name.fullName}}", "Full name", "John Doe"),
                new("{{faker.name.firstName}}", "First name", "John"),
                new("{{faker.name.lastName}}", "Last name", "Doe"),
                new("{{faker.internet.email}}", "Email address", "john@example.com"),
                new("{{faker.internet.userName}}", "Username", "johndoe_42"),
                new("{{faker.phone.number}}", "Phone number", "+1 555-1234"),
                new("{{faker.image.avatar}}", "Avatar URL", "https://..."),
            }
        },
        new VariableCategory
        {
            Name = "Address",
            Icon = "📍",
            Description = "Address and location information",
            Variables = new List<VariableInfo>
            {
                new("{{faker.address.city}}", "City", "New York"),
                new("{{faker.address.country}}", "Country", "United States"),
                new("{{faker.address.streetAddress}}", "Street address", "123 Main St"),
                new("{{faker.address.zipCode}}", "Zip code", "10001"),
                new("{{faker.address.latitude}}", "Latitude", "40.7128"),
                new("{{faker.address.longitude}}", "Longitude", "-74.0060"),
            }
        },
        new VariableCategory
        {
            Name = "Commerce",
            Icon = "🛒",
            Description = "Product and price information",
            Variables = new List<VariableInfo>
            {
                new("{{faker.commerce.productName}}", "Product name", "Ergonomic Keyboard"),
                new("{{faker.commerce.price}}", "Price", "299.99"),
                new("{{faker.commerce.department}}", "Department", "Electronics"),
                new("{{faker.commerce.productAdjective}}", "Product adjective", "Elegant"),
                new("{{faker.commerce.productMaterial}}", "Material", "Wood"),
            }
        },
        new VariableCategory
        {
            Name = "Date & Time",
            Icon = "📅",
            Description = "Date and time values",
            Variables = new List<VariableInfo>
            {
                new("{{now}}", "Current time (ISO)", "2024-12-24T18:30:00Z"),
                new("{{nowUnix}}", "Unix timestamp", "1703442600"),
                new("{{faker.date.recent}}", "Recent date", "2024-12-20"),
                new("{{faker.date.past}}", "Past date", "2023-06-15"),
                new("{{faker.date.future}}", "Future date", "2025-03-20"),
                new("{{faker.date.birthdate}}", "Birth date", "1990-05-12"),
            }
        },
        new VariableCategory
        {
            Name = "Random Values",
            Icon = "🎲",
            Description = "UUID, number and random values",
            Variables = new List<VariableInfo>
            {
                new("{{faker.random.uuid}}", "UUID", "550e8400-e29b-41d4..."),
                new("{{faker.random.number}}", "Random number", "42"),
                new("{{faker.random.number(1,100)}}", "Range number", "73"),
                new("{{faker.random.boolean}}", "Boolean", "true"),
                new("{{faker.random.word}}", "Random word", "lorem"),
                new("{{faker.lorem.sentence}}", "Sentence", "Lorem ipsum..."),
                new("{{faker.lorem.paragraph}}", "Paragraph", "Lorem ipsum dolor..."),
            }
        },
        new VariableCategory
        {
            Name = "Internet",
            Icon = "🌐",
            Description = "URL, IP and internet information",
            Variables = new List<VariableInfo>
            {
                new("{{faker.internet.url}}", "URL", "https://example.com"),
                new("{{faker.internet.ip}}", "IP address", "192.168.1.1"),
                new("{{faker.internet.ipv6}}", "IPv6 address", "2001:0db8:85a3..."),
                new("{{faker.internet.mac}}", "MAC address", "00:1A:2B:3C:4D:5E"),
                new("{{faker.internet.color}}", "Hex color", "#3498db"),
                new("{{faker.internet.domainName}}", "Domain", "example.com"),
            }
        },
        new VariableCategory
        {
            Name = "Request Information",
            Icon = "📨",
            Description = "Incoming request parameters",
            Variables = new List<VariableInfo>
            {
                new("{{request.id}}", "Request ID", "req_abc123"),
                new("{{request.params.id}}", "URL parameter", "/users/:id -> 5"),
                new("{{request.query.page}}", "Query string", "?page=2 -> 2"),
                new("{{request.body.name}}", "Body field", "{name:\"John\"} -> John"),
                new("{{request.headers.authorization}}", "Header value", "Bearer xyz..."),
                new("{{request.method}}", "HTTP method", "GET"),
                new("{{request.path}}", "Request path", "/api/users"),
            }
        },
        new VariableCategory
        {
            Name = "Company",
            Icon = "🏢",
            Description = "Company and business information",
            Variables = new List<VariableInfo>
            {
                new("{{faker.company.name}}", "Company name", "Acme Ltd."),
                new("{{faker.company.catchPhrase}}", "Catchphrase", "Innovative solutions"),
                new("{{faker.company.bs}}", "Business description", "synergize e-markets"),
                new("{{faker.name.jobTitle}}", "Job title", "Software Engineer"),
                new("{{faker.name.jobType}}", "Job type", "Developer"),
            }
        },
        new VariableCategory
        {
            Name = "Finance",
            Icon = "💳",
            Description = "Finance and payment information",
            Variables = new List<VariableInfo>
            {
                new("{{faker.finance.account}}", "Account number", "12345678"),
                new("{{faker.finance.amount}}", "Amount", "1,234.56"),
                new("{{faker.finance.currencyCode}}", "Currency code", "USD"),
                new("{{faker.finance.creditCardNumber}}", "Credit card (fake)", "4111-1111-XXXX"),
                new("{{faker.finance.iban}}", "IBAN", "US00 0000 0000..."),
            }
        }
    };

    private IEnumerable<VariableCategory> GetFilteredCategories()
    {
        if (string.IsNullOrEmpty(searchTerm))
            return categories;
            
        var term = searchTerm.ToLower();
        return categories
            .Select(c => new VariableCategory
            {
                Name = c.Name,
                Icon = c.Icon,
                Description = c.Description,
                Variables = c.Variables
                    .Where(v => v.Syntax.ToLower().Contains(term) || 
                                v.Description.ToLower().Contains(term))
                    .ToList()
            })
            .Where(c => c.Variables.Count > 0);
    }

    private async Task CopyToClipboard(string text)
    {
        // Note: In a real implementation, you'd use JS interop for clipboard
        // Since we don't have JS interop yet, just show the toast
        ToastService.ShowSuccess("Copied to clipboard!");
        await Task.CompletedTask;
    }

    private class VariableCategory
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Description { get; set; } = "";
        public List<VariableInfo> Variables { get; set; } = new();
    }

    private class VariableInfo
    {
        public string Syntax { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }

        public VariableInfo(string syntax, string description, string example)
        {
            Syntax = syntax;
            Description = description;
            Example = example;
        }
    }
}
