#!/bin/bash

# Test script for BFF Gateway endpoints

echo "=== BFF Gateway API Testing ==="
echo ""

# Test health endpoints
echo "1. Testing Health Endpoints:"
echo "   Live Health Check:"
curl -s http://localhost:5000/health/live | jq '.' 2>/dev/null || echo "$(curl -s http://localhost:5000/health/live)"
echo ""

echo "   Ready Health Check:"
curl -s http://localhost:5000/health/ready | jq '.' 2>/dev/null || echo "$(curl -s http://localhost:5000/health/ready)"
echo ""

# Test v1 auth endpoint
echo "2. Testing v1 Authentication:"
curl -s -X POST http://localhost:5000/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"password123"}' | \
  jq '.' 2>/dev/null || echo "$(curl -s -X POST http://localhost:5000/v1/auth/login -H "Content-Type: application/json" -d '{"username":"testuser","password":"password123"}')"
echo ""

# Test v2 auth endpoint
echo "3. Testing v2 Authentication (Enhanced Format):"
curl -s -X POST http://localhost:5000/v2/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"password123"}' | \
  jq '.' 2>/dev/null || echo "$(curl -s -X POST http://localhost:5000/v2/auth/login -H "Content-Type: application/json" -d '{"username":"testuser","password":"password123"}')"
echo ""

# Test payment endpoint
echo "4. Testing Payment Processing:"
curl -s -X POST http://localhost:5000/v1/payments \
  -H "Content-Type: application/json" \
  -d '{"amount":100.50,"currency":"USD","destinationAccount":"ACC123456"}' | \
  jq '.' 2>/dev/null || echo "$(curl -s -X POST http://localhost:5000/v1/payments -H "Content-Type: application/json" -d '{"amount":100.50,"currency":"USD","destinationAccount":"ACC123456"}')"
echo ""

# Test validation errors
echo "5. Testing Input Validation:"
echo "   Invalid login (empty username):"
curl -s -X POST http://localhost:5000/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"","password":"password123"}' | \
  head -c 200
echo ""

echo "   Invalid payment (negative amount):"
curl -s -X POST http://localhost:5000/v1/payments \
  -H "Content-Type: application/json" \
  -d '{"amount":-10,"currency":"USD","destinationAccount":"ACC123456"}' | \
  head -c 200
echo ""

echo "=== Testing Complete ==="
echo ""
echo "Services:"
echo "  Mock Provider: http://localhost:5001/swagger"
echo "  BFF Gateway: http://localhost:5000/swagger"
