# VRS – Video Renting System
 
## Team
 
| Name            | Role        | Email                     |
|-----------------|-------------|---------------------------|
| Raj Aryan       | Team Leader | RA1506@live.mdx.ac.uk     |
| Muzammil Hassan | Secretary   | MH2049@live.mdx.ac.uk     |
| Ali Alnabhan    | Developer   | AA5715@live.mdx.ac.uk     |
| Subbs           | Developer   | TM1102@live.mdx.ac.uk     |
| Aymen Marzouk   | Tester      | AM3951@live.mdx.ac.uk     |
 
---
 
An indie-first pay-per-title video rental marketplace where customers can rent individual titles and small studios can publish and manage their own catalogs.
 
## Project Structure
 
```
├── backend/                          .NET solution (API + Core + Tests)
│   ├── VideoRentingSystem.Api/       ASP.NET Core Web API
│   ├── VideoRentingSystem.Core/      Domain models, data structures, SQLite repositories
│   ├── VideoRentingSystem.Tests/     MSTest unit and integration tests
│   ├── VideoRentingSystem.App/       WinForms desktop client
│   └── VideoRentingSystem.slnx      Solution file
│
├── frontend/                         React + TypeScript + Vite web client
│   ├── src/App.tsx                   Marketplace UI (browse, rent, studio dashboard)
│   ├── src/Admin.tsx                 Admin panel (user/video/rental management)
│   └── src/index.css                 Global styles and theme
```
 
## Getting Started
 
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
 
| Username   | Password   | Role      |
|------------|------------|-----------|
| admin      | admin      | Admin     |
| publisher1 | publisher1 | Publisher |
| customer1  | customer1  | Customer  |
 
## Features
 
**Marketplace**
- Browse catalog with keyword, genre, and price filters
- Per-title rental with configurable price and rental window
- Publisher studio dashboard for catalog management
 
**Admin Panel** (`#/admin`)
- View and manage all users
- Full video CRUD with publish/draft toggle
- Global rental management with force-return and rent-on-behalf
 
## API Reference
 
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
- `GET /api/rentals/me` — view active rentals
 
### Publisher
- `GET /api/publisher/videos/me` — list own catalog
- `POST /api/publisher/videos` — create a title
- `DELETE /api/publisher/videos/{id}` — delete a title
 
### Admin
- `POST /api/admin/login` — admin-only login
- `GET /api/admin/users` — list all users
- `GET /api/admin/videos` — list all videos
- `POST /api/admin/videos` — create video
- `PUT /api/admin/videos/{id}` — edit video
- `PATCH /api/admin/videos/{id}/publish` — toggle publish status
- `DELETE /api/admin/videos/{id}` — delete video
- `GET /api/admin/rentals` — list all active rentals
- `POST /api/admin/rentals/{videoId}/rent` — rent on behalf of user
- `POST /api/admin/rentals/{videoId}/return` — return on behalf of user
 
## Technical Notes
 
- **Database**: SQLite stored at `%LOCALAPPDATA%\VideoRentingSystem\videos.db` — auto-created on first run
- **Data structures**: Custom AVL tree (title index), hash table (ID lookup), BST (username index), hash maps (keyword search, publisher catalog, user rentals)
- **Auth**: In-memory bearer tokens with 8-hour expiry
- **CORS**: Configured for `localhost:5173` and `127.0.0.1:5173`
 
 
