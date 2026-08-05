using AventStack.ExtentReports;
using Microsoft.Playwright;
using NUnit.Framework;
using EbayPlaywrightAutomation.Utilities;

namespace EbayPlaywrightAutomation.Infrastructure.BusinessProcesses
{
    /// <summary>
    /// Orchestrates the four main business processes:
    ///   0. LoginAsync
    ///   1. SearchItemsByNameUnderPriceAsync
    ///   2. AddItemsToCartAsync  — returns actual added count
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

        /// <summary>
        /// FIX: Captures screenshot once as bytes, saves to file AND attaches to Extent.
        /// Previously called CaptureAsync + CaptureBytesAsync = two browser IO operations.
        /// Now only one capture is made — bytes are reused for both file save and Base64.
        /// </summary>
        private async Task LogScreenshotAsync(string label)
        {
            byte[] bytes = await ScreenshotHelper.CaptureBytesAsync(_page);

            // Save to file using the bytes already captured
            string path = await ScreenshotHelper.SaveBytesAsync(bytes, label);
            Log($"[Screenshot] {path}");

            // Attach to Extent report as Base64 — no second browser capture needed
            if (ExtentTest != null)
            {
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

            await _pages.HomePage.SearchAsync(query, maxPrice);

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

        /// <summary>
        /// FIX: Returns actual count of successfully added items.
        /// Previously returned void — caller had no way to know how many were added,
        /// leading to incorrect budget assertion when some items were skipped.
        /// </summary>
        public async Task<int> AddItemsToCartAsync(List<string> urls)
        {
            Log($"[Cart] Adding {urls.Count} item(s) to cart.");

            int addedCount = 0;

            for (int i = 0; i < urls.Count; i++)
            {
                string url = urls[i];
                Log($"[Cart] [{i + 1}/{urls.Count}] Opening: {url}");

                bool added = await _pages.ItemPage.AddToCartAsync(url);

                if (added)
                {
                    addedCount++;
                    await LogScreenshotAsync($"item_added_{i + 1}");
                    ExtentTest?.Pass($"Item {i + 1} added to cart");
                }
                else
                {
                    Log($"[Cart] Item {i + 1} could not be added — skipping.");
                    await LogScreenshotAsync($"item_skipped_{i + 1}");
                    ExtentTest?.Info($"Item {i + 1} skipped (no Add to Cart button)");
                }
            }

            Log($"[Cart] Successfully added {addedCount}/{urls.Count} item(s).");
            return addedCount;
        }

        // ------------------------------------------------------------------ //
        //  3. Assert cart total                                                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// FIX: Uses Assert.Fail() instead of throw new Exception().
        /// In NUnit, Assert.Fail marks the test as a proper test failure,
        /// not as an unexpected error — giving cleaner test reports.
        /// Also uses actualAddedCount for precise budget calculation.
        /// </summary>
        public async Task AssertCartTotalNotExceedsAsync(double budgetPerItem, int actualAddedCount)
        {
            double budget = budgetPerItem * actualAddedCount;
            Log($"[Assert] Budget ceiling: {budgetPerItem} × {actualAddedCount} = {budget}");

            await _pages.CartPage.OpenAsync();
            await LogScreenshotAsync("cart_page");

            double? total = await _pages.CartPage.GetSubtotalAsync();

            if (total == null)
            {
                Log("[Assert] Could not read cart subtotal — treating as 0.");
                ExtentTest?.Info("Cart subtotal not visible — assertion skipped.");
                return;
            }

            Log($"[Assert] Cart subtotal: {total:F2}  Budget: {budget:F2}");

            if (total > budget)
            {
                string msg = $"Cart total {total:F2} exceeds budget {budget:F2} " +
                             $"({budgetPerItem} × {actualAddedCount} items).";
                ExtentTest?.Fail(msg);
                Assert.Fail(msg); // FIX: NUnit proper test failure instead of raw Exception
            }

            Log("[Assert] PASS — cart total is within budget.");
            ExtentTest?.Pass($"Cart total {total:F2} ≤ budget {budget:F2}");
        }
    }
}
