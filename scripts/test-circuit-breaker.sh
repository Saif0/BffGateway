#!/bin/bash

echo "🔌 Circuit Breaker Test Script"
echo "=============================="

# Check if k6 is installed
if ! command -v k6 &> /dev/null; then
    echo "❌ k6 is not installed. Please install k6 first:"
    echo "   brew install k6  # macOS"
    echo "   https://k6.io/docs/get-started/installation/"
    exit 1
fi

# Check if services are running
echo "🔍 Checking if services are running..."

# Check BFF Gateway
BFF_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health 2>/dev/null || echo "000")
if [ "$BFF_HEALTH" != "200" ]; then
    echo "❌ BFF Gateway is not running on port 5000"
    echo "   Start it with: cd src/BffGateway.WebApi && dotnet run"
    exit 1
fi

# Check MockProvider
MOCK_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5002/health 2>/dev/null || echo "000")
if [ "$MOCK_HEALTH" != "200" ]; then
    echo "❌ MockProvider is not running on port 5002"
    echo "   Start it with: cd src/MockProvider && dotnet run --urls http://localhost:5002"
    exit 1
fi

echo "✅ Both services are running"
echo ""

# Display current circuit breaker configuration
echo "⚙️  Current Circuit Breaker Configuration:"
echo "   - Failure Threshold: 5 failures"
echo "   - Break Duration: 30 seconds"
echo "   - Sampling Duration: 60 seconds"
echo ""

# Show what the test will do
echo "🧪 Test Plan:"
echo "   Phase 1 (0-15s):  Generate failures using 'Fail' scenario"
echo "   Phase 2 (16-51s): Test fast failures when circuit is OPEN"
echo "   Phase 3 (52-72s): Test recovery when circuit RESETS"
echo ""

# Ask for confirmation
read -p "🚀 Ready to run circuit breaker test? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Test cancelled."
    exit 0
fi

echo ""
echo "🔥 Starting Circuit Breaker Test..."
echo "📊 Watch for these log messages in your BFF Gateway console:"
echo "   - 'Circuit breaker OPEN for outbound provider calls'"
echo "   - 'Circuit breaker HALF-OPEN for outbound provider calls'"
echo "   - 'Circuit breaker RESET for outbound provider calls'"
echo ""

# Run the test
cd performance
k6 run circuit-breaker-test.js

echo ""
echo "✅ Circuit Breaker Test Complete!"
echo ""
echo "📈 Analysis Tips:"
echo "   1. Check the test output above for circuit breaker triggers"
echo "   2. Look at your BFF Gateway logs for circuit breaker state changes"
echo "   3. Phase 1 should show high error rates and slow responses"
echo "   4. Phase 2 should show fast failures (circuit breaker open)"
echo "   5. Phase 3 should show recovery with normal response times"
echo ""
echo "🔧 To adjust circuit breaker settings, edit:"
echo "   src/BffGateway.WebApi/appsettings.json (CircuitBreaker section)"
