# CareerPilot

An AI-powered career management platform to help job seekers track applications, analyse their CV against job descriptions, and manage their job search in one place.

**Live demo:** [career-pilot-gamma-sable.vercel.app](https://career-pilot-gamma-sable.vercel.app)
*(Backend runs on a free Render instance — the first request after a period of inactivity may take 30-50 seconds while it spins back up.)*

## Vision

Most graduate job seekers manage their search across scattered spreadsheets, browser tabs, and sticky notes. CareerPilot centralises that — track every application, get AI-driven feedback on how well your CV matches a role, and see your job search progress at a glance.

Built as a full-stack portfolio project demonstrating production-style engineering: authentication, a relational data model, cloud file storage, a real AI integration, and a live deployment.

## Features

- User registration and login (JWT authentication)
- Job application tracker — add, edit, and track applications by status (Wishlist, Applied, Assessment, Interview, Offer, Rejected)
- Dashboard with application counts by status
- CV upload and secure storage (Amazon S3, private bucket with time-limited signed download links)
- CVs accepted as PDF, DOCX, or plain text
- AI-powered CV-to-job-description match scoring (OpenAI), with missing skills identified
- Full analysis history — every past match result is saved and viewable, not just the latest
- Upload validation (file size/type limits) and a cooldown on repeat analyses, to keep usage predictable
- Responsive, modern UI with shared navigation across the app

**Planned / stretch**
- AI-assisted CV rewriting to better match a specific job's keywords/ATS filters
- AI-generated cover letters
- Interview preparation questions with AI feedback
- Deeper analytics (response times, most successful CV version, etc.)
- Automated backend test suite (xUnit, mocked AWS/OpenAI dependencies)

## Architecture

```
Next.js frontend  →  ASP.NET Core API  →  PostgreSQL
                            │
                            ├──  Amazon S3        (CV storage)
                            └──  OpenAI API        (CV match analysis)
```

- REST API built with ASP.NET Core, EF Core for data access
- JWT-based authentication, no server-side session state
- PostgreSQL for relational data (users, applications, CVs, analyses)
- CV files stored in a private S3 bucket; only the object key is persisted in the database, with signed URLs generated on demand for downloads
- CI (GitHub Actions) builds/lints/type-checks both the backend and frontend on every push

## Tech Stack

**Backend:** ASP.NET Core, C#, Entity Framework Core, PostgreSQL, PdfPig (PDF text extraction), DocumentFormat.OpenXml (DOCX text extraction)
**Frontend:** Next.js (App Router), React, TypeScript, Tailwind CSS
**Auth:** JWT (custom implementation with BCrypt password hashing)
**Cloud:** AWS S3 (CV storage), OpenAI API (CV match analysis)
**Deployment:** Vercel (frontend), Render (backend + PostgreSQL, Docker-based)
**DevOps:** Docker (local Postgres), GitHub Actions (CI for backend and frontend)

## Roadmap

- [x] Project scaffolding (backend + repo structure)
- [x] Database schema (Users, Applications, CVs, CvAnalyses)
- [x] JWT authentication (register/login)
- [x] Application CRUD endpoints
- [x] Dashboard summary endpoint
- [x] Frontend: auth pages + protected routes
- [x] Frontend: application tracker UI
- [x] S3 CV upload
- [x] OpenAI CV-match analysis endpoint
- [x] Deployment (frontend + backend live)

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker (for local PostgreSQL)
- Node.js 20+ (for the frontend)
- An AWS account (S3 bucket + IAM user scoped to it) and an OpenAI API key, if you want CV upload/analysis to work locally

### Backend setup
```bash
cd backend/CareerPilot.Api
docker compose -f ../../docker/docker-compose.yml up -d   # starts local Postgres
dotnet ef database update                                  # applies migrations
dotnet user-secrets init
dotnet user-secrets set "Aws:AccessKeyId" "..."
dotnet user-secrets set "Aws:SecretAccessKey" "..."
dotnet user-secrets set "Aws:BucketName" "..."
dotnet user-secrets set "Aws:Region" "..."
dotnet user-secrets set "OpenAI:ApiKey" "..."
dotnet run --launch-profile http
```

API available at `http://localhost:5242`.

### Frontend setup
```bash
cd frontend
npm install
echo "NEXT_PUBLIC_API_URL=http://localhost:5242" > .env.local
npm run dev
```

App available at `http://localhost:3000`.

## Screenshots

_Coming soon._
