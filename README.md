# YARP Reverse Proxy Demo

This project demonstrates a YARP (Yet Another Reverse Proxy) setup with a Blazor WebAssembly frontend and an ASP.NET Core Web API backend.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    YARP Proxy (Port 80)                     │
│                 http://localhost:80                          │
└───────────────┬─────────────────────────┬───────────────────┘
                │                         │
                │                         │
    ┌───────────▼──────────┐   ┌─────────▼──────────┐
    │  Blazor WebAssembly  │   │   Web API          │
    │  WebApp              │   │   WebApi           │
    │  Port 5001           │   │   Port 5002        │
    │  webapp.localhost    │   │   api.localhost    │
    └──────────────────────┘   └────────────────────┘
```

## Projects

### 1. **WebApp** - Blazor WebAssembly App
- **Path**: `src/WebApp/`
- **Port**: 5001
- **Proxy Route**: http://webapp.localhost
- Blazor WASM application for the frontend

### 2. **WebApi** - ASP.NET Core Web API
- **Path**: `src/WebApi/`
- **Port**: 5002
- **Proxy Route**: http://api.localhost
- REST API with city data
- Includes Scalar API documentation

### 3. **ReverseProxy** - YARP Reverse Proxy
- **Path**: `src/RevereseProxy/`
- **Port**: 80
- Routes traffic based on hostnames:
  - `webapp.localhost` → `localhost:5001`
  - `api.localhost` → `localhost:5002`

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- macOS, Linux, or Windows

### Running the Applications

#### Option 1: Run All Services Separately (Recommended for Development)

Open **three separate terminals**:

**Terminal 1 - Start Blazor App:**
```bash
cd "src/WebApp"
dotnet run --urls http://localhost:5001
```

**Terminal 2 - Start Web API:**
```bash
cd "src/WebApi"
dotnet run --urls http://localhost:5002
```

**Terminal 3 - Start Proxy (requires sudo for port 80):**
```bash
cd "src/RevereseProxy"
sudo dotnet run
```

> **Note**: If you don't want to use sudo, change the proxy port in `src/RevereseProxy/Properties/launchSettings.json` to `5000` or another port above 1024.

#### Option 2: Using VS Code Launch Configuration

1. Press **F5** or go to **Run and Debug** (Cmd+Shift+D)
2. Select **"Launch All Projects"** from the dropdown
3. Click the green play button

This will build and start WebApp, WebApi, and ReverseProxy automatically.

## Testing

### Direct Access (Without Proxy)

**Blazor App:**
- http://localhost:5001

**Web API:**
- Cities Endpoint: http://localhost:5002/Cities
- Scalar API Docs: http://localhost:5002/scalar/v1

### Through Proxy

**Blazor App:**
- http://webapp.localhost

**Web API:**
- Cities Endpoint: http://api.localhost/Cities
- Scalar API Docs: http://api.localhost/scalar/v1

### Testing with cURL

```bash
# Test API through proxy
curl http://api.localhost/Cities

# Test API directly
curl http://localhost:5002/Cities

# Check proxy health (should get response from backend)
curl -H "Host: api.localhost" http://localhost/Cities
```

### Testing in Browser

1. **Without Proxy**: Navigate to http://localhost:5001 and http://localhost:5002/scalar/v1
2. **With Proxy**: Navigate to http://webapp.localhost and http://api.localhost/scalar/v1

## API Endpoints

### GET /Cities
Returns a list of European cities with population data.

**Response Example:**
```json
[
  {
    "city": "Istanbul",
    "country": "Turkey",
    "population": 15636000
  },
  {
    "city": "Moscow",
    "country": "Russia",
    "population": 13010000
  }
  // ... more cities
]
```

## Configuration Files

### YARP Configuration (`src/RevereseProxy/appsettings.json`)

```json
{
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "api-cluster",
        "Match": {
          "Hosts": ["api.localhost"]
        }
      },
      "webapp-route": {
        "ClusterId": "webapp-cluster",
        "Match": {
          "Hosts": ["webapp.localhost"]
        }
      }
    },
    "Clusters": {
      "api-cluster": {
        "Destinations": {
          "api-destination": {
            "Address": "http://localhost:5002"
          }
        }
      },
      "webapp-cluster": {
        "Destinations": {
          "webapp-destination": {
            "Address": "http://localhost:5001"
          }
        }
      }
    }
  }
}
```

## Troubleshooting

### Port 80 Permission Denied
If you get permission denied on port 80:
```bash
# Option 1: Run with sudo
sudo dotnet run

# Option 2: Change to a higher port (e.g., 5000)
# Edit src\RevereseProxy/Properties/launchSettings.json and change port to 5000
# Then access via http://api.localhost:5000 and http://webapp.localhost:5000
```

### Application Not Starting
Make sure no other services are using the required ports:
```bash
# Check what's using ports
lsof -i :80
lsof -i :5001
lsof -i :5002

# Kill process if needed
kill -9 <PID>
```

### Browser Cannot Resolve *.localhost
Modern browsers should resolve `*.localhost` automatically. If not:
- Try using `127.0.0.1` instead
- Or add entries to `/etc/hosts`:
  ```
  127.0.0.1 api.localhost
  127.0.0.1 webapp.localhost
  ```

## Development

### Building All Projects
```bash
dotnet build yarp-reverse-proxy-sample.slnx
```

### Restore Packages
```bash
dotnet restore
```

### VS Code Tasks
Available tasks (Ctrl+Shift+P → "Tasks: Run Task"):
- `build-WebApp` - Build Blazor app
- `build-WebApi` - Build API
- `build-ReverseProxy` - Build Proxy
- `build-all` - Build all projects

## Tech Stack

- **YARP** 2.2.0 - Reverse Proxy
- **.NET** 10.0
- **Blazor WebAssembly** - Frontend framework
- **ASP.NET Core** - Web API
- **Scalar** - API documentation

## License

This is a demo project for learning purposes.
