using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;
using AventStack.ExtentReports;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using EbayPlaywrightAutomation.DataManagers;
using EbayPlaywrightAutomation.Infrastructure;
using EbayPlaywrightAutomation.Utilities;

namespace EbayPlaywrightAutomation.Tests
{
    [TestFixture]
    [AllureNUnit]
    [AllureSuite("eBay E2E")]
    public class EbayE2ETests
    {
        private EbayInfra _infra = null!;
        private ExtentTest _extentTest = null!;

        // ------------------------------------------------------------------ //
        //  One-time setup/teardown — Extent Report lifecycle                  //
        // ------------------------------------------------------------------ //

        [OneTimeSetUp]
        public void InitReport()
        {
            ExtentReportManager.Init();
        }

        [OneTimeTearDown]
        public void FlushReport()
        {
            ExtentReportManager.Flush();
            ExtentReportManager.OpenReport(); // פותח אוטומטית בדפדפן
        }

        // ------------------------------------------------------------------ //
        //  Test data source                                                    //
        // ------------------------------------------------------------------ //

        private static IEnumerable<TestCaseData> ScenarioSource()
        {
            var root = JsonManager.LoadFromFile<TestDataRoot>("TestData/ebay_search.json");
            foreach (var s in root.TestScenarios)
                yield return new TestCaseData(s).SetName(s.TestName);
        }

        // ------------------------------------------------------------------ //
        //  Setup / Teardown                                                    //
        // ------------------------------------------------------------------ //

        [SetUp]
        public async Task SetUpAsync()
        {
            string testName = TestContext.CurrentContext.Test.Name;
            _extentTest = ExtentReportManager.CreateTest(testName);
            _extentTest.Info("Test started");

            _infra = new EbayInfra();
            await _infra.InitAsync();

            // חיבור ExtentTest ל-BusinessProcesses לרישום בזמן אמת
            _infra.BusinessProcesses.ExtentTest = _extentTest;
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            string message = TestContext.CurrentContext.Result.Message ?? "";

            if (status == TestStatus.Failed)
            {
                // Screenshot לדוח Extent
                byte[] screenshotBytes = await ScreenshotHelper.CaptureBytesAsync(_infra.Page);
                string base64 = Convert.ToBase64String(screenshotBytes);
                _extentTest.Fail("Test FAILED: " + message,
                    MediaEntityBuilder.CreateScreenCaptureFromBase64String(base64).Build());

                // Screenshot לדוח Allure
                AllureApi.AddAttachment("failure_screenshot", "image/png", screenshotBytes, ".png");
            }
            else if (status == TestStatus.Passed)
            {
                _extentTest.Pass("Test PASSED");
            }
            else
            {
                _extentTest.Skip("Test SKIPPED: " + message);
            }

            await _infra.DisposeAsync();
        }

        // ------------------------------------------------------------------ //
        //  Main E2E test                                                       //
        // ------------------------------------------------------------------ //

        [Test]
        [TestCaseSource(nameof(ScenarioSource))]
        [AllureFeature("Search, Cart, Assertion")]
        [AllureTag("e2e", "regression")]
        public async Task EbayFullFlow_SearchAddCartAssert(TestScenario scenario)
        {
            // ── Step 0: Login ───────────────────────────────────────────────
            if (!ConfigManager.SkipLogin)
            {
                _extentTest.Info("Step 0: Login to eBay");
                AllureApi.Step("Login to eBay");

                bool loggedIn = await _infra.BusinessProcesses.LoginAsync(
                    ConfigManager.LoginEmail,
                    ConfigManager.LoginPassword);

                Assert.That(loggedIn, Is.True, "Login failed — check credentials in appsettings.json");
                _extentTest.Pass("Login successful");
            }

            // ── Step 1: Search ──────────────────────────────────────────────
            _extentTest.Info($"Step 1: Search '{scenario.Query}' under ${scenario.MaxPrice}");
            AllureApi.Step($"Search '{scenario.Query}' under ${scenario.MaxPrice}");

            var urls = await _infra.BusinessProcesses.SearchItemsByNameUnderPriceAsync(
                scenario.Query, scenario.MaxPrice, scenario.Limit);

            _extentTest.Info($"Found {urls.Count} qualifying item URL(s)");
            Console.WriteLine($"[Test] URLs collected: {urls.Count}");

            // Attach URL list
            string urlList = string.Join("\n", urls.Select((u, i) => $"{i + 1}. {u}"));
            AllureApi.AddAttachment("collected_urls", "text/plain",
                System.Text.Encoding.UTF8.GetBytes(urlList), ".txt");

            Assert.That(urls, Is.Not.Null, "URL list should not be null");

            if (urls.Count == 0)
            {
                _extentTest.Skip($"No items found for '{scenario.Query}' under ${scenario.MaxPrice}");
                Assert.Inconclusive($"No items found for query '{scenario.Query}' under ${scenario.MaxPrice}. Skipping cart steps.");
                return;
            }

            // ── Step 2: Add to cart ─────────────────────────────────────────
            _extentTest.Info($"Step 2: Adding {urls.Count} item(s) to cart");
            AllureApi.Step($"Add {urls.Count} item(s) to cart");

            int addedCount = await _infra.BusinessProcesses.AddItemsToCartAsync(urls);
            _extentTest.Pass($"Added {addedCount}/{urls.Count} item(s) to cart");

            // ── Step 3: Assert cart total ────────────────────────────────────
            _extentTest.Info($"Step 3: Assert cart total ≤ ${scenario.BudgetPerItem} × {addedCount}");
            AllureApi.Step($"Assert cart total ≤ ${scenario.BudgetPerItem} × {addedCount}");

            await _infra.BusinessProcesses.AssertCartTotalNotExceedsAsync(
                scenario.BudgetPerItem, addedCount);

            _extentTest.Pass("Cart total assertion passed");
        }

        // ------------------------------------------------------------------ //
        //  PriceParser unit test                                               //
        // ------------------------------------------------------------------ //

        [Test]
        [AllureFeature("Utilities")]
        [AllureTag("unit", "price-parser")]
        public void PriceParser_ParsesCommonFormats()
        {
            _extentTest.Info("Testing PriceParser with common eBay price formats");

            Assert.Multiple(() =>
            {
                Assert.That(PriceParser.Parse("$12.99"),        Is.EqualTo(12.99).Within(0.001));
                Assert.That(PriceParser.Parse("US $219.00"),    Is.EqualTo(219.00).Within(0.001));
                Assert.That(PriceParser.Parse("$10.00 to $15.00"), Is.EqualTo(10.00).Within(0.001));
                Assert.That(PriceParser.Parse("USD 99"),        Is.EqualTo(99.0).Within(0.001));
                Assert.That(PriceParser.Parse(null),            Is.Null);
                Assert.That(PriceParser.Parse("Free"),          Is.Null);
            });

            _extentTest.Pass("All price formats parsed correctly");
        }
    }
}
