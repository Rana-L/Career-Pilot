# CareerPilot

An AI-powered career management platform to help job seekers track applications, analyse their CV against job descriptions, and manage their job search in one place.

**Live demo:** [career-pilot-gamma-sable.vercel.app](https://career-pilot-gamma-sable.vercel.app)
*(Backend runs on a free Render instance — the first request after a period of inactivity may take 30-50 seconds while it spins back up.)*

## Vision

Most graduate job seekers manage their search across scattered spreadsheets, browser tabs, and sticky notes. CareerPilot centralises that — track every application, get AI-driven feedback on how well your CV matches a role, and see your job search progress at a glance.

Built as a full-stack portfolio project demonstrating production-style engineering: authentication, a relational data model, cloud file storage, a real AI integration, and a live deployment.

## Features

**Accounts & security**
- Registration and login with JWT authentication (no server-side session state)
- Server-side validation: real email format, strong-password policy (length + character classes)
- Case-insensitive email, constant-time login response to prevent account enumeration
- Per-IP rate limiting on the auth endpoints

**Application tracking**
- Add, edit, and track applications by status (Wishlist, Applied, Assessment, Interview, Offer, Rejected)
- Paste a job posting and have AI fill in the company, title, and description
- Dashboard with application counts by status

**CVs & AI**
- CV upload and secure storage (private S3 bucket, time-limited signed download links)
- CVs accepted as PDF, DOCX, or plain text — real text extraction from each
- AI CV-to-job-description **match scoring** with missing skills identified, and full analysis history
- AI **CV rewrite** tailored to a job — rendered as a live document preview, downloadable as PDF or DOCX
- AI **cover letter** generation from a CV + job — same preview and PDF/DOCX download
- Upload validation (size/type) and a cooldown on repeat analyses to keep AI usage predictable

**Engineering**
- Responsive, modern UI with a shared navigation shell
- 42 backend tests (xUnit) covering auth, IDOR protection, validation, and document generation, run in CI
- GitHub Actions CI builds/lints/type-checks and tests both apps on every push

**Possible future work**
- Interview preparation questions with AI feedback
- Deeper analytics (response times, most successful CV version, etc.)
- URL-based job posting import (currently paste-text only, since major job boards block server-side scraping)

## Architecture

```
Next.js frontend  →  ASP.NET Core API  →  PostgreSQL
                            │
                            ├──  Amazon S3   (CV file storage)
                            └──  OpenAI API  (match scoring, CV rewrite, cover letter, job parsing)
```

- REST API built with ASP.NET Core, EF Core for data access
- JWT-based authentication, no server-side session state
- PostgreSQL for relational data (users, applications, CVs, analyses)
- CV files stored in a private S3 bucket; only the object key is persisted in the database, with signed URLs generated on demand for downloads
- AI-generated documents are produced as Markdown, then rendered to PDF (QuestPDF) and DOCX (OpenXML) server-side
- CI (GitHub Actions) builds/lints/type-checks and tests both apps on every push

## Tech Stack

**Backend:** ASP.NET Core, C#, Entity Framework Core, PostgreSQL, PdfPig (PDF text extraction), DocumentFormat.OpenXml (DOCX read/write), QuestPDF (PDF generation), Markdig (Markdown parsing)
**Frontend:** Next.js (App Router), React, TypeScript, Tailwind CSS, react-markdown
**Auth:** JWT with BCrypt hashing, strong-password policy, per-IP rate limiting
**Cloud:** AWS S3 (CV storage), OpenAI API (all AI features)
**Deployment:** Vercel (frontend), Render (backend + PostgreSQL, Docker-based)
**Testing / DevOps:** xUnit + Moq + EF Core InMemory, Docker (local Postgres), GitHub Actions CI

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
- [x] AI CV rewrite + cover letter, with PDF/DOCX export
- [x] Paste-a-job-posting auto-fill
- [x] Auth hardening (password policy, rate limiting, anti-enumeration)
- [x] Backend test suite in CI

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
