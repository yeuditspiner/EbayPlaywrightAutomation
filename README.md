# eBay Playwright Automation — C#

End-to-end automation for eBay using **Microsoft Playwright** + **NUnit** + **Extent Reports** + **Allure Reports**, written in C# (.NET 8).

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        TEST LAYER                               │
│   EbayE2ETests.cs  ←  [TestCaseSource]  ←  ebay_search.json   │
└───────────────────────────┬─────────────────────────────────────┘
                            │ calls
┌───────────────────────────▼─────────────────────────────────────┐
│                   BUSINESS PROCESS LAYER                        │
│              EbayBusinessProcesses.cs                           │
│  SearchItemsByNameUnderPrice │ AddItemsToCart │ AssertCartTotal  │
└──────┬──────────────────────┬───────────────────────┬───────────┘
       │                      │                       │
┌──────▼──────┐  ┌────────────▼──────────┐  ┌────────▼──────────┐
│  PAGE LAYER (POM)                                               │
│  HomePage   │  │ SearchResultsPage     │  │ ItemPage/CartPage  │
│             │  │ (filter+pagination)   │  │ (variants+cart)    │
└──────┬──────┘  └────────────┬──────────┘  └────────┬──────────┘
       │                      │                       │
┌──────▼──────────────────────▼───────────────────────▼──────────┐
│                      INFRASTRUCTURE                             │
│   BasePage.cs │ PagesFactory.cs │ EbayInfra.cs                  │
│   (helpers)   │ (lazy registry) │ (browser lifecycle)           │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│                    SUPPORT LAYERS                               │
│  ConfigManager │ JsonManager │ PriceParser │ ScreenshotHelper   │
│  ExtentReportManager │ TestScenario (POCO)                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| Node.js (for Playwright browsers) | 18+ |
| Allure CLI (optional) | 2.x |

---

## How to Run

### 1. Restore packages and install Playwright browsers

```bash
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

### 4. Run with settings file

```bash
dotnet test --settings ebay.runsettings
```

### 5. View Extent HTML report

After the test run, the report opens **automatically** in the browser.
Manual path: `bin/Debug/net8.0/ExtentReports/TestReport.html`

### 6. Generate Allure HTML report (optional)

```bash
allure serve allure-results
```

---

## Layer Structure

| Layer | Files | Responsibility |
|-------|-------|----------------|
| **Tests** | `Tests/EbayE2ETests.cs` | NUnit test cases, setup/teardown |
| **Business Processes** | `Infrastructure/BusinessProcesses/` | Orchestration logic — search, add to cart, assert |
| **Pages (POM)** | `Infrastructure/Pages/` | One class per page — locators + actions |
| **Infrastructure** | `Infrastructure/EbayInfra.cs`, `BasePage.cs`, `PagesFactory.cs` | Browser lifecycle, shared helpers, page registry |
| **Data** | `TestData/`, `DataManagers/` | JSON test data, config, models |
| **Utilities** | `Utilities/` | Price parsing, screenshots, reporting |

---

## Project Structure

```
EbayPlaywrightAutomation/
│
├── appsettings.json                   # Runtime config (URL, browser, timeouts)
├── allureConfig.json                  # Allure report settings
├── ebay.runsettings                   # NUnit run settings
│
├── TestData/
│   └── ebay_search.json               # Data-driven test scenarios
│
├── DataManagers/
│   ├── ConfigManager.cs               # Typed accessors over appsettings.json
│   ├── JsonManager.cs                 # Generic JSON loader/saver
│   └── TestScenario.cs                # POCO models (TestScenario, TestDataRoot)
│
├── Infrastructure/
│   ├── BasePage.cs                    # Abstract base — Playwright helpers, timeouts
│   ├── PagesFactory.cs                # Lazy ??= page registry
│   ├── EbayInfra.cs                   # Root facade — browser/context/page lifecycle
│   │
│   ├── Pages/
│   │   ├── HomePage.cs                # Search → navigate with price filter in URL
│   │   ├── SearchResultsPage.cs       # Collect URLs via XPath, pagination
│   │   ├── ItemPage.cs                # Variant selection, Add to Cart
│   │   ├── CartPage.cs                # Read subtotal, item count
│   │   └── LoginPage.cs               # Login (optional, Skip=true by default)
│   │
│   └── BusinessProcesses/
│       └── EbayBusinessProcesses.cs   # SearchItemsByNameUnderPrice
│                                      # AddItemsToCart
│                                      # AssertCartTotalNotExceeds
│
├── Utilities/
│   ├── ExtentReportManager.cs         # Extent Spark HTML report (Dark theme, v5)
│   ├── PriceParser.cs                 # Price extractor — $, ILS, USD, ranges
│   └── ScreenshotHelper.cs            # Timestamped PNG + base64 for reports
│
└── Tests/
    └── EbayE2ETests.cs                # NUnit [TestCaseSource] data-driven E2E tests
```

---

## Design Principles

- **Page Object Model (POM)** — every page is a dedicated class; locators and actions live together; tests never touch Playwright directly.
- **OOP / SRP** — `BasePage` holds reusable helpers; `PagesFactory` manages page instances; `EbayBusinessProcesses` owns orchestration; tests only call business processes.
- **Data-Driven** — all parameters come from `TestData/ebay_search.json`; adding a scenario requires zero code changes.
- **Lazy initialisation** — pages are created on first access via `??=`.
- **IAsyncDisposable** — `EbayInfra` guarantees browser cleanup in `[TearDown]`.

---

## Configuration

Edit `appsettings.json` to change runtime behaviour:

```json
{
  "BaseUrl":  "https://www.ebay.com",
  "Browser":  "Chromium",
  "Headless": false,
  "SlowMo":   50,
  "DefaultTimeoutMs":    30000,
  "NavigationTimeoutMs": 120000,
  "ScreenshotsPath": "Screenshots",
  "AllureResultsPath": "allure-results",
  "Login": {
    "Skip": true
  }
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

## 🔐 Authentication Strategy

By default, the test suite executes in **Guest Mode** (unauthenticated) to streamline test execution, reduce network overhead, and avoid anti-bot mechanisms or CAPTCHA triggers during automated checkout flows.

- **Default Behavior:** `ConfigManager.SkipLogin` is set to `true`, bypassing credentials-based login.
- **Optional Login:** Authentication can be enabled via `appsettings.json` by configuring valid user credentials and setting `SkipLogin` to `false`.

```json
"Login": {
  "Email": "your@email.com",
  "Password": "yourpassword",
  "Skip": false
}
```

---

## Assumptions & Limitations

- **Login** — tests run as a **guest** by default (`Skip: true`). `LoginPage.cs` is implemented and can be enabled via config.
- **Currency** — eBay may display prices in ILS when accessed from Israel. The search URL uses `_udhi` (server-side price cap) to filter results before parsing. `PriceParser` strips ILS, USD, $, ₪ symbols.
- **Variant selection** — variants (size/color) are chosen at random from available (non-disabled) options.
- **eBay DOM changes** — locators are CSS/XPath-based and isolated to Page Object classes for easy maintenance.
- **Cart login wall** — handles the "Continue shopping" overlay. Full login redirect requires enabling `LoginPage`.

---

## 📊 Dual-Reporting Architecture

The framework utilizes a dual-reporting system to support both local development/debugging and enterprise CI/CD pipeline integration:

1. **Extent Reports (Local & Immediate Feedback):**
   - Generates a lightweight, standalone HTML report automatically opened upon test completion.
   - Embedded with Base64 screenshot captures for step-by-step UI verification and fast local debugging.

2. **Allure Framework (CI/CD & Analytics Integration):**
   - Structures test results by `@AllureFeature`, `@AllureSuite`, and `@AllureTag` for high-level business categorization.
   - Attaches test logs, dynamically collected URLs (`collected_urls.txt`), and step attachments suitable for test history tracking in CI/CD pipelines (e.g., GitHub Actions, Jenkins).

| Type | Location | How to open |
|------|----------|-------------|
| **Extent HTML report** | `bin/Debug/net8.0/ExtentReports/TestReport.html` | Opens automatically after test run |
| Screenshots | Embedded in Extent report as base64 | Visible inside the report |
| Allure results (raw) | `allure-results/` | Run `allure serve allure-results` |
| Allure HTML report | Generated on demand | Run `allure serve allure-results` |
