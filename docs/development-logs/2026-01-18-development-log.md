# Development Log - 2026-01-18

## Summary
Major improvements to the template engine and Monaco editor UX.

---

## Changes Made

### 🔧 Template Engine Overhaul
- **Removed Handlebars.Net** - Simplified to use only Scriban for templating
- **Professional Scriban Integration** - All templating features now use Scriban's built-in mechanisms
- **Faker Support** - Added comprehensive faker object with categories:
  - `faker.name` (full_name, first_name, last_name, job_title, prefix)
  - `faker.internet` (email, user_name, url, ip, avatar, password, domain_name)
  - `faker.commerce` (product_name, price, department, color, product)
  - `faker.address` (city, country, street_address, zip_code, latitude, longitude, state, full_address)
  - `faker.company` (name, catch_phrase, bs)
  - `faker.phone` (number)
  - `faker.date` (past, future, recent, birthdate)
  - `faker.random` (uuid, number, boolean, word)
  - `faker.lorem` (sentence, paragraph, word, words)
  - `faker.finance` (account, amount, currency_code, credit_card_number, iban)
  - `faker.image` (avatar, url)

### 🎨 Monaco Editor Improvements
- **Fixed slow loading** - Removed unnecessary `Task.Delay(100)` from MonacoEditor.razor
- **Added Monaco to Rules tab** - Custom Error Response Template now uses Monaco editor
- **Added Monaco to Webhooks tab** - Body Template field now uses Monaco editor
- **Consistent UX** - All JSON template fields now have the same professional editing experience

### 📋 Webhook Persistence
- Fixed bug where webhooks were not being saved to database during endpoint creation
- Added proper webhook handling in `CreateWithResponseAsync` and `UpdateWithResponseAsync`

### 📜 Licensing
- Added MIT License file

---

## Files Modified
- `src/Mokit.MockEngine/Templates/TemplateEngine.cs` - Complete rewrite
- `src/Mokit.Web/Components/Shared/MonacoEditor.razor` - Performance fix
- `src/Mokit.Web/Components/Shared/EndpointModal.razor` - Added Monaco editors
- `src/Mokit.Web/Components/Shared/EndpointModal.razor.cs` - Added editor references
- `src/Mokit.Web/Components/Pages/Variables.razor` - Updated examples to Scriban syntax
- `LICENSE` - New file (MIT)

---

## Scriban Syntax Reference
The template engine now uses Scriban syntax exclusively:

```
// Loops
{{ for i in 1..5 }}
  { "id": {{ for.index }}, "name": "{{ faker.name.full_name }}" }
  {{ if !for.last }},{{ end }}
{{ end }}

// Conditionals
{{ if request.query.success }}success{{ else }}error{{ end }}

// Variables
{{ request.query.id }}
{{ request.route.userId }}
{{ now }}
{{ guid }}
```

---

## 🧪 Unit & Integration Tests Added

### Test Summary
- **Unit Tests**: 54 tests (TemplateEngine, Domain Entities)
- **Integration Tests**: 17 tests (Database CRUD operations)
- **Total**: 71 tests, all passing

### Test Files Created
- `tests/Mokit.UnitTests/Templates/TemplateEngineTests.cs` - 25 tests
- `tests/Mokit.UnitTests/Domain/EntityTests.cs` - 29 tests
- `tests/Mokit.IntegrationTests/Database/DatabaseIntegrationTests.cs` - 17 tests
- `tests/Mokit.IntegrationTests/TestDbContextFactory.cs` - Helper for InMemory DB

---

## ⚠️ Test Coverage Status - Known Gap

**Current Line Coverage: 3.68%** (548 lines covered / 14,886 total lines)

This is a known limitation. The current tests focus on core functionality but do not cover the entire codebase.

### What Was Tested

| Component | Tests Written | Status |
|-----------|--------------|--------|
| `Mokit.MockEngine/TemplateEngine` | 25 tests | ✅ Good coverage |
| `Mokit.Domain/Entities` | 29 tests | ✅ Properties tested |
| `Mokit.Infrastructure/MockEndpointService` | 1 test | ⚠️ Partial |
| Database CRUD operations | 17 integration tests | ✅ Good |

### What Needs Testing (Coverage Gaps)

| Component | Priority | Estimated Tests Needed |
|-----------|----------|----------------------|
| `Mokit.Application/Helpers/SlugHelper` | High | 5-10 |
| `Mokit.Infrastructure/Services/*` | High | 30-50 |
| `Mokit.MockEngine/RouteMatcher` | High | 10-15 |
| `Mokit.MockEngine/RequestValidator` | High | 15-20 |
| `Mokit.HostManager/WebhookProcessingService` | Medium | 10-15 |
| `Mokit.Web/Components` | Low | Blazor component tests |

### Recommended Next Steps

1. **Priority 1**: Add tests for `SlugHelper`, `RouteMatcher`, `RequestValidator`
2. **Priority 2**: Add service layer tests for `ProjectService`, `AuthService`, `ImportService`
3. **Priority 3**: Add webhook processing tests
4. **Target**: Aim for 40-60% coverage before production use
