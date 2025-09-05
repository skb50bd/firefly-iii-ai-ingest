#!/bin/bash

# Database initialization script for Firefly Buddy
echo "Initializing Firefly Buddy database..."

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
until pg_isready -h buddy-db -p 5432 -U postgres; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 2
done

echo "PostgreSQL is ready!"

# Create database if it doesn't exist
echo "Creating database if it doesn't exist..."
psql -h buddy-db -U postgres -c "CREATE DATABASE firefly_buddy;" 2>/dev/null || echo "Database already exists"

# Run EF Core migrations
echo "Running database migrations..."
dotnet ef database update --connection "Host=buddy-db;Database=firefly_buddy;Username=postgres;Password=password"

echo "Database initialization complete!"
