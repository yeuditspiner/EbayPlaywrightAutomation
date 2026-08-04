using Microsoft.Playwright;
using EbayPlaywrightAutomation.Infrastructure;

namespace EbayPlaywrightAutomation.Infrastructure.Pages
{
    /// <summary>
    /// Page Object for the eBay home page.
    /// All locators use XPath.
    /// </summary>
    public class HomePage : BasePage
    {
        #region XPath Locators
        private ILocator SearchInput  => Page.Locator("xpath=//input[@id='gh-ac']");
        private ILocator SearchButton => Page.Locator("xpath=//input[@id='gh-btn'] | //button[@id='gh-search-btn']");
        private ILocator SignInLink   => Page.Locator("xpath=//a[contains(@href,'signin')]").First;
        #endregion

        public HomePage(IPage page) : base(page) { }

        /// <summary>Types a query into the search bar and submits it.</summary>
        public async Task SearchAsync(string query)
        {
            await FillAsync(SearchInput, query);
            await ClickAsync(SearchButton);
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        /// <summary>Returns true when the home page search bar is visible.</summary>
        public async Task<bool> IsReadyAsync()
            => await IsVisibleAsync(SearchInput);
    }
}
