# ============================================================
# RunTestsAndReport.ps1
# 1. מריץ את כל הטסטים
# 2. מייצר דוח Allure
# 3. פותח את הדוח אוטומטית בדפדפן
#
# הרצה: לחצי פעמיים על הקובץ
#        או: powershell -File RunTestsAndReport.ps1
# ============================================================

$projectPath   = "c:\Users\YEHUDITSP\source\repos\EbayPlaywrightAutomation\EbayPlaywrightAutomation.csproj"
$allureResults = "c:\Users\YEHUDITSP\source\repos\EbayPlaywrightAutomation\bin\Debug\net8.0\allure-results"
$allureReport  = "c:\Users\YEHUDITSP\source\repos\EbayPlaywrightAutomation\bin\Debug\net8.0\allure-report"
$allureBat     = "C:\tools\allure-2.27.0 (1)\allure-2.27.0\bin\allure.bat"

Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "   Step 1: Running Tests..."            -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

dotnet test $projectPath --logger "console;verbosity=normal"

Write-Host ""
Write-Host "=======================================" -ForegroundColor Yellow
Write-Host "   Step 2: Generating Allure Report..." -ForegroundColor Yellow
Write-Host "=======================================" -ForegroundColor Yellow

# מייצר דוח HTML מתוך קבצי ה-JSON
& $allureBat generate $allureResults --output $allureReport --clean

Write-Host ""
Write-Host "=======================================" -ForegroundColor Green
Write-Host "   Step 3: Opening Report in Browser..." -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green

# פותח את הדוח בדפדפן
Start-Process "$allureReport\index.html"

Write-Host ""
Write-Host "Done! Report opened in browser." -ForegroundColor Green
Write-Host "Report location: $allureReport\index.html"
