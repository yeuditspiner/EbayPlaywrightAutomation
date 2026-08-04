# OpenAllureReport.ps1
# מייצר דוח Allure ופותח את התיקייה אוטומטית

$allureResults = "c:\Users\YEHUDITSP\source\repos\EbayPlaywrightAutomation\bin\Debug\net8.0\allure-results"
$allureReport  = "c:\Users\YEHUDITSP\source\repos\EbayPlaywrightAutomation\bin\Debug\net8.0\allure-report"
$allureBat     = "C:\tools\allure-2.27.0 (1)\allure-2.27.0\bin\allure.bat"

Write-Host "Generating Allure report..."

# יצירת הדוח
& $allureBat generate $allureResults --output $allureReport --clean

# פתיחת התיקייה ב-Explorer
Start-Process explorer.exe $allureReport

# פתיחת הדוח בדפדפן
Start-Process "$allureReport\index.html"

Write-Host "Done! Report folder opened."
