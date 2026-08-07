# CareerPilot

An AI-powered career management platform to help job seekers track applications, analyse their CV against job descriptions, and manage their job search in one place.

## Vision

Most graduate job seekers manage their search across scattered spreadsheets, browser tabs, and sticky notes. CareerPilot centralises that — track every application, get AI-driven feedback on how well your CV matches a role, and see your job search progress at a glance.

Built as a full-stack portfolio project demonstrating production-style engineering: authentication, a relational data model, cloud file storage, and a real AI integration.

##  Features

**MVP (current focus)**
- User registration and login (JWT authentication)
- Job application tracker — add, edit, and track applications by status (Wishlist, Applied, Assessment, Interview, Offer, Rejected)
- Dashboard with application counts by status
- CV upload and storage (Amazon S3)
- AI-powered CV-to-job-description match scoring, with missing skills identified

**Planned / stretch**
- AI-generated cover letters
- Interview preparation questions with AI feedback
- Deeper analytics (response times, most successful CV version, etc.)
- Email notifications for upcoming interviews

## Architecture

Frontend (Next.js) → Backend API (ASP.NET Core) → PostgreSQL
↓
Amazon S3 (CV storage)
↓
OpenAI API (CV analysis)

- REST API built with ASP.NET Core, EF Core for data access
- JWT-based authentication, no server-side session state
- PostgreSQL for relational data (users, applications, CVs, analyses)
- File uploads stored in S3, only the URL persisted in the database

## Tech Stack

**Backend:** ASP.NET Core, C#, Entity Framework Core, PostgreSQL
**Frontend:** Next.js, React, TypeScript, Tailwind CSS
**Auth:** JWT (custom implementation with BCrypt password hashing)
**Cloud:** AWS (S3 for file storage)
**AI:** OpenAI API
**DevOps:** Docker (local Postgres), GitHub Actions (CI, planned)

## 🗺️ Roadmap

- [x] Project scaffolding (backend + repo structure)
- [x] Database schema (Users, Applications, CVs, CvAnalyses)
- [x] JWT authentication (register/login)
- [ ] Application CRUD endpoints
- [ ] Dashboard summary endpoint
- [ ] Frontend: auth pages + protected routes
- [ ] Frontend: application tracker UI
- [ ] S3 CV upload
- [ ] OpenAI CV-match analysis endpoint
- [ ] Deployment (frontend + backend live)

## Screenshots

_Coming soon — once the frontend is in place._

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker (for local PostgreSQL)
- Node.js (for the frontend, once added)

### Backend setup
```bash
cd backend/CareerPilot.Api
docker compose up -d          # starts local Postgres
dotnet ef database update     # applies migrations
dotnet run
```

API will be available at `http://localhost:5242`, with Swagger docs at `/swagger`.
