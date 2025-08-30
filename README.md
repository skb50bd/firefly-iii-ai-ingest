# Firefly III AI Ingest

A .NET 9.0 web service that uses AI to automatically categorize and create transactions in Firefly III from natural language descriptions.

## Features

- **AI-Powered Transaction Processing**: Automatically analyzes transaction descriptions and creates categorized transactions in Firefly III
- **Multiple AI Providers**: Support for both OpenAI and Ollama (local AI models)
- **Flexible Authentication**: Optional API key or basic authentication
- **Docker Support**: Easy deployment with Docker and Docker Compose
- **Health Checks**: Built-in health monitoring for containerized deployments

## Prerequisites

- Docker and Docker Compose installed on your system
- A running Firefly III instance
- Personal Access Token from Firefly III
- AI provider credentials (OpenAI API key or Ollama with appropriate model)

## Quick Start with Docker

### 1. Clone the Repository

```bash
git clone git@github.com:skb50bd/firefly-iii-ai-ingest.git
cd firefly-iii-ai-ingest
```

### 2. Configure Environment Variables

Copy the example environment file and customize it:

```bash
cp env.example .env
```

Edit the `.env` file with your configuration:

```env
# Firefly III Configuration
FIREFLY__URL=http://your-firefly-instance:8080/api
FIREFLY__PERSONALACCESSTOKEN=your-personal-access-token-here

# AI Provider Configuration
AIPROVIDER=Ollama

# Ollama Configuration
OLLAMA__URL=http://ollama:11434
OLLAMA__MODEL=gemma2:2b

# Authentication (optional)
AUTHENTICATION__ENABLED=false
```

### 3. Start the Services

```bash
docker-compose up -d
```

This will start:
- **firefly-buddy**: The main AI ingest service (port 5000)
- **ollama**: Local AI model service (port 11434)

### 4. Pull AI Model (if using Ollama)

If you're using Ollama, you'll need to pull a model:

```bash
docker exec -it ollama ollama pull gemma2:2b
```

### 5. Test the Service

Send a test transaction:

```bash
curl -X POST http://localhost:5000/txn \
  -H "Content-Type: application/json" \
  -d '{"text": "Bought groceries at Walmart for $45.67"}'
```

## Configuration Options

### Firefly III Settings

| Environment Variable | Description | Default |
|---------------------|-------------|---------|
| `FIREFLY__URL` | Your Firefly III API URL | Required |
| `FIREFLY__PERSONALACCESSTOKEN` | Your Firefly III Personal Access Token | Required |

### AI Provider Settings

#### Using OpenAI

```env
AIPROVIDER=OpenAI
OPENAI__APIKEY=sk-your-openai-api-key-here
OPENAI__MODEL=gpt-4o-mini
```

#### Using Ollama (Local)

```env
AIPROVIDER=Ollama
OLLAMA__URL=http://ollama:11434
OLLAMA__MODEL=gemma2:2b
```

### Authentication Settings

Enable API key authentication:

```env
AUTHENTICATION__ENABLED=true
AUTHENTICATION__APIKEYS=your-api-key-here,another-api-key-here
```

Enable basic authentication:

```env
AUTHENTICATION__ENABLED=true
AUTHENTICATION__BASICAUTH__USERNAME=admin
AUTHENTICATION__BASICAUTH__PASSWORD=password
```

## API Usage

### Endpoint: `POST /txn`

Creates a transaction from natural language description.

**Request Body:**
```json
{
  "text": "Bought groceries at Walmart for $45.67 with Cash"
}
```

**Response:**
```json
{
  "isTransactional": true,
  "isConfident": true,
  "draft": {
    "amount": "45.67",
    "description": "Walmart groceries",
    "category": "Groceries",
    "sourceAccountName": "Cash Wallet",
    "destinationAccountName": "Walmart",
    "tags": ["grocery", "food", "AI"]
  },
  "reason": null
}
```

### Authentication

If authentication is enabled, include your credentials:

**API Key:**
```bash
curl -X POST http://localhost:5000/txn \
  -H "X-API-Key: your-api-key-here" \
  -H "Content-Type: application/json" \
  -d '{"text": "Your transaction description"}'
```

**Basic Auth:**
```bash
curl -X POST http://localhost:5000/txn \
  -u "admin:password" \
  -H "Content-Type: application/json" \
  -d '{"text": "Your transaction description"}'
```

## Docker Commands

### Build and Run

```bash
# Build the image
docker build -t firefly-buddy .

# Run with docker-compose
docker-compose up -d

# View logs
docker-compose logs -f firefly-buddy

# Stop services
docker-compose down
```

### Development

```bash
# Run in development mode
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

# Rebuild after code changes
docker-compose build --no-cache firefly-buddy
```

### Ollama Management

```bash
# Pull a new model
docker exec -it ollama ollama pull llama3.1:8b

# List available models
docker exec -it ollama ollama list

# Remove a model
docker exec -it ollama ollama rm gemma2:2b
```

## Health Checks

The service includes health checks for both containers:

- **firefly-buddy**: Checks if the web service is responding
- **ollama**: Checks if the AI service is available

Monitor health status:

```bash
docker-compose ps
```

## Troubleshooting

### Common Issues

1. **Firefly III Connection Failed**
   - Verify `FIREFLY__URL` is correct and accessible
   - Check your Personal Access Token is valid
   - Ensure Firefly III is running and accessible

2. **AI Model Not Found**
   - Pull the required model: `docker exec -it ollama ollama pull <model-name>`
   - Check model name in configuration matches available models

3. **Service Won't Start**
   - Check logs: `docker-compose logs firefly-buddy`
   - Verify environment variables are set correctly
   - Ensure ports are not already in use

4. **Authentication Errors**
   - Verify API keys or credentials are correct
   - Check if authentication is properly enabled/disabled

### Logs and Debugging

```bash
# View service logs
docker-compose logs -f firefly-buddy

# View Ollama logs
docker-compose logs -f ollama

# Access container shell
docker exec -it firefly-buddy /bin/bash
```

## Security Considerations

1. **Environment Variables**: Never commit `.env` files with real credentials
2. **Network Security**: Consider using Docker networks to isolate services
3. **Authentication**: Enable authentication in production environments
4. **API Keys**: Use strong, unique API keys and rotate them regularly

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

To be announced.
