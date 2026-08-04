# eBay Playwright Automation — C#

End-to-end automation for eBay using **Microsoft Playwright** + **NUnit** + **Allure Reports**, written in C# (.NET 8).

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| Node.js (for Playwright browsers) | 18+ |
| Allure CLI (optional, for HTML report) | 2.x |

---

## How to Run

### 1. Restore packages and install Playwright browsers

```bash
cd EbayPlaywrightAutomation
dotnet restore
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install
```

### 2. Run all tests

```bash
dotnet test --logger "console;verbosity=detailed"
```

### 3. Run a single scenario by name

```bash
dotnet test --filter "TestName=Shoes under 220"
```

### 4. Generate Allure HTML report

```bash
allure serve allure-results
```

---

## Architecture

```
EbayPlaywrightAutomation/
│
├── appsettings.json              # Runtime config (URL, browser, timeouts, paths)
├── allureConfig.json             # Allure report settings
│
├── TestData/
│   └── ebay_search.json          # Data-driven scenarios (query, maxPrice, limit, budget)
│
├── DataManagers/
│   ├── ConfigManager.cs          # Typed accessors over appsettings.json (IConfiguration)
│   ├── JsonManager.cs            # Generic JSON file loader / saver (Newtonsoft.Json)
│   └── TestScenario.cs           # POCO models for test data (TestScenario, TestDataRoot)
│
├── Infrastructure/
│   ├── BasePage.cs               # Abstract base for all POMs — Playwright helpers, timeouts
│   ├── PagesFactory.cs           # Lazy ??= registry — one IPage, all page objects
│   ├── EbayInfra.cs              # Root facade — owns browser/context/page lifecycle
│   │
│   ├── Pages/
│   │   ├── HomePage.cs           # Search input → submit
│   │   ├── SearchResultsPage.cs  # Price filter, collect URLs, pagination
│   │   ├── ItemPage.cs           # Variant selection, Add to Cart, overlay dismiss
│   │   └── CartPage.cs           # Open cart, read subtotal, item count
│   │
│   └── BusinessProcesses/
│       └── EbayBusinessProcesses.cs  # SearchItemsByNameUnderPrice, AddItemsToCart,
│                                     # AssertCartTotalNotExceeds
│
├── Utilities/
│   ├── PriceParser.cs            # Regex price extractor ($12.99, US $x, ranges, commas)
│   └── ScreenshotHelper.cs       # Timestamped PNG screenshots + byte[] for Allure
│
└── Tests/
    └── EbayE2ETests.cs           # NUnit [TestCaseSource] data-driven E2E test class
```

### Design Principles

- **Page Object Model (POM)** — every page has its own class; locators and actions live together, tests never touch Playwright directly.
- **OOP / SRP** — `BasePage` holds reusable helpers; `PagesFactory` manages page instances; `EbayBusinessProcesses` owns orchestration logic; tests only call BPs.
- **Data-Driven** — all search parameters come from `TestData/ebay_search.json`; adding a new scenario requires zero code changes.
- **Lazy initialisation** — pages are created on first access via `??=`, reducing unnecessary object construction.
- **IAsyncDisposable** — `EbayInfra` implements `IAsyncDisposable` to guarantee browser cleanup in `[TearDown]`.

---

## Configuration

Edit `appsettings.json` to change runtime behaviour:

```json
{
  "BaseUrl":  "https://www.ebay.com",
  "Browser":  "Chromium",          // Chromium | Firefox | Webkit
  "Headless": false,
  "SlowMo":   50,
  "DefaultTimeoutMs":    30000,
  "NavigationTimeoutMs": 60000,
  "ScreenshotsPath": "Screenshots",
  "AllureResultsPath": "allure-results"
}
```

---

## Test Data

`TestData/ebay_search.json` drives every test case:

```json
{
  "TestScenarios": [
    {
      "TestName":      "Shoes under 220",
      "Query":         "shoes",
      "MaxPrice":      220.00,
      "Limit":         5,
      "BudgetPerItem": 220.00
    }
  ]
}
```

Add a new row to run a new scenario — no code changes needed.

---

## Assumptions & Limitations

- **Login** — tests run as a **guest** (no account). eBay allows guest cart on most regions. If login is required in your region, implement `LoginPage.cs` and call it from `EbayInfra.InitAsync()`.
- **Currency** — prices are parsed as USD (`$`). For other currencies add symbol stripping in `PriceParser.cs`.
- **Variant selection** — variants are chosen **at random** from available (non-disabled) options. A specific selection strategy can be added to `ItemPage.SelectRandomVariantsAsync`.
- **eBay DOM changes** — locators are CSS/attribute-based and may need updating if eBay redesigns its UI. They are isolated to individual Page Object classes for easy maintenance.
- **Cart login wall** — some regions redirect to a login page when adding to cart. The current implementation handles the "Continue shopping" overlay but not a full login redirect.
- **Allure** — results are written to `allure-results/`. Run `allure serve allure-results` to view the HTML report.

---

## Reports

| Type | Location |
|------|----------|
| Screenshots | `bin/Debug/net8.0/Screenshots/` |
| Allure results (raw) | `allure-results/` |
| Allure HTML report | Run `allure serve allure-results` |
