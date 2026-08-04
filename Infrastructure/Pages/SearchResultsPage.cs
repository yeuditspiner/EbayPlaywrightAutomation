using Microsoft.Playwright;
using EbayPlaywrightAutomation.Infrastructure;
using EbayPlaywrightAutomation.Utilities;

namespace EbayPlaywrightAutomation.Infrastructure.Pages
{
    /// <summary>
    /// Page Object for the eBay search results page.
    /// Handles price filtering, result item collection via XPath, and pagination.
    ///
    /// Two critical eBay considerations addressed here:
    /// 1. First item is often a dummy "Shop on eBay" header — filtered via XPath
    /// 2. Next page uses Client-Side Rendering — WaitForURLAsync + NetworkIdle
    /// </summary>
    public class SearchResultsPage : BasePage
    {
        #region XPath Locators

        private ILocator MaxPriceInput => Page.Locator(
            "xpath=//input[@aria-label='Maximum Value in $'] | //input[@placeholder='Max']");

        private ILocator PriceFilterSubmit => Page.Locator(
            "xpath=//button[contains(@class,'x-refine__go-btn')] | //button[@aria-label='Price filter submit button']");

        // FIX 1: מסנן s-item--header (הפריט הפיקטיבי הראשון "Shop on eBay")
        private ILocator ResultItems => Page.Locator(
            "xpath=//li[contains(@class,'s-item')" +
            " and not(contains(@class,'s-item--large'))" +
            " and not(contains(@class,'s-item--placeholder'))" +
            " and not(contains(@class,'s-item--header'))]" +
            " | //*[@data-view='mi:1686|iid:1']//li[contains(@class,'s-item')" +
            " and not(contains(@class,'s-item--header'))]");

        private ILocator AnyResultContainer => Page.Locator(
            "xpath=//li[contains(@class,'s-item') and not(contains(@class,'s-item--header'))]" +
            " | //*[@data-view='mi:1686|iid:1']");

        private const string XPathPrice = ".//span[contains(@class,'s-item__price')]";
        private const string XPathLink  = ".//a[contains(@class,'s-item__link')]";

        private ILocator NextPageButton => Page.Locator(
            "xpath=//a[contains(@class,'pagination__next')] | //*[@aria-label='Go to next search page']");

        #endregion

        public SearchResultsPage(IPage page) : base(page) { }

        // ------------------------------------------------------------------ //
        //  Wait helper — SPA-safe                                              //
        // ------------------------------------------------------------------ //

        private async Task WaitForResultsAsync(int timeoutMs = 15000)
        {
            try
            {
                await AnyResultContainer.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State   = WaitForSelectorState.Visible,
                    Timeout = timeoutMs
                });
            }
            catch (TimeoutException)
            {
                Console.WriteLine("[SearchResults] Timed out waiting for result items.");
            }
        }

        // ------------------------------------------------------------------ //
        //  Price filter                                                        //
        // ------------------------------------------------------------------ //

        public async Task ApplyMaxPriceFilterAsync(double maxPrice)
        {
            bool maxVisible = await IsVisibleAsync(MaxPriceInput, 5000);
            if (!maxVisible)
            {
                Console.WriteLine("[SearchResults] Price filter panel not found — skipping.");
                return;
            }

            await FillAsync(MaxPriceInput, maxPrice.ToString("F0"));

            bool submitVisible = await IsVisibleAsync(PriceFilterSubmit, 3000);
            if (submitVisible)
                await ClickAsync(PriceFilterSubmit);
            else
                await MaxPriceInput.PressAsync("Enter");

            await WaitForResultsAsync();
        }

        // ------------------------------------------------------------------ //
        //  Collect item URLs via XPath                                         //
        // ------------------------------------------------------------------ //

        private const int MaxPages = 3; // הגבלת עמודים — מניעת ריצה אינסופית

        public async Task<List<string>> CollectItemUrlsUnderPriceAsync(double maxPrice, int limit)
        {
            // HashSet מונע כפילויות אוטומטית (פריטים Sponsored שמופיעים פעמיים)
            var urlSet = new HashSet<string>();
            int pageCount = 0;

            while (urlSet.Count < limit && pageCount < MaxPages)
            {
                pageCount++;
                await WaitForResultsAsync();

                var items = await ResultItems.AllAsync();
                Console.WriteLine($"[SearchResults] Page {pageCount}/{MaxPages} — Found {items.Count} result items (XPath, header filtered).");

                foreach (var item in items)
                {
                    if (urlSet.Count >= limit) break;

                    string? url = await TryGetItemUrlByXPathAsync(item, maxPrice);

                    if (!string.IsNullOrEmpty(url) && !urlSet.Contains(url))
                        urlSet.Add(url);
                }

                if (urlSet.Count >= limit) break;

                if (pageCount >= MaxPages)
                {
                    Console.WriteLine($"[SearchResults] Reached max page limit ({MaxPages}) — stopping with {urlSet.Count} URLs.");
                    break;
                }

                if (await IsVisibleAsync(NextPageButton, 3000))
                {
                    Console.WriteLine($"[SearchResults] Collected {urlSet.Count}/{limit} — going to next page.");
                    string currentUrl = Page.Url;
                    await ClickAsync(NextPageButton);

                    // FIX 2: eBay CSR — נחכה לשינוי URL ואם לא משתנה נחכה ל-NetworkIdle
                    bool urlChanged = false;
                    try
                    {
                        await Page.WaitForURLAsync(
                            u => u != currentUrl,
                            new PageWaitForURLOptions { Timeout = 5000 });
                        urlChanged = true;
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("[SearchResults] URL did not change — waiting for NetworkIdle (CSR).");
                    }

                    // בכל מקרה — נחכה ל-NetworkIdle כדי לוודא שה-DOM התעדכן
                    try
                    {
                        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                            new PageWaitForLoadStateOptions { Timeout = 10000 });
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("[SearchResults] NetworkIdle timeout — continuing anyway.");
                    }

                    if (!urlChanged && Page.Url == currentUrl)
                    {
                        Console.WriteLine("[SearchResults] Page did not change after Next — stopping.");
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return urlSet.ToList();
        }

        // ------------------------------------------------------------------ //
        //  Private helpers                                                     //
        // ------------------------------------------------------------------ //

        private async Task<string?> TryGetItemUrlByXPathAsync(ILocator item, double maxPrice)
        {
            try
            {
                // בדיקת null לפני Parse — מניעת NullReferenceException
                var priceLocator = item.Locator($"xpath={XPathPrice}");
                if (await priceLocator.CountAsync() == 0) return null;

                string rawPrice = await priceLocator.First.InnerTextAsync();
                if (string.IsNullOrWhiteSpace(rawPrice)) return null;  // null check

                double? parsed = PriceParser.Parse(rawPrice);
                Console.WriteLine($"[XPath] Raw price: '{rawPrice}' → parsed: {parsed}");

                if (parsed == null || parsed > maxPrice) return null;

                var linkLocator = item.Locator($"xpath={XPathLink}");
                if (await linkLocator.CountAsync() == 0) return null;

                string? href = await linkLocator.First.GetAttributeAsync("href");
                if (string.IsNullOrWhiteSpace(href)) return null;  // null check

                // סינון לינקים פיקטיביים של "Shop on eBay"
                if (href.Contains("rover.ebay.com") || href.Contains("pulsar.ebay.com"))
                {
                    Console.WriteLine($"[XPath] Skipping dummy link: {href[..Math.Min(60, href.Length)]}");
                    return null;
                }

                return href;
            }
            catch
            {
                return null;
            }
        }

        // ------------------------------------------------------------------ //
        //  Has results check                                                   //
        // ------------------------------------------------------------------ //

        public async Task<bool> HasResultsAsync()
        {
            Console.WriteLine($"[SearchResults] URL: {Page.Url}");
            Console.WriteLine($"[SearchResults] Title: {await Page.TitleAsync()}");

            await WaitForResultsAsync(10000);

            int count = await ResultItems.CountAsync();
            if (count > 0)
            {
                Console.WriteLine($"[SearchResults] Found {count} result items.");
                return true;
            }

            var fallbacks = new[]
            {
                "xpath=//ul[contains(@class,'srp-results')]//li[contains(@class,'s-item') and not(contains(@class,'s-item--header'))]",
                "xpath=//*[contains(@class,'s-item__wrapper')]",
                "xpath=//*[@data-view='mi:1686|iid:1']"
            };

            foreach (var xpath in fallbacks)
            {
                int fb = await Page.Locator(xpath).CountAsync();
                if (fb > 0)
                {
                    Console.WriteLine($"[SearchResults] Fallback found {fb} items: {xpath}");
                    return true;
                }
            }

            Console.WriteLine("[SearchResults] No result items found.");
            return false;
        }
    }
}
