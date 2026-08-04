using Microsoft.Playwright;
using EbayPlaywrightAutomation.Infrastructure;

namespace EbayPlaywrightAutomation.Infrastructure.Pages
{
    /// <summary>
    /// Page Object for an individual eBay item (product detail) page.
    /// All locators use XPath.
    /// Handles variant selection and adding the item to the cart.
    /// </summary>
    public class ItemPage : BasePage
    {
        #region XPath Locators
        private ILocator AddToCartButton => Page.Locator(
            "xpath=//button[@id='atcBtn_btn'] | //*[@data-testid='x-mweb-atc-btn'] | //button[normalize-space(.)='Add to cart']").First;

        private ILocator BuyItNowButton => Page.Locator(
            "xpath=//button[@id='binBtn_btn'] | //button[normalize-space(.)='Buy It Now']").First;

        // Variant <select> dropdowns (size, colour, etc.)
        private ILocator VariantSelectBoxes => Page.Locator(
            "xpath=//select[contains(@class,'msku-sel-single')]");

        // Variant button-group tiles
        private ILocator VariantButtonGroups => Page.Locator(
            "xpath=//*[contains(@class,'x-msku__select-box')]");

        // Cart overlay — continue shopping button
        private ILocator ContinueShoppingButton => Page.Locator(
            "xpath=//button[normalize-space(.)='Continue shopping'] | //a[normalize-space(.)='Continue shopping']").First;

        // Quantity input
        private ILocator QuantityInput => Page.Locator(
            "xpath=//input[@id='qtyTextBox'] | //input[contains(@aria-label,'quantity')]").First;
        #endregion

        private static readonly Random _rng = new();

        public ItemPage(IPage page) : base(page) { }

        /// <summary>
        /// Navigates to the item URL, selects variants at random, clicks Add to Cart.
        /// </summary>
        public async Task<bool> AddToCartAsync(string url)
        {
            await GoToAsync(url);
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await SelectRandomVariantsAsync();

            if (await IsVisibleAsync(QuantityInput, 2000))
                await FillAsync(QuantityInput, "1");

            bool hasAtc = await IsVisibleAsync(AddToCartButton, 5000);
            if (!hasAtc) return false;

            await ClickAsync(AddToCartButton);
            await DismissCartOverlayAsync();
            return true;
        }

        /// <summary>
        /// Picks a random valid option for each variant on the page.
        /// Handles both &lt;select&gt; dropdowns and button-group tiles via XPath.
        /// </summary>
        private async Task SelectRandomVariantsAsync()
        {
            // --- <select> dropdowns ---
            int selectCount = await VariantSelectBoxes.CountAsync();
            for (int i = 0; i < selectCount; i++)
            {
                var select = VariantSelectBoxes.Nth(i);
                // XPath: options that have a value (exclude placeholder)
                var options = await select
                    .Locator("xpath=.//option[@value and @value!='']")
                    .AllAsync();
                if (options.Count == 0) continue;

                var chosen = options[_rng.Next(options.Count)];
                string? value = await chosen.GetAttributeAsync("value");
                if (!string.IsNullOrEmpty(value))
                    await select.SelectOptionAsync(value);
            }

            // --- Button-group tiles ---
            var groups = await VariantButtonGroups.AllAsync();
            foreach (var group in groups)
            {
                // XPath: available (non-disabled) tile options
                var available = await group
                    .Locator("xpath=.//*[contains(@class,'listbox__option') and not(@aria-disabled='true')]")
                    .AllAsync();
                if (available.Count == 0) continue;

                var pick = available[_rng.Next(available.Count)];
                await pick.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        /// <summary>Dismisses the "Continue shopping" overlay after Add to Cart.</summary>
        private async Task DismissCartOverlayAsync()
        {
            bool overlayVisible = await IsVisibleAsync(ContinueShoppingButton, 4000);
            if (overlayVisible)
                await ClickAsync(ContinueShoppingButton);
        }

        /// <summary>Returns true when the Add to Cart button is visible.</summary>
        public async Task<bool> CanAddToCartAsync()
            => await IsVisibleAsync(AddToCartButton, 5000);
    }
}
