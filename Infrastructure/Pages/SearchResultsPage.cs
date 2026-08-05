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

        // FIX: eBay changed DOM — items are now inside .srp-river-results
        // Using XPath as required by the spec
        private ILocator ResultItems => Page.Locator(
            "xpath=//ul[contains(@class,'srp-river-results')]//li[not(contains(@class,'header')) and not(contains(@class,'placeholder'))]");

        private ILocator AnyResultContainer => Page.Locator(
            "xpath=//li[contains(@class,'s-item') and not(contains(@class,'s-item--header'))]" +
            " | //*[@data-view='mi:1686|iid:1']");

        private const string XPathPrice = ".//span[contains(@class,'s-item__price')] | .//span[contains(@class,'price')]";
        private const string XPathLink  = ".//a[contains(@class,'s-item__link')] | .//a[contains(@href,'/itm/')]";

        private ILocator NextPageButton => Page.Locator(
            "a.pagination__next, a[aria-label='Go to next search page']");

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

            // דיאגנוסטיקה — מדפיס כמה פריטים כל selector מוצא
            var c1 = await Page.Locator("li.s-item").CountAsync();
            var c2 = await Page.Locator("[data-view='mi:1686|iid:1'] li.s-item").CountAsync();
            var c3 = await Page.Locator("li.s-item:not(.s-item--header)").CountAsync();
            var c4 = await Page.Locator(".srp-results li.s-item").CountAsync();
            Console.WriteLine($"[DIAG] li.s-item={c1} | data-view li.s-item={c2} | not-header={c3} | srp-results={c4}");
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

        public async Task<List<string>> CollectItemUrlsUnderPriceAsync(double maxPrice, int limit)
        {
            var urlSet = new HashSet<string>();
            int emptyPageStreak = 0; // הגנה מפני לולאה אינסופית

            while (urlSet.Count < limit)
            {
                await WaitForResultsAsync();

                var items = await ResultItems.AllAsync();
                Console.WriteLine($"[SearchResults] Found {items.Count} result items on this page (XPath, header filtered).");

                if (items.Count == 0)
                {
                    emptyPageStreak++;
                    Console.WriteLine($"[SearchResults] Empty page streak: {emptyPageStreak}/3");
                    if (emptyPageStreak >= 3)
                    {
                        Console.WriteLine("[SearchResults] 3 consecutive empty pages — stopping.");
                        break;
                    }
                }
                else
                {
                    emptyPageStreak = 0; // איפוס הסטריק אם מצאנו פריטים
                }

                foreach (var item in items)
                {
                    if (urlSet.Count >= limit) break;
                    string? url = await TryGetItemUrlByXPathAsync(item);
                    if (!string.IsNullOrEmpty(url) && !urlSet.Contains(url))
                        urlSet.Add(url);
                }

                if (urlSet.Count >= limit) break;

                bool wentToNextPage = await TryGoToNextPageAsync();
                if (!wentToNextPage) break;
            }

            return urlSet.ToList();
        }

        // ------------------------------------------------------------------ //
        //  Private helpers                                                     //
        // ------------------------------------------------------------------ //

        private async Task<string?> TryGetItemUrlByXPathAsync(ILocator item)
        {
            try
            {
                var linkLocator = item.Locator($"xpath={XPathLink}");
                if (await linkLocator.CountAsync() == 0) return null;

                string? href = await linkLocator.First.GetAttributeAsync("href");
                if (string.IsNullOrWhiteSpace(href)) return null;

                // סינון לינקים פיקטיביים של "Shop on eBay"
                if (href.Contains("rover.ebay.com") || href.Contains("pulsar.ebay.com"))
                {
                    Console.WriteLine($"[XPath] Skipping dummy link: {href[..Math.Min(60, href.Length)]}");
                    return null;
                }

                // לוג המחיר לצרכי debugging בלבד — הסינון כבר נעשה server-side ע"י _udhi ב-URL
                var priceLocator = item.Locator($"xpath={XPathPrice}");
                if (await priceLocator.CountAsync() > 0)
                {
                    string rawPrice = await priceLocator.First.InnerTextAsync();
                    Console.WriteLine($"[XPath] price: '{rawPrice}' → accepted (server-side filtered)");
                }

                return href;
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> TryGoToNextPageAsync()
        {
            // CountAsync() לא מחכה ולא זורק Timeout — בטוח לבדיקה מהירה
            if (await NextPageButton.CountAsync() > 0 && await NextPageButton.First.IsVisibleAsync())
            {
                string currentUrl = Page.Url;
                await NextPageButton.First.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                // eBay CSR — אם ה-URL לא השתנה נחכה ל-NetworkIdle
                if (Page.Url == currentUrl)
                {
                    try
                    {
                        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                            new PageWaitForLoadStateOptions { Timeout = 10000 });
                    }
                    catch (TimeoutException) { }
                }

                if (Page.Url == currentUrl)
                {
                    Console.WriteLine("[SearchResults] Page did not change after Next — stopping.");
                    return false;
                }

                return true;
            }

            Console.WriteLine("[SearchResults] No Next button found — stopping.");
            return false;
        }

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
