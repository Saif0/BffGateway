#!/bin/bash

# BFF Gateway Test Runner Script
# Simple script to run unit tests with proper configuration

set -e

echo "🧪 BFF Gateway Unit Tests Runner"
echo "================================"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to print colored output
print_step() {
    echo -e "${YELLOW}$1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    print_error "dotnet CLI not found. Please install .NET 8 SDK."
    exit 1
fi

print_success "dotnet CLI found"

# Clean previous builds
print_step "Cleaning previous builds..."
dotnet clean > /dev/null 2>&1 || true
print_success "Build artifacts cleaned"

# Restore packages
print_step "Restoring NuGet packages..."
if dotnet restore > /dev/null 2>&1; then
    print_success "Packages restored successfully"
else
    print_error "Failed to restore packages"
    exit 1
fi

# Build solution
print_step "Building solution..."
if dotnet build --no-restore > /dev/null 2>&1; then
    print_success "Solution built successfully"
else
    print_error "Build failed"
    print_step "Running build with output for debugging..."
    dotnet build --no-restore
    exit 1
fi

# Run tests
print_step "Running unit tests..."
echo ""

# Run Application Tests
if [ -d "tests/BffGateway.Application.Tests" ]; then
    print_step "📦 Running Application Layer Tests..."
    if dotnet test tests/BffGateway.Application.Tests/ --no-build --verbosity quiet; then
        print_success "Application tests passed"
    else
        print_error "Application tests failed"
    fi
    echo ""
fi

# Run Infrastructure Tests
if [ -d "tests/BffGateway.Infrastructure.Tests" ]; then
    print_step "🔧 Running Infrastructure Layer Tests..."
    if dotnet test tests/BffGateway.Infrastructure.Tests/ --no-build --verbosity quiet; then
        print_success "Infrastructure tests passed"
    else
        print_error "Infrastructure tests failed"
    fi
    echo ""
fi

# Run WebApi Tests
if [ -d "tests/BffGateway.WebApi.Tests" ]; then
    print_step "🌐 Running WebApi Layer Tests..."
    if dotnet test tests/BffGateway.WebApi.Tests/ --no-build --verbosity quiet; then
        print_success "WebApi tests passed"
    else
        print_error "WebApi tests failed"
    fi
    echo ""
fi

# Run all tests with coverage (optional)
print_step "📊 Running all tests with summary..."
if dotnet test --no-build --verbosity normal; then
    print_success "All tests completed successfully! 🎉"
else
    print_error "Some tests failed. Check the output above for details."
    exit 1
fi

echo ""
print_success "Test run complete! Check the results above."
echo ""
echo "💡 Tips:"
echo "  - To run specific tests: dotnet test --filter \"FullyQualifiedName~LoginCommandHandlerTests\""
echo "  - To run with coverage: dotnet test --collect:\"XPlat Code Coverage\""
echo "  - To run in watch mode: dotnet watch test"
