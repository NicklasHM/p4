# run-tests.ps1
# Usage: ./run-tests.ps1 <folder>
# Example: ./run-tests.ps1 parser/valid/precedence


param(
    [Parameter(Mandatory=$true)]
    [string]$folder
)

$testsRoot = "../../tests"
$targetPath = Join-Path $testsRoot $folder

if (-not (Test-Path $targetPath)) {
    Write-Host "Error: Folder not found: $targetPath"
    exit 1
}

$files = Get-ChildItem -Path $targetPath -Filter "*.ra" -Recurse

if ($files.Count -eq 0) {
    Write-Host "No .ra files found in $targetPath"
    exit 0
}

function Get-ExpectedOutcome($filePath) {
    if ($filePath -match "\\invalid\\|/invalid/") {
        return "fail"
    }
    return "pass"
}

$passed = 0
$failed = 0

Write-Host ""
Write-Host "Running tests in: $targetPath"
Write-Host "---------------------------------------------"

foreach ($file in $files) {
    $expected = Get-ExpectedOutcome $file.FullName
    $result = dotnet run --project . -- $file.FullName 2>&1
    $success = $result -match "Parsing successful"

    if (($expected -eq "pass" -and $success) -or ($expected -eq "fail" -and -not $success)) {
        Write-Host "PASS: $($file.Name)"
        $passed++
    } else {
        Write-Host "FAIL: $($file.Name)"
        Write-Host "      Expected: $expected"
        Write-Host "      Got: $result"
        $failed++
    }
}

Write-Host "---------------------------------------------"
Write-Host ""
Write-Host "Results: $passed passed, $failed failed"
Write-Host ""

if ($failed -gt 0) {
    exit 1
}