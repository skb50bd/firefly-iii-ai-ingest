# Firefly Buddy - AI-Powered Firefly-III Companion

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/skb50bd/firefly-iii-ai-ingest/publish.yml)
[![License](https://img.shields.io/github/license/skb50bd/firefly-iii-ai-ingest)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)

Firefly Buddy is a sophisticated companion application for Firefly-III that uses AI to analyze financial messages, receipts, and transaction descriptions, then automatically creates transaction drafts for your review and approval.

## 🚀 Features

- **AI-Powered Analysis**: Uses Ollama or OpenAI to analyze financial messages and identify transactions
- **Smart Draft Creation**: Automatically creates transaction drafts with proper categorization
- **Web Interface**: Beautiful, modern web UI for managing messages and drafts
- **Background Processing**: TickerQ-powered job scheduling for reliable message processing
- **PostgreSQL Persistence**: Robust data storage with EF Core
- **Real-time Dashboard**: Monitor job processing and system health
- **Docker Ready**: Easy deployment with Docker Compose

## 🏗️ Architecture

- **Backend**: ASP.NET Core 9.0 with MVC
- **Database**: PostgreSQL with Entity Framework Core
- **Job Scheduling**: TickerQ for background processing
- **AI Integration**: Ollama (local) or OpenAI (cloud) support
- **UI**: Bootstrap 5 with modern responsive design
- **Containerization**: Docker with multi-stage builds

## 📋 Prerequisites

- Docker and Docker Compose
- Firefly-III instance running
- Ollama (for local AI) or OpenAI API key

## 🚀 Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd firefly-iii-ai-ingest
   ```

2. **Configure environment**
   ```bash
   cp env.example .env
   # Edit .env with your Firefly-III settings
   ```

3. **Start the application**
   ```bash
   docker-compose up -d
   ```

4. **Access the application**
   - Web UI: http://localhost:5000
   - Job Dashboard: http://localhost:5000/tickerq-dashboard
   - Health Check: http://localhost:5000/health

## ⚙️ Configuration

### Environment Variables

Create a `.env` file with the following variables:

```env
# Firefly-III Configuration
FIREFLY_URL=http://your-firefly-instance:8080/api
FIREFLY_PERSONAL_ACCESS_TOKEN=your-pat-token

# AI Provider (Ollama or OpenAI)
AI_PROVIDER=Ollama
OLLAMA_URL=http://ollama:11434
OLLAMA_MODEL=gemma3n:e4b

# Or for OpenAI
# AI_PROVIDER=OpenAI
# OPENAI_API_KEY=your-openai-api-key
# OPENAI_MODEL=gpt-4

# Authentication (Optional)
AUTHENTICATION_ENABLED=false
API_KEYS=your-api-key-here,another-key
```

### Database Configuration

The application automatically creates and migrates the PostgreSQL database on startup. No manual database setup required.

## 📱 Usage

### 1. Adding Messages

- **Web UI**: Navigate to Messages → Add New Message
- **API**: POST to `/api/messages` with JSON payload:
  ```json
  {
    "text": "Paid $25.50 for groceries at Walmart",
    "source": "SMS",
    "externalId": "optional-external-id"
  }
  ```

### 2. Processing Flow

1. **Message Ingestion**: Messages are queued for processing
2. **AI Analysis**: Background jobs analyze messages every 5 minutes
3. **Draft Creation**: Transactional messages create drafts automatically
4. **Review & Edit**: Use the web UI to review and edit drafts
5. **Submission**: Approved drafts are sent to Firefly-III

### 3. Managing Drafts

- View all drafts in the Drafts section
- Edit draft details before submission
- Submit drafts directly to Firefly-III
- Track submission status and errors

## 🔧 API Endpoints

### Messages
- `GET /api/messages` - List all messages
- `GET /api/messages/{id}` - Get specific message
- `POST /api/messages` - Create new message

### Drafts
- `GET /api/drafts` - List all drafts
- `PUT /api/drafts/{id}` - Update draft
- `POST /api/drafts/{id}/submit` - Submit draft to Firefly-III

### Legacy
- `POST /txn` - Legacy endpoint for backward compatibility

## 🎯 Background Jobs

The application uses TickerQ for reliable background processing:

- **ProcessPendingMessages**: Runs every 5 minutes to analyze new messages
- **SubmitReadyDrafts**: Runs every 10 minutes to submit approved drafts

Monitor job status at: http://localhost:5000/tickerq-dashboard

## 🛠️ Development

### Local Development

1. **Install .NET 9.0 SDK**
2. **Start PostgreSQL and Ollama**:
   ```bash
   docker-compose up buddy-db ollama -d
   ```
3. **Run the application**:
   ```bash
   cd Brotal.FireflyBuddy
   dotnet run
   ```

### Database Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### Building Docker Image

```bash
docker build -t firefly-buddy .
```

## 📊 Monitoring

- **Health Check**: `/health` endpoint for container health
- **Job Dashboard**: `/tickerq-dashboard` for background job monitoring
- **Application Logs**: Available in Docker logs

## 🔒 Security

- Optional API key authentication
- Basic authentication for job dashboard
- Non-root container execution
- Input validation and sanitization

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

## 🙏 Acknowledgments

- [Firefly-III](https://github.com/firefly-iii/firefly-iii) - Personal finance manager
- [TickerQ](https://github.com/Arcenox-co/TickerQ) - Background job scheduler
- [Ollama](https://ollama.ai/) - Local AI model runner
- [Bootstrap](https://getbootstrap.com/) - UI framework

## 📞 Support

For issues and questions:
- Create an issue on GitHub
- Check the documentation
- Review the logs for troubleshooting

---

**Firefly Buddy** - Making personal finance management smarter with AI! 🚀