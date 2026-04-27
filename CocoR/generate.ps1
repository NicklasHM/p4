# generate.ps1
# Regenerates Scanner.cs and Parser.cs from the grammar file

$outputDir = "../src/RAL/Generated"

# Create output directory if it doesn't exist
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir
}

./Coco.exe ralGrammar.cs.atg -o $outputDir

Write-Host "Generated Scanner.cs and Parser.cs in $outputDir"