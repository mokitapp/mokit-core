using Bogus;

namespace Mokit.MockEngine.Variables;

public class DynamicVariableProvider
{
    private readonly Faker _faker;
    private readonly Dictionary<string, Func<string>> _builtInVariables;

    public DynamicVariableProvider()
    {
        _faker = new Faker("en");
        _builtInVariables = InitializeBuiltInVariables();
    }

    private Dictionary<string, Func<string>> InitializeBuiltInVariables()
    {
        return new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // Numbers
            ["randomInt"] = () => _faker.Random.Int(0, 10000).ToString(),
            ["randomDecimal"] = () => _faker.Random.Decimal(0, 10000).ToString("F2"),
            ["randomPrice"] = () => _faker.Commerce.Price(),
            ["randomDouble"] = () => _faker.Random.Double(0, 10000).ToString("F4"),
            
            // Strings
            ["randomUUID"] = () => Guid.NewGuid().ToString(),
            ["randomGuid"] = () => Guid.NewGuid().ToString(),
            ["randomHex"] = () => _faker.Random.Hexadecimal(32),
            ["randomString"] = () => _faker.Random.AlphaNumeric(16),
            ["randomWord"] = () => _faker.Lorem.Word(),
            ["randomWords"] = () => string.Join(" ", _faker.Lorem.Words(3)),
            ["randomSentence"] = () => _faker.Lorem.Sentence(),
            ["randomParagraph"] = () => _faker.Lorem.Paragraph(),
            
            // Person
            ["randomName"] = () => _faker.Name.FullName(),
            ["randomFirstName"] = () => _faker.Name.FirstName(),
            ["randomLastName"] = () => _faker.Name.LastName(),
            ["randomEmail"] = () => _faker.Internet.Email(),
            ["randomPhone"] = () => _faker.Phone.PhoneNumber(),
            ["randomUserName"] = () => _faker.Internet.UserName(),
            ["randomJobTitle"] = () => _faker.Name.JobTitle(),
            
            // Address
            ["randomCity"] = () => _faker.Address.City(),
            ["randomCountry"] = () => _faker.Address.Country(),
            ["randomCountryCode"] = () => _faker.Address.CountryCode(),
            ["randomAddress"] = () => _faker.Address.FullAddress(),
            ["randomStreetAddress"] = () => _faker.Address.StreetAddress(),
            ["randomZipCode"] = () => _faker.Address.ZipCode(),
            ["randomLatitude"] = () => _faker.Address.Latitude().ToString("F6"),
            ["randomLongitude"] = () => _faker.Address.Longitude().ToString("F6"),
            
            // Internet
            ["randomUrl"] = () => _faker.Internet.Url(),
            ["randomIP"] = () => _faker.Internet.Ip(),
            ["randomIPv6"] = () => _faker.Internet.Ipv6(),
            ["randomUserAgent"] = () => _faker.Internet.UserAgent(),
            ["randomMac"] = () => _faker.Internet.Mac(),
            ["randomDomainName"] = () => _faker.Internet.DomainName(),
            ["randomProtocol"] = () => _faker.Internet.Protocol(),
            
            // Commerce
            ["randomProduct"] = () => _faker.Commerce.ProductName(),
            ["randomProductCategory"] = () => _faker.Commerce.Categories(1)[0],
            ["randomCompany"] = () => _faker.Company.CompanyName(),
            ["randomCompanySuffix"] = () => _faker.Company.CompanySuffix(),
            ["randomColor"] = () => _faker.Commerce.Color(),
            ["randomDepartment"] = () => _faker.Commerce.Department(),
            
            // Date
            ["now"] = () => DateTime.UtcNow.ToString("O"),
            ["nowUnix"] = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["nowMs"] = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            ["today"] = () => DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["randomDate"] = () => _faker.Date.Past(2).ToString("yyyy-MM-dd"),
            ["randomDateTime"] = () => _faker.Date.Past(2).ToString("O"),
            ["randomPastDate"] = () => _faker.Date.Past(1).ToString("yyyy-MM-dd"),
            ["randomFutureDate"] = () => _faker.Date.Future(1).ToString("yyyy-MM-dd"),
            ["randomMonth"] = () => _faker.Date.Month(),
            ["randomWeekday"] = () => _faker.Date.Weekday(),
            
            // Boolean
            ["randomBoolean"] = () => _faker.Random.Bool().ToString().ToLower(),
            
            // Finance
            ["randomCreditCard"] = () => _faker.Finance.CreditCardNumber(),
            ["randomCurrency"] = () => _faker.Finance.Currency().Code,
            ["randomBitcoinAddress"] = () => _faker.Finance.BitcoinAddress(),
            ["randomIban"] = () => _faker.Finance.Iban(),
            
            // System
            ["randomFileName"] = () => _faker.System.FileName(),
            ["randomFileExtension"] = () => _faker.System.FileExt(),
            ["randomMimeType"] = () => _faker.System.MimeType(),
            
            // Vehicle
            ["randomVehicle"] = () => _faker.Vehicle.Model(),
            ["randomVehicleType"] = () => _faker.Vehicle.Type(),
            ["randomVehicleManufacturer"] = () => _faker.Vehicle.Manufacturer(),
            
            // Images
            ["randomImageUrl"] = () => _faker.Image.PicsumUrl(),
            ["randomAvatarUrl"] = () => _faker.Internet.Avatar(),
        };
    }

    public string? GetVariable(string name)
    {
        if (_builtInVariables.TryGetValue(name, out var generator))
        {
            return generator();
        }
        return null;
    }

    /// <summary>
    /// Get faker value using dot notation like "name.fullName", "internet.email", etc.
    /// </summary>
    public string? GetFakerValue(string expression)
    {
        var parts = expression.ToLower().Split('.');
        if (parts.Length < 2) return null;

        var category = parts[0];
        var method = parts.Length > 1 ? parts[1] : null;

        return category switch
        {
            "name" => GetNameValue(method),
            "internet" => GetInternetValue(method),
            "address" => GetAddressValue(method),
            "commerce" => GetCommerceValue(method),
            "company" => GetCompanyValue(method),
            "date" => GetDateValue(method),
            "random" => GetRandomValue(method),
            "phone" => GetPhoneValue(method),
            "finance" => GetFinanceValue(method),
            "lorem" => GetLoremValue(method),
            "image" => GetImageValue(method),
            "system" => GetSystemValue(method),
            "vehicle" => GetVehicleValue(method),
            _ => null
        };
    }

    private string? GetNameValue(string? method) => method?.ToLower() switch
    {
        "fullname" or "full_name" => _faker.Name.FullName(),
        "firstname" or "first_name" => _faker.Name.FirstName(),
        "lastname" or "last_name" => _faker.Name.LastName(),
        "prefix" => _faker.Name.Prefix(),
        "suffix" => _faker.Name.Suffix(),
        "jobtitle" or "job_title" => _faker.Name.JobTitle(),
        "jobtype" or "job_type" => _faker.Name.JobType(),
        "jobarea" or "job_area" => _faker.Name.JobArea(),
        _ => _faker.Name.FullName()
    };

    private string? GetInternetValue(string? method) => method?.ToLower() switch
    {
        "email" => _faker.Internet.Email(),
        "username" or "user_name" => _faker.Internet.UserName(),
        "password" => _faker.Internet.Password(),
        "url" => _faker.Internet.Url(),
        "ip" => _faker.Internet.Ip(),
        "ipv6" => _faker.Internet.Ipv6(),
        "mac" => _faker.Internet.Mac(),
        "useragent" or "user_agent" => _faker.Internet.UserAgent(),
        "domainname" or "domain_name" => _faker.Internet.DomainName(),
        "protocol" => _faker.Internet.Protocol(),
        "color" => _faker.Internet.Color(),
        "avatar" => _faker.Internet.Avatar(),
        _ => _faker.Internet.Email()
    };

    private string? GetAddressValue(string? method) => method?.ToLower() switch
    {
        "city" => _faker.Address.City(),
        "country" => _faker.Address.Country(),
        "countrycode" or "country_code" => _faker.Address.CountryCode(),
        "streetaddress" or "street_address" => _faker.Address.StreetAddress(),
        "streetname" or "street_name" => _faker.Address.StreetName(),
        "zipcode" or "zip_code" => _faker.Address.ZipCode(),
        "latitude" => _faker.Address.Latitude().ToString("F6"),
        "longitude" => _faker.Address.Longitude().ToString("F6"),
        "state" => _faker.Address.State(),
        "stateabbr" or "state_abbr" => _faker.Address.StateAbbr(),
        "fulladdress" or "full_address" => _faker.Address.FullAddress(),
        _ => _faker.Address.City()
    };

    private string? GetCommerceValue(string? method) => method?.ToLower() switch
    {
        "productname" or "product_name" => _faker.Commerce.ProductName(),
        "price" => _faker.Commerce.Price(),
        "department" => _faker.Commerce.Department(),
        "color" => _faker.Commerce.Color(),
        "productmaterial" or "product_material" => _faker.Commerce.ProductMaterial(),
        "productadjective" or "product_adjective" => _faker.Commerce.ProductAdjective(),
        "product" => _faker.Commerce.Product(),
        "ean8" => _faker.Commerce.Ean8(),
        "ean13" => _faker.Commerce.Ean13(),
        _ => _faker.Commerce.ProductName()
    };

    private string? GetCompanyValue(string? method) => method?.ToLower() switch
    {
        "name" or "companyname" or "company_name" => _faker.Company.CompanyName(),
        "catchphrase" or "catch_phrase" => _faker.Company.CatchPhrase(),
        "bs" => _faker.Company.Bs(),
        "suffix" or "companysuffix" => _faker.Company.CompanySuffix(),
        _ => _faker.Company.CompanyName()
    };

    private string? GetDateValue(string? method) => method?.ToLower() switch
    {
        "past" => _faker.Date.Past(2).ToString("yyyy-MM-dd"),
        "future" => _faker.Date.Future(1).ToString("yyyy-MM-dd"),
        "recent" => _faker.Date.Recent(7).ToString("yyyy-MM-dd"),
        "soon" => _faker.Date.Soon(7).ToString("yyyy-MM-dd"),
        "birthdate" or "birth_date" => _faker.Date.Past(50, DateTime.Now.AddYears(-18)).ToString("yyyy-MM-dd"),
        "month" => _faker.Date.Month(),
        "weekday" => _faker.Date.Weekday(),
        "now" => DateTime.UtcNow.ToString("O"),
        _ => _faker.Date.Recent().ToString("yyyy-MM-dd")
    };

    private string? GetRandomValue(string? method) => method?.ToLower() switch
    {
        "uuid" or "guid" => Guid.NewGuid().ToString(),
        "number" => _faker.Random.Int(0, 10000).ToString(),
        "boolean" or "bool" => _faker.Random.Bool().ToString().ToLower(),
        "word" => _faker.Random.Word(),
        "words" => string.Join(" ", _faker.Random.Words(3)),
        "alphanumeric" or "alpha_numeric" => _faker.Random.AlphaNumeric(16),
        "hexadecimal" or "hex" => _faker.Random.Hexadecimal(16),
        _ => _faker.Random.Int(0, 10000).ToString()
    };

    private string? GetPhoneValue(string? method) => method?.ToLower() switch
    {
        "number" or "phonenumber" or "phone_number" => _faker.Phone.PhoneNumber(),
        _ => _faker.Phone.PhoneNumber()
    };

    private string? GetFinanceValue(string? method) => method?.ToLower() switch
    {
        "account" or "accountnumber" or "account_number" => _faker.Finance.Account(),
        "amount" => _faker.Finance.Amount().ToString("F2"),
        "currencycode" or "currency_code" => _faker.Finance.Currency().Code,
        "currencyname" or "currency_name" => _faker.Finance.Currency().Description,
        "creditcardnumber" or "credit_card_number" => _faker.Finance.CreditCardNumber(),
        "iban" => _faker.Finance.Iban(),
        "bic" => _faker.Finance.Bic(),
        "bitcoinaddress" or "bitcoin_address" => _faker.Finance.BitcoinAddress(),
        _ => _faker.Finance.Amount().ToString("F2")
    };

    private string? GetLoremValue(string? method) => method?.ToLower() switch
    {
        "word" => _faker.Lorem.Word(),
        "words" => string.Join(" ", _faker.Lorem.Words(5)),
        "sentence" => _faker.Lorem.Sentence(),
        "sentences" => string.Join(" ", _faker.Lorem.Sentences(3)),
        "paragraph" => _faker.Lorem.Paragraph(),
        "paragraphs" => string.Join("\n\n", _faker.Lorem.Paragraphs(3)),
        "text" => _faker.Lorem.Text(),
        "slug" => _faker.Lorem.Slug(),
        _ => _faker.Lorem.Sentence()
    };

    private string? GetImageValue(string? method) => method?.ToLower() switch
    {
        "avatar" => _faker.Internet.Avatar(),
        "url" or "imageurl" or "image_url" => _faker.Image.PicsumUrl(),
        "placeholder" => $"https://via.placeholder.com/{_faker.Random.Int(100, 500)}",
        _ => _faker.Image.PicsumUrl()
    };

    private string? GetSystemValue(string? method) => method?.ToLower() switch
    {
        "filename" or "file_name" => _faker.System.FileName(),
        "fileext" or "file_ext" => _faker.System.FileExt(),
        "mimetype" or "mime_type" => _faker.System.MimeType(),
        "directorypath" or "directory_path" => _faker.System.DirectoryPath(),
        "filepath" or "file_path" => _faker.System.FilePath(),
        _ => _faker.System.FileName()
    };

    private string? GetVehicleValue(string? method) => method?.ToLower() switch
    {
        "model" => _faker.Vehicle.Model(),
        "type" => _faker.Vehicle.Type(),
        "manufacturer" => _faker.Vehicle.Manufacturer(),
        "fuel" => _faker.Vehicle.Fuel(),
        "vin" => _faker.Vehicle.Vin(),
        _ => _faker.Vehicle.Model()
    };

    public bool HasVariable(string name)
    {
        return _builtInVariables.ContainsKey(name);
    }

    public IEnumerable<string> GetAvailableVariables()
    {
        return _builtInVariables.Keys.OrderBy(k => k);
    }

    public string GenerateValue(string expression)
    {
        // Handle expressions like randomInt(1, 100) or randomString(32)
        var match = System.Text.RegularExpressions.Regex.Match(expression, @"(\w+)(?:\(([^)]*)\))?");
        if (!match.Success) return expression;

        var funcName = match.Groups[1].Value;
        var args = match.Groups[2].Success ? match.Groups[2].Value : null;

        return funcName.ToLower() switch
        {
            "randomint" when args != null => GenerateRandomInt(args),
            "randomstring" when args != null => GenerateRandomString(args),
            "repeat" when args != null => GenerateRepeat(args),
            _ => GetVariable(funcName) ?? expression
        };
    }

    private string GenerateRandomInt(string args)
    {
        var parts = args.Split(',').Select(p => p.Trim()).ToArray();
        if (parts.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
        {
            return _faker.Random.Int(min, max).ToString();
        }
        return _faker.Random.Int(0, 10000).ToString();
    }

    private string GenerateRandomString(string args)
    {
        if (int.TryParse(args.Trim(), out var length))
        {
            return _faker.Random.AlphaNumeric(length);
        }
        return _faker.Random.AlphaNumeric(16);
    }

    private string GenerateRepeat(string args)
    {
        var parts = args.Split(',').Select(p => p.Trim()).ToArray();
        if (parts.Length == 2 && int.TryParse(parts[1], out var count))
        {
            return string.Concat(Enumerable.Repeat(parts[0].Trim('"', '\''), count));
        }
        return args;
    }
}


