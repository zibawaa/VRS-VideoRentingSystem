# Video Renting System (CST2550)

An indie-first pay-per-title video rental marketplace where customers rent individual titles and small studios can publish their own catalogs.

## Project Structure

```
├── backend/                          .NET solution (API + Core + Tests)
│   ├── VideoRentingSystem.Api/       ASP.NET Core Web API (primary runtime)
│   ├── VideoRentingSystem.Core/      Domain models, custom data structures, SQLite repos
│   ├── VideoRentingSystem.Tests/     MSTest unit & integration tests
│   ├── VideoRentingSystem.App/       Legacy WinForms prototype (deprecated)
│   └── VideoRentingSystem.slnx       Solution file
│
├── frontend/                         React + TypeScript + Vite web client
│   ├── src/App.tsx                   Marketplace UI (browse, rent, studio dashboard)
│   ├── src/Admin.tsx                 Admin panel (user/video/rental management)
│   └── src/index.css                 Global design tokens and theme
```

## Quick Start

### 1. Build the backend

```bash
dotnet build backend/VideoRentingSystem.slnx
```

### 2. Run the API

```bash
dotnet run --project backend/VideoRentingSystem.Api --urls "http://localhost:5265"
```

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173` for the marketplace, or `http://localhost:5173/#/admin` for the admin panel.

### 4. Run tests

```bash
dotnet test backend/VideoRentingSystem.Tests
```

## Demo Accounts

| Username    | Password    | Role      |
|-------------|-------------|-----------|
| admin       | admin       | Admin     |
| 123         | 123         | Admin     |
| publisher1  | publisher1  | Publisher |
| customer1   | customer1   | Customer  |

## Features

**Marketplace (Customer & Publisher)**
- Browse catalog with keyword, genre, and price filters
- Per-title rental with configurable price and time window
- Publisher studio dashboard for catalog management
- AI assistant chatbot (search, recommend, rent, return via natural language)

**Admin Panel** (`#/admin`)
- View all users with active session tokens
- Full video CRUD with inline editing and publish/draft toggle
- Global rental view with force-return and rent-on-behalf
- Dedicated admin login (rejects non-admin accounts)

## API Endpoints

### Auth
- `POST /api/auth/register` — register as Customer or Publisher
- `POST /api/auth/login` — login and receive bearer token
- `POST /api/auth/logout` — revoke session

### Videos
- `GET /api/videos?keyword=&genre=&maxPrice=` — browse published catalog
- `GET /api/videos/{id}` — get video details

### Rentals
- `POST /api/rentals/{videoId}/rent` — rent a title
- `POST /api/rentals/{videoId}/return` — return a title
- `GET /api/rentals/me` — list active rentals

### Publisher
- `GET /api/publisher/videos/me` — list own catalog
- `POST /api/publisher/videos` — create a title
- `DELETE /api/publisher/videos/{id}` — delete a title

### Admin
- `POST /api/admin/login` — admin-only login
- `GET /api/admin/users` — list all users with session tokens
- `GET /api/admin/videos` — list all videos (published + unpublished)
- `POST /api/admin/videos` — create video
- `PUT /api/admin/videos/{id}` — edit video
- `PATCH /api/admin/videos/{id}/publish` — toggle publish status
- `DELETE /api/admin/videos/{id}` — delete video
- `GET /api/admin/rentals` — list all active rentals
- `POST /api/admin/rentals/{videoId}/rent` — rent on behalf of user
- `POST /api/admin/rentals/{videoId}/return` — return on behalf of user

### AI Agent
- `POST /api/agent/chat` — natural language assistant
- `GET /api/agent/capabilities` — list agent tools

## Technical Notes

- **Database**: SQLite file at `%LOCALAPPDATA%\VideoRentingSystem\videos.db` (auto-created on first run)
- **Custom data structures**: AVL tree (title index), hash table (ID lookup), BST (username index), hash maps (keyword search, publisher catalog, user rentals)
- **Auth**: Lightweight in-memory bearer tokens with 8-hour expiry
- **CORS**: Configured for `localhost:5173` and `127.0.0.1:5173` during development
