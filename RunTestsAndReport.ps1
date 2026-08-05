# ============================================================
# RunTestsAndReport.ps1
# 1. מריץ את כל הטסטים
# 2. מייצר דוח Allure
# 3. פותח את הדוח אוטומטית בדפדפן
#
# הרצה: לחצי פעמיים על הקובץ
#        או: powershell -File RunTestsAndReport.ps1
#
# דרישה: allure מותקן ונגיש ב-PATH המערכתי
#         להתקנה: https://allurereport.org/docs/install/
# ============================================================

# נתיבים יחסיים — עובדים על כל מחשב ללא שינוי
$scriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectPath   = Join-Path $scriptDir "EbayPlaywrightAutomation.csproj"
$allureResults = Join-Path $scriptDir "bin\Debug\net8.0\allure-results"
$allureReport  = Join-Path $scriptDir "bin\Debug\net8.0\allure-report"

# בדיקה ש-allure מותקן ב-PATH
if (-not (Get-Command "allure" -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Host "ERROR: 'allure' not found in PATH." -ForegroundColor Red
    Write-Host "Please install Allure and add it to PATH: https://allurereport.org/docs/install/" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "   Step 1: Running Tests..."            -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

dotnet test $projectPath --logger "console;verbosity=normal"

Write-Host ""
Write-Host "=======================================" -ForegroundColor Yellow
Write-Host "   Step 2: Generating Allure Report..." -ForegroundColor Yellow
Write-Host "=======================================" -ForegroundColor Yellow

# מריץ allure מה-PATH — עובד על כל מחשב
allure generate $allureResults --output $allureReport --clean

Write-Host ""
Write-Host "=======================================" -ForegroundColor Green
Write-Host "   Step 3: Opening Report in Browser..." -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green

Start-Process (Join-Path $allureReport "index.html")

Write-Host ""
Write-Host "Done! Report opened in browser." -ForegroundColor Green
Write-Host "Report location: $allureReport\index.html"
