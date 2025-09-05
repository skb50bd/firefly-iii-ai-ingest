# Use the official .NET 9.0 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install PostgreSQL client for database operations
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

# Use the official .NET 9.0 SDK as the build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project files
COPY ["Brotal.FireflyBuddy/Brotal.FireflyBuddy.csproj", "Brotal.FireflyBuddy/"]

# Restore dependencies
RUN dotnet restore "Brotal.FireflyBuddy/Brotal.FireflyBuddy.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/Brotal.FireflyBuddy"
RUN dotnet build "Brotal.FireflyBuddy.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Brotal.FireflyBuddy.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy database initialization script
COPY init-db.sh /app/init-db.sh
RUN chmod +x /app/init-db.sh

# Create a non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Use a startup script that initializes the database and then starts the app
COPY --chown=appuser:appuser <<EOF /app/startup.sh
#!/bin/bash
echo "Starting Firefly Buddy..."
/app/init-db.sh
echo "Starting application..."
exec dotnet Brotal.FireflyBuddy.dll
EOF

RUN chmod +x /app/startup.sh

ENTRYPOINT ["/app/startup.sh"]
