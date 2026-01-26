# AskData: Natural Language Interface for Live Databases

AskData is a full-stack internal analytics platform that enables users to query live operational data using natural language instead of SQL. It is designed to reduce the reliance on technical teams for everyday reporting while preserving the reliability, governance, and security required in enterprise systems. The application is built using an Angular single-page frontend, an ASP.NET Core Web API backend, and a serverless Neon PostgreSQL database, forming a modern, cloud-native architecture suitable for internal tools.

A central challenge in building AskData was safely translating natural language questions into executable SQL using an LLM. To address this, the system dynamically extracts the live database schema and injects it into the prompt context, enforces read-only constraints, and applies SQL validation and rewriting before execution. This ensures that AI-generated queries remain accurate, secure, and aligned with the underlying data model, making the system reliable enough for real operational use rather than a simple prototype.

As part of authentication and access control, there was an enterprise solution. Instead of using an authentication framework, the application makes use of a package called Neon Auth, which contains support for email and password authentication, JWT support, along with automatic syncing of users in the database. This way, there will always be an authentic user attached to every single query, which is essential in internal applications that run against company data.

To ensure that AskData becomes production-ready, the project focuses on observability and maintainability through full query auditing, execution, and review, in addition to utilizing CI/CD and container deployment. The project delivers a scalable, transparent, and secure AI-assisted data access solution that mirrors the current approach to developing internal analytics solutions and demonstrates the practical and business-oriented usage of AI in software systems.

## Tech Stack

### Frontend
- Angular (Standalone components)
- TypeScript
- Angular Signals
- CSS Grid / Tailwind-style layout
- GitHub Pages (deployment)

### Backend
- ASP.NET Core Web API
- C#
- JWT-based authentication
- LLM integration for SQL generation
- RESTful architecture

### Database & Authentication
- Neon PostgreSQL (serverless, branch-aware)
- Neon Auth (email/password authentication)
- Automatic user synchronization into Postgres

### AI & Data Layer
- LLM-based natural language to SQL translation
- Schema-aware prompting
- SQL rewriting and validation
- Read-only query enforcement

### DevOps
- GitHub Codespaces
- CI/CD via GitHub Actions
- Containerized deployment
- Zeabur for hosting backend services

---

## Key Features

- Natural language interface for querying structured SQL data  
- Live schema-aware SQL generation using an LLM  
- Safe query execution with validation and read-only enforcement  
- User authentication via Neon Auth (email/password)  
- JWT-secured API endpoints  
- Full query auditing with user attribution  
- Execution metadata (latency, confidence, generated SQL)  
- UI for reviewing results and query explanations  
- CI/CD and containerized deployment pipeline  
- Designed for enterprise-style internal tools  

---

## Project Goals

- Demonstrate a production-grade LLM-powered data access system  
- Showcase secure AI integration in enterprise-style software  
- Combine full-stack engineering, AI, and cloud infrastructure  
- Provide a practical example of modern internal analytics tooling  


