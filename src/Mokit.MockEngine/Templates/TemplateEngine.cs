using Scriban;
using Scriban.Runtime;
using Bogus;

namespace Mokit.MockEngine.Templates;

/// <summary>
/// Professional template engine using Scriban.
/// All templating features are built-in - no custom implementations.
/// </summary>
public class TemplateEngine
{
    private readonly Faker _faker;

    public TemplateEngine()
    {
        _faker = new Faker("en");
    }

    public string Render(string template, MockRequestContext context)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        try
        {
            var scribanTemplate = Template.Parse(template);
            
            if (scribanTemplate.HasErrors)
            {
                return template;
            }

            var scriptObject = new ScriptObject();
            
            // Request context
            scriptObject.Add("request", new ScriptObject
            {
                { "path", context.Path },
                { "method", context.Method },
                { "query", context.QueryParams },
                { "querystring", context.QueryParams },
                { "headers", context.Headers },
                { "body", context.Body },
                { "route", context.RouteParams },
                { "params", context.RouteParams },
                { "id", Guid.NewGuid().ToString("N")[..12] }
            });
            
            // Date/Time
            scriptObject.Add("now", DateTime.UtcNow.ToString("O"));
            scriptObject.Add("now_unix", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            scriptObject.Add("today", DateTime.UtcNow.ToString("yyyy-MM-dd"));
            scriptObject.Add("guid", Guid.NewGuid().ToString());
            scriptObject.Add("uuid", Guid.NewGuid().ToString());
            
            // Faker - as a nested script object
            scriptObject.Add("faker", CreateFakerObject());

            var templateContext = new TemplateContext();
            templateContext.PushGlobal(scriptObject);

            return scribanTemplate.Render(templateContext);
        }
        catch
        {
            return template;
        }
    }

    private ScriptObject CreateFakerObject()
    {
        var faker = new ScriptObject();
        
        // Name
        var name = new ScriptObject();
        name.Import("full_name", new Func<string>(() => _faker.Name.FullName()));
        name.Import("first_name", new Func<string>(() => _faker.Name.FirstName()));
        name.Import("last_name", new Func<string>(() => _faker.Name.LastName()));
        name.Import("job_title", new Func<string>(() => _faker.Name.JobTitle()));
        name.Import("prefix", new Func<string>(() => _faker.Name.Prefix()));
        faker.Add("name", name);
        
        // Internet
        var internet = new ScriptObject();
        internet.Import("email", new Func<string>(() => _faker.Internet.Email()));
        internet.Import("user_name", new Func<string>(() => _faker.Internet.UserName()));
        internet.Import("url", new Func<string>(() => _faker.Internet.Url()));
        internet.Import("ip", new Func<string>(() => _faker.Internet.Ip()));
        internet.Import("avatar", new Func<string>(() => _faker.Internet.Avatar()));
        internet.Import("password", new Func<string>(() => _faker.Internet.Password()));
        internet.Import("domain_name", new Func<string>(() => _faker.Internet.DomainName()));
        faker.Add("internet", internet);
        
        // Commerce
        var commerce = new ScriptObject();
        commerce.Import("product_name", new Func<string>(() => _faker.Commerce.ProductName()));
        commerce.Import("price", new Func<string>(() => _faker.Commerce.Price()));
        commerce.Import("department", new Func<string>(() => _faker.Commerce.Department()));
        commerce.Import("color", new Func<string>(() => _faker.Commerce.Color()));
        commerce.Import("product", new Func<string>(() => _faker.Commerce.Product()));
        faker.Add("commerce", commerce);
        
        // Address
        var address = new ScriptObject();
        address.Import("city", new Func<string>(() => _faker.Address.City()));
        address.Import("country", new Func<string>(() => _faker.Address.Country()));
        address.Import("street_address", new Func<string>(() => _faker.Address.StreetAddress()));
        address.Import("zip_code", new Func<string>(() => _faker.Address.ZipCode()));
        address.Import("latitude", new Func<string>(() => _faker.Address.Latitude().ToString("F6")));
        address.Import("longitude", new Func<string>(() => _faker.Address.Longitude().ToString("F6")));
        address.Import("state", new Func<string>(() => _faker.Address.State()));
        address.Import("full_address", new Func<string>(() => _faker.Address.FullAddress()));
        faker.Add("address", address);
        
        // Company
        var company = new ScriptObject();
        company.Import("name", new Func<string>(() => _faker.Company.CompanyName()));
        company.Import("catch_phrase", new Func<string>(() => _faker.Company.CatchPhrase()));
        company.Import("bs", new Func<string>(() => _faker.Company.Bs()));
        faker.Add("company", company);
        
        // Phone
        var phone = new ScriptObject();
        phone.Import("number", new Func<string>(() => _faker.Phone.PhoneNumber()));
        faker.Add("phone", phone);
        
        // Date
        var date = new ScriptObject();
        date.Import("past", new Func<string>(() => _faker.Date.Past(2).ToString("yyyy-MM-dd")));
        date.Import("future", new Func<string>(() => _faker.Date.Future(1).ToString("yyyy-MM-dd")));
        date.Import("recent", new Func<string>(() => _faker.Date.Recent(7).ToString("yyyy-MM-dd")));
        date.Import("birthdate", new Func<string>(() => _faker.Date.Past(50, DateTime.Now.AddYears(-18)).ToString("yyyy-MM-dd")));
        faker.Add("date", date);
        
        // Random
        var random = new ScriptObject();
        random.Import("uuid", new Func<string>(() => Guid.NewGuid().ToString()));
        random.Import("number", new Func<int>(() => _faker.Random.Int(0, 10000)));
        random.Import("boolean", new Func<bool>(() => _faker.Random.Bool()));
        random.Import("word", new Func<string>(() => _faker.Random.Word()));
        faker.Add("random", random);
        
        // Lorem
        var lorem = new ScriptObject();
        lorem.Import("sentence", new Func<string>(() => _faker.Lorem.Sentence()));
        lorem.Import("paragraph", new Func<string>(() => _faker.Lorem.Paragraph()));
        lorem.Import("word", new Func<string>(() => _faker.Lorem.Word()));
        lorem.Import("words", new Func<string>(() => string.Join(" ", _faker.Lorem.Words(5))));
        faker.Add("lorem", lorem);
        
        // Finance
        var finance = new ScriptObject();
        finance.Import("account", new Func<string>(() => _faker.Finance.Account()));
        finance.Import("amount", new Func<string>(() => _faker.Finance.Amount().ToString("F2")));
        finance.Import("currency_code", new Func<string>(() => _faker.Finance.Currency().Code));
        finance.Import("credit_card_number", new Func<string>(() => _faker.Finance.CreditCardNumber()));
        finance.Import("iban", new Func<string>(() => _faker.Finance.Iban()));
        faker.Add("finance", finance);
        
        // Image
        var image = new ScriptObject();
        image.Import("avatar", new Func<string>(() => _faker.Internet.Avatar()));
        image.Import("url", new Func<string>(() => _faker.Image.PicsumUrl()));
        faker.Add("image", image);
        
        return faker;
    }
}

public class MockRequestContext
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, string> QueryParams { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> RouteParams { get; set; } = new();
    public object? Body { get; set; }
    public string? RawBody { get; set; }
}
