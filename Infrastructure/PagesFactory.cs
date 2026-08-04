using Microsoft.Playwright;
using EbayPlaywrightAutomation.Infrastructure.Pages;

namespace EbayPlaywrightAutomation.Infrastructure
{
    /// <summary>
    /// Central registry of all Page Objects. Pages are lazily initialized
    /// and reuse the same IPage instance throughout a test.
    /// </summary>
    public class PagesFactory
    {
        private readonly IPage _page;

        private LoginPage? _loginPage;
        private HomePage? _homePage;
        private SearchResultsPage? _searchResultsPage;
        private ItemPage? _itemPage;
        private CartPage? _cartPage;

        public PagesFactory(IPage page)
        {
            _page = page;
        }

        public LoginPage LoginPage => _loginPage ??= new LoginPage(_page);

        public HomePage HomePage => _homePage ??= new HomePage(_page);

        public SearchResultsPage SearchResultsPage => _searchResultsPage ??= new SearchResultsPage(_page);

        public ItemPage ItemPage => _itemPage ??= new ItemPage(_page);

        public CartPage CartPage => _cartPage ??= new CartPage(_page);
    }
}
