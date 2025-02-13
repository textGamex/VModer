# Set UTF-8 encoding
$OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

Write-Host "=== Starting VModer Extension Build ===" -ForegroundColor Green

# Check tools
Write-Host "=== Checking Required Tools ===" -ForegroundColor Yellow

# Check Node.js
if (!(Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: Node.js not found. Please install Node.js" -ForegroundColor Red
    Write-Host "Download: https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Check npm
if (!(Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: npm not found. Please install npm" -ForegroundColor Red
    exit 1
}

# Check and install vsce
if (!(Get-Command vsce -ErrorAction SilentlyContinue)) {
    Write-Host "=== Installing vsce ===" -ForegroundColor Yellow
    try {
        npm install -g @vscode/vsce
        if ($LASTEXITCODE -ne 0) { throw "vsce installation failed" }
    } catch {
        Write-Host "ERROR: vsce installation failed. Try manually: npm install -g @vscode/vsce" -ForegroundColor Red
        exit 1
    }
}

# Clean old files
Write-Host "=== Cleaning old build files ===" -ForegroundColor Yellow
if (Test-Path "out") {
    Remove-Item "out" -Recurse -Force
}
if (Test-Path "*.vsix") {
    Remove-Item "*.vsix" -Force
}

# Install dependencies and dev tools
Write-Host "=== Installing dependencies and tools ===" -ForegroundColor Yellow
try {
    npm install
    npm install --save-dev esbuild
    if ($LASTEXITCODE -ne 0) { throw "dependency installation failed" }
} catch {
    Write-Host "ERROR: dependency installation failed" -ForegroundColor Red
    exit 1
}

# Run ESLint
Write-Host "=== Running code check ===" -ForegroundColor Yellow
try {
    npm run lint
    if ($LASTEXITCODE -ne 0) { throw "lint check failed" }
} catch {
    Write-Host "ERROR: lint check failed" -ForegroundColor Red
    exit 1
}

# Temporarily skip tests
Write-Host "=== Skipping tests ===" -ForegroundColor Yellow

# Build production version
Write-Host "=== Building production version ===" -ForegroundColor Yellow
try {
    npm run vscode:prepublish
    if ($LASTEXITCODE -ne 0) { throw "build failed" }
} catch {
    Write-Host "ERROR: build failed" -ForegroundColor Red
    exit 1
}

# Package VSIX
Write-Host "=== Packaging VSIX ===" -ForegroundColor Yellow
try {
    vsce package
    if ($LASTEXITCODE -ne 0) { throw "packaging failed" }
} catch {
    Write-Host "ERROR: packaging failed" -ForegroundColor Red
    exit 1
}

# Check VSIX file
$vsixFile = Get-ChildItem -Filter "*.vsix" | Select-Object -First 1
if ($vsixFile) {
    Write-Host "SUCCESS: Build complete! File: $($vsixFile.Name)" -ForegroundColor Green
    Write-Host "=== Installation Instructions ===" -ForegroundColor Cyan
    Write-Host "To install: code --install-extension $($vsixFile.Name)" -ForegroundColor Cyan
    Write-Host "To publish: vsce publish" -ForegroundColor Cyan
} else {
    Write-Host "ERROR: Build failed - VSIX file not generated" -ForegroundColor Red
    exit 1
} 