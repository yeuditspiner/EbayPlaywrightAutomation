namespace EbayPlaywrightAutomation.DataManagers
{
    /// <summary>
    /// Represents a single data-driven test scenario loaded from ebay_search.json.
    /// </summary>
    public class TestScenario
    {
        public string TestName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public double MaxPrice { get; set; }
        public int Limit { get; set; } = 5;
        public double BudgetPerItem { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Root wrapper matching the structure of ebay_search.json.
    /// </summary>
    public class TestDataRoot
    {
        public List<TestScenario> TestScenarios { get; set; } = new();
    }
}
