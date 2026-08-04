using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;

namespace EbayPlaywrightAutomation.Utilities
{
    /// <summary>
    /// Singleton manager for ExtentReports Spark HTML report (v5).
    /// Call Init() once in [OneTimeSetUp] and Flush() in [OneTimeTearDown].
    /// </summary>
    public static class ExtentReportManager
    {
        private static ExtentReports? _extent;
        private static ExtentSparkReporter? _sparkReporter;

        public static string ReportPath { get; private set; } = string.Empty;

        /// <summary>Initializes the Extent Spark HTML report.</summary>
        public static void Init()
        {
            string reportDir = Path.Combine(AppContext.BaseDirectory, "ExtentReports");
            Directory.CreateDirectory(reportDir);

            ReportPath = Path.Combine(reportDir, "TestReport.html");

            _sparkReporter = new ExtentSparkReporter(ReportPath);
            _sparkReporter.Config.DocumentTitle = "eBay Automation Report";
            _sparkReporter.Config.ReportName    = "eBay E2E Test Results";
            _sparkReporter.Config.Theme         = Theme.Dark;

            _extent = new ExtentReports();
            _extent.AttachReporter(_sparkReporter);
            _extent.AddSystemInfo("Application", "eBay");
            _extent.AddSystemInfo("Framework",   "Playwright + NUnit");
            _extent.AddSystemInfo("Browser",     "Chromium");
            _extent.AddSystemInfo("Environment", "QA");
        }

        /// <summary>Creates a new test node in the report.</summary>
        public static ExtentTest CreateTest(string testName, string description = "")
            => _extent!.CreateTest(testName, description);

        /// <summary>Writes the report to disk.</summary>
        public static void Flush()
        {
            _extent?.Flush();
            Console.WriteLine($"[ExtentReport] Report saved: {ReportPath}");
        }

        /// <summary>Opens the report in the default browser.</summary>
        public static void OpenReport()
        {
            if (File.Exists(ReportPath))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = ReportPath,
                    UseShellExecute = true
                });
        }
    }
}
