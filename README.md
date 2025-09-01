# Firefly III AI Ingest

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/skb50bd/firefly-iii-ai-ingest/publish.yml)
[![License](https://img.shields.io/github/license/skb50bd/firefly-iii-ai-ingest)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)

A powerful .NET 9.0 web service that leverages AI to automatically categorize and create transactions in Firefly III from natural language descriptions. Transform your financial data entry with intelligent automation.

## 🚀 Features

- **🤖 AI-Powered Transaction Processing**: Automatically analyzes transaction descriptions and creates categorized transactions in Firefly III
- **🔄 Multiple AI Providers**: Support for both OpenAI (cloud) and Ollama (local AI models)
- **🔐 Flexible Authentication**: Optional API key or basic authentication for secure access
- **🐳 Docker Support**: Easy deployment with Docker and Docker Compose
- **💚 Health Checks**: Built-in health monitoring for containerized deployments
- **📊 Comprehensive Logging**: Detailed logging for debugging and monitoring
- **⚡ High Performance**: Optimized for quick transaction processing
- **🛡️ Security First**: Built with security best practices

## 📋 Prerequisites

- **Docker** and **Docker Compose** installed on your system
- A running **Firefly III** instance
- **Personal Access Token** from Firefly III
- **AI provider credentials**:
  - OpenAI API key (for cloud AI)
  - OR Ollama with appropriate model (for local AI)

## 🏃‍♂️ Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/skb50bd/firefly-iii-ai-ingest.git
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
OLLAMA__MODEL=gemma3n:e4b

# Authentication (optional)
AUTHENTICATION__ENABLED=false
```

### 3. Start the Services

```bash
docker compose up -d
```

This will start:
- **firefly-buddy**: The main AI ingest service (port 5000)
- **ollama**: Local AI model service (port 11434)

### 4. Pull AI Model (if using Ollama)

If you're using Ollama, pull a model:

```bash
docker exec -it ollama ollama pull gemma3n:e4b
```

### 5. Test the Service

Send a test transaction:

```bash
curl -X POST http://localhost:5000/txn \
  -H "Content-Type: application/json" \
  -d '{"text": "Bought groceries at Walmart for $45.67 with Cash"}'
```

## ⚙️ Configuration

### Firefly III Settings

| Environment Variable | Description | Default | Required |
|---------------------|-------------|---------|----------|
| `FIREFLY__URL` | Your Firefly III API URL | - | ✅ |
| `FIREFLY__PERSONALACCESSTOKEN` | Your Firefly III Personal Access Token | - | ✅ |

### AI Provider Settings

#### Using OpenAI (Cloud)

```env
AIPROVIDER=OpenAI
OPENAI__APIKEY=sk-your-openai-api-key-here
OPENAI__MODEL=gpt-4o-mini
```

#### Using Ollama (Local)

```env
AIPROVIDER=Ollama
OLLAMA__URL=http://ollama:11434
OLLAMA__MODEL=gemma3n:e4b
```

**Recommended Ollama Models:**
- `gemma3n:e4b` - Fast and efficient
- `llama3.2:3b` - Good balance of speed and accuracy
- `mistral:7b` - High accuracy, slower processing

### Authentication Settings

#### API Key Authentication

```env
AUTHENTICATION__ENABLED=true
AUTHENTICATION__APIKEYS__0=your-api-key-here
AUTHENTICATION__APIKEYS__1=another-api-key-here
```

#### Basic Authentication

```env
AUTHENTICATION__ENABLED=true
AUTHENTICATION__BASICAUTH__USERNAME=admin
AUTHENTICATION__BASICAUTH__PASSWORD=secure-password
```

### Logging Configuration

```env
LOGGING__LOGLEVEL__DEFAULT=Information
LOGGING__LOGLEVEL__MICROSOFTASPNETCORE=Information
```

## 🔌 API Reference

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
    "currencyCode": "USD",
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

### Health Check: `GET /health`

```bash
curl http://localhost:5000/health
```

## 🛡️ Reverse Proxy Setup

### Using Nginx

Create an Nginx configuration file:

```nginx
server {
    listen 80;
    server_name your-domain.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;

    # SSL Configuration
    ssl_certificate /path/to/your/certificate.crt;
    ssl_certificate_key /path/to/your/private.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    # Security Headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Rate Limiting
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
    limit_req zone=api burst=20 nodelay;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Timeouts
        proxy_connect_timeout 30s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;

        # Buffer settings
        proxy_buffering on;
        proxy_buffer_size 4k;
        proxy_buffers 8 4k;
    }
}
```

### Using Traefik

Add labels to your `compose.yml`:

```yaml
services:
  firefly-buddy:
    # ... existing configuration ...
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.firefly-buddy.rule=Host(`your-domain.com`)"
      - "traefik.http.routers.firefly-buddy.tls=true"
      - "traefik.http.routers.firefly-buddy.tls.certresolver=letsencrypt"
      - "traefik.http.services.firefly-buddy.loadbalancer.server.port=80"
      - "traefik.http.middlewares.firefly-buddy-ratelimit.ratelimit.average=10"
      - "traefik.http.middlewares.firefly-buddy-ratelimit.ratelimit.burst=20"
      - "traefik.http.routers.firefly-buddy.middlewares=firefly-buddy-ratelimit"
```

### Using Caddy

Create a `Caddyfile`:

```
your-domain.com {
    reverse_proxy localhost:5000 {
        header_up X-Real-IP {remote_host}
        header_up X-Forwarded-For {remote_host}
        header_up X-Forwarded-Proto {scheme}
    }

    # Rate limiting
    rate_limit {
        zone api
        rate 10r/s
        burst 20
    }

    # Security headers
    header {
        X-Frame-Options DENY
        X-Content-Type-Options nosniff
        X-XSS-Protection "1; mode=block"
        Strict-Transport-Security "max-age=31536000; includeSubDomains"
    }
}
```

## 🐳 Docker Management

### Build and Run

```bash
# Build the image
docker build -t firefly-buddy .

# Run with docker-compose
docker compose up -d

# View logs
docker compose logs -f firefly-buddy

# Stop services
docker compose down

# Rebuild and restart
docker compose up -d --build
```

### Ollama Management

```bash
# Pull a new model
docker exec -it ollama ollama pull gemma3n:e4b

# List available models
docker exec -it ollama ollama list

# Remove a model
docker exec -it ollama ollama rm gemma3n:e4b

# Check model status
docker exec -it ollama ollama show gemma3n:e4b
```

### Health Monitoring

```bash
# Check service status
docker compose ps

# Monitor health checks
docker compose logs --tail=50 firefly-buddy | grep health

# Check resource usage
docker stats firefly-buddy ollama
```

## 🔧 Troubleshooting

### Common Issues

#### 1. Firefly III Connection Failed

**Symptoms:**
- Service fails to start
- "Firefly configuration is missing" error
- Connection timeout errors

**Solutions:**
```bash
# Verify Firefly III is accessible
curl -H "Authorization: Bearer YOUR_TOKEN" http://your-firefly-instance:8080/api/v1/about

# Check environment variables
docker exec firefly-buddy env | grep FIREFLY

# Test network connectivity
docker exec firefly-buddy ping your-firefly-instance
```

#### 2. AI Model Not Found

**Symptoms:**
- "Model not found" errors
- AI service unavailable
- Ollama connection failures

**Solutions:**
```bash
# Check if model is downloaded
docker exec -it ollama ollama list

# Pull the required model
docker exec -it ollama ollama pull gemma3n:e4b

# Verify Ollama is running
docker exec -it ollama ollama ps

# Check model configuration
docker exec firefly-buddy env | grep OLLAMA
```

#### 3. Service Won't Start

**Symptoms:**
- Container exits immediately
- Port already in use
- Configuration errors

**Solutions:**
```bash
# Check detailed logs
docker compose logs firefly-buddy

# Verify port availability
netstat -tulpn | grep :5000

# Check container status
docker ps -a | grep firefly-buddy

# Restart with fresh state
docker compose down
docker compose up -d --force-recreate
```

#### 4. Authentication Errors

**Symptoms:**
- 401 Unauthorized responses
- Authentication middleware errors
- API key validation failures

**Solutions:**
```bash
# Verify authentication configuration
docker exec firefly-buddy env | grep AUTHENTICATION

# Test with correct credentials
curl -H "X-API-Key: your-api-key" http://localhost:5000/health

# Check authentication middleware logs
docker compose logs firefly-buddy | grep -i auth
```

#### 5. Performance Issues

**Symptoms:**
- Slow response times
- High memory usage
- Timeout errors

**Solutions:**
```bash
# Monitor resource usage
docker stats firefly-buddy ollama

# Check AI model performance
docker exec -it ollama ollama show gemma3n:e4b

# Optimize model settings
# Use smaller models for better performance
docker exec -it ollama ollama pull gemma3n:e4b
```

### Debugging Commands

```bash
# View real-time logs
docker compose logs -f firefly-buddy

# Access container shell
docker exec -it firefly-buddy /bin/bash

# Check configuration
docker exec firefly-buddy cat /app/appsettings.json

# Test API endpoints
curl -v http://localhost:5000/health

# Monitor network traffic
docker exec firefly-buddy netstat -tulpn
```

### Log Analysis

```bash
# Search for errors
docker compose logs firefly-buddy | grep -i error

# Search for warnings
docker compose logs firefly-buddy | grep -i warn

# Monitor transaction processing
docker compose logs firefly-buddy | grep -i transaction

# Check AI processing
docker compose logs firefly-buddy | grep -i ai
```

## 🔒 Security Best Practices

### 1. Environment Security
- Never commit `.env` files with real credentials
- Use Docker secrets for sensitive data in production
- Rotate API keys and tokens regularly

### 2. Network Security
- Use Docker networks to isolate services
- Implement proper firewall rules
- Use HTTPS/TLS for all external communications

### 3. Authentication
- Enable authentication in production environments
- Use strong, unique API keys
- Implement rate limiting
- Consider IP whitelisting for sensitive deployments

### 4. Container Security
- Keep base images updated
- Run containers as non-root users
- Implement resource limits
- Use security scanning tools

### 5. Monitoring and Logging
- Monitor access logs for suspicious activity
- Implement alerting for failed authentication attempts
- Regular security audits
- Keep logs for compliance requirements

## 📊 Monitoring and Metrics

### Health Checks

```bash
# Check service health
curl http://localhost:5000/health

# Monitor container health
docker compose ps

# Check resource usage
docker stats --no-stream firefly-buddy ollama
```

### Log Monitoring

```bash
# Follow logs in real-time
docker compose logs -f firefly-buddy

# Search for specific events
docker compose logs firefly-buddy | grep "Created transaction"

# Monitor error rates
docker compose logs firefly-buddy | grep -c "ERROR"
```

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Setup

```bash
# Clone the repository
git clone https://github.com/skb50bd/firefly-iii-ai-ingest.git
cd firefly-iii-ai-ingest

# Open in your preferred IDE
code .

# Run locally with Docker
docker compose up -d

# Run tests
dotnet test
```

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

## 🆘 Support

### Getting Help

- **Issues**: Create an issue on GitHub for bugs or feature requests
- **Discussions**: Use GitHub Discussions for questions and general help
- **Documentation**: Check this README and the project wiki

### Community

- **GitHub**: [Project Repository](https://github.com/skb50bd/firefly-iii-ai-ingest)
- **Firefly III**: [Official Documentation](https://docs.firefly-iii.org/)

---

**Made with ❤️ for the Firefly III community**

