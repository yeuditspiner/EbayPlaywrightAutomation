using Microsoft.Playwright;
using EbayPlaywrightAutomation.Infrastructure;
using EbayPlaywrightAutomation.Utilities;

namespace EbayPlaywrightAutomation.Infrastructure.Pages
{
    /// <summary>
    /// Page Object for the eBay shopping cart page.
    /// All locators use XPath.
    /// </summary>
    public class CartPage : BasePage
    {
        private const string CartUrl = "https://cart.ebay.com/";

        #region XPath Locators
        // Cart subtotal — tries multiple XPath expressions for different eBay UI versions
        private ILocator SubtotalLocator => Page.Locator(
            "xpath=//*[@id='subtotal-value'] | " +
            "//*[@data-test-id='cart-subtotal-value'] | " +
            "//*[contains(@class,'sc-subtotal__value')] | " +
            "//*[contains(@class,'subtotal')]//span | " +
            "//span[contains(@class,'cart-bucket-footer-total-price')]").First;

        // Individual item price elements in cart
        private ILocator ItemPrices => Page.Locator(
            "xpath=//span[contains(@class,'sc-item-price-secondary')] | " +
            "//span[contains(@class,'item-price')] | " +
            "//*[contains(@class,'cart-item__price')]//span");

        // Empty cart message
        private ILocator EmptyCartMessage => Page.Locator(
            "xpath=//h2[contains(normalize-space(.),'Your cart is empty')] | " +
            "//*[contains(@class,'empty-cart')]").First;
        #endregion

        public CartPage(IPage page) : base(page) { }

        /// <summary>Navigates directly to the eBay cart.</summary>
        public async Task OpenAsync()
        {
            await GoToAsync(CartUrl);
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        /// <summary>
        /// Reads the cart subtotal using XPath and returns it as a double.
        /// Returns null if the element is not found or the value cannot be parsed.
        /// </summary>
        public async Task<double?> GetSubtotalAsync()
        {
            bool visible = await IsVisibleAsync(SubtotalLocator, 10000);
            if (!visible) return null;

            string raw = await GetTextAsync(SubtotalLocator);
            return PriceParser.Parse(raw);
        }

        /// <summary>Returns true when at least one item is present in the cart.</summary>
        public async Task<bool> HasItemsAsync()
        {
            bool empty = await IsVisibleAsync(EmptyCartMessage, 5000);
            return !empty;
        }

        /// <summary>Returns the number of item price elements found in the cart.</summary>
        public async Task<int> GetItemCountAsync()
            => await ItemPrices.CountAsync();
    }
}
