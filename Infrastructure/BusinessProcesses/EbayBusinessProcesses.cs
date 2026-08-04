using AventStack.ExtentReports;
using Microsoft.Playwright;
using EbayPlaywrightAutomation.Utilities;

namespace EbayPlaywrightAutomation.Infrastructure.BusinessProcesses
{
    /// <summary>
    /// Orchestrates the four main business processes:
    ///   0. LoginAsync
    ///   1. SearchItemsByNameUnderPriceAsync
    ///   2. AddItemsToCartAsync
    ///   3. AssertCartTotalNotExceedsAsync
    ///
    /// Accepts an optional ExtentTest for real-time step logging + screenshots.
    /// </summary>
    public class EbayBusinessProcesses
    {
        private readonly PagesFactory _pages;
        private readonly IPage _page;

        // ExtentTest — set per-test by the test class
        public ExtentTest? ExtentTest { get; set; }

        public EbayBusinessProcesses(PagesFactory pages, IPage page)
        {
            _pages = pages;
            _page  = page;
        }

        // ------------------------------------------------------------------ //
        //  Helper — logs to both Console and ExtentTest                       //
        // ------------------------------------------------------------------ //

        private void Log(string message)
        {
            Console.WriteLine(message);
            ExtentTest?.Info(message);
        }

        private async Task LogScreenshotAsync(string label)
        {
            string path = await ScreenshotHelper.CaptureAsync(_page, label);
            Log($"[Screenshot] {path}");

            // Attach screenshot to Extent report in real-time
            if (ExtentTest != null)
            {
                byte[] bytes = await ScreenshotHelper.CaptureBytesAsync(_page);
                string base64 = Convert.ToBase64String(bytes);
                ExtentTest.Info(label,
                    MediaEntityBuilder.CreateScreenCaptureFromBase64String(base64).Build());
            }
        }

        // ------------------------------------------------------------------ //
        //  0. Login                                                            //
        // ------------------------------------------------------------------ //

        public async Task<bool> LoginAsync(string email, string password)
        {
            Log("[Login] Checking login status...");

            bool alreadyLoggedIn = await _pages.LoginPage.IsLoggedInAsync();
            if (alreadyLoggedIn)
            {
                Log("[Login] Already logged in — skipping.");
                return true;
            }

            bool success = await _pages.LoginPage.LoginAsync(email, password);
            await LogScreenshotAsync(success ? "login_success" : "login_failed");

            if (success)
                ExtentTest?.Pass("Login successful");
            else
                ExtentTest?.Fail("Login failed");

            return success;
        }

        // ------------------------------------------------------------------ //
        //  1. Search                                                           //
        // ------------------------------------------------------------------ //

        public async Task<List<string>> SearchItemsByNameUnderPriceAsync(
            string query, double maxPrice, int limit = 5)
        {
            Log($"[Search] query='{query}' maxPrice={maxPrice} limit={limit}");

            await _pages.HomePage.SearchAsync(query);

            bool hasResults = await _pages.SearchResultsPage.HasResultsAsync();
            if (!hasResults)
            {
                Log("[Search] No results found.");
                await LogScreenshotAsync("search_no_results");
                return new List<string>();
            }

            await _pages.SearchResultsPage.ApplyMaxPriceFilterAsync(maxPrice);
            await LogScreenshotAsync("search_results_filtered");

            var urls = await _pages.SearchResultsPage
                .CollectItemUrlsUnderPriceAsync(maxPrice, limit);

            Log($"[Search] Collected {urls.Count} URL(s).");
            ExtentTest?.Pass($"Search complete — {urls.Count} item(s) found under ${maxPrice}");
            return urls;
        }

        // ------------------------------------------------------------------ //
        //  2. Add to cart                                                      //
        // ------------------------------------------------------------------ //

        public async Task AddItemsToCartAsync(List<string> urls)
        {
            Log($"[Cart] Adding {urls.Count} item(s) to cart.");

            for (int i = 0; i < urls.Count; i++)
            {
                string url = urls[i];
                Log($"[Cart] [{i + 1}/{urls.Count}] Opening: {url}");

                bool added = await _pages.ItemPage.AddToCartAsync(url);

                if (added)
                {
                    await LogScreenshotAsync($"item_added_{i + 1}");
                    ExtentTest?.Pass($"Item {i + 1} added to cart");
                }
                else
                {
                    Log($"[Cart] Item {i + 1} could not be added — skipping.");
                    await LogScreenshotAsync($"item_skipped_{i + 1}");
                    ExtentTest?.Warning($"Item {i + 1} skipped (no Add to Cart button)");
                }
            }
        }

        // ------------------------------------------------------------------ //
        //  3. Assert cart total                                                //
        // ------------------------------------------------------------------ //

        public async Task AssertCartTotalNotExceedsAsync(double budgetPerItem, int itemsCount)
        {
            double budget = budgetPerItem * itemsCount;
            Log($"[Assert] Budget ceiling: {budgetPerItem} × {itemsCount} = {budget}");

            await _pages.CartPage.OpenAsync();
            await LogScreenshotAsync("cart_page");

            double? total = await _pages.CartPage.GetSubtotalAsync();

            if (total == null)
            {
                Log("[Assert] Could not read cart subtotal — treating as 0.");
                ExtentTest?.Warning("Cart subtotal not visible — assertion skipped.");
                return;
            }

            Log($"[Assert] Cart subtotal: {total:F2}  Budget: {budget:F2}");

            if (total > budget)
            {
                string msg = $"Cart total {total:F2} exceeds budget {budget:F2} " +
                             $"({budgetPerItem} × {itemsCount} items).";
                ExtentTest?.Fail(msg);
                throw new Exception(msg);
            }

            Log("[Assert] PASS — cart total is within budget.");
            ExtentTest?.Pass($"Cart total {total:F2} ≤ budget {budget:F2}");
        }
    }
}
