#!/bin/bash

# Start services script for BFF Gateway

echo "Starting Mock Provider on http://localhost:5001..."
cd src/MockProvider
dotnet run --urls "http://localhost:5001" &
PROVIDER_PID=$!

echo "Waiting for provider to start..."
sleep 5

echo "Starting BFF Gateway on http://localhost:5000..."
cd ../BffGateway.WebApi
dotnet run --urls "http://localhost:5000" &
GATEWAY_PID=$!

echo "Waiting for gateway to start..."
sleep 5

echo "Services started!"
echo "Mock Provider: http://localhost:5001/swagger"
echo "BFF Gateway: http://localhost:5000/swagger"
echo ""
echo "Health checks:"
echo "  Live: curl http://localhost:5000/health/live"
echo "  Ready: curl http://localhost:5000/health/ready"
echo ""
echo "To stop services:"
echo "  kill $PROVIDER_PID $GATEWAY_PID"

# Keep script running
wait
