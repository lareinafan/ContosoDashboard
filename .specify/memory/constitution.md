# ContosoDashboard Constitution

## Core Principles

### I. Security-First Design
All features must implement defense-in-depth security. Every page requires authentication via `@attribute [Authorize]`. Service-layer authorization checks must prevent IDOR vulnerabilities by verifying user relationships before returning data. Security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy) are mandatory on all responses. Cookie-based authentication must use sliding expiration. No sensitive data in client-side code.

### II. Layered Architecture
The application follows a strict layered architecture: Blazor Pages → Service Interfaces → Service Implementations → EF Core DbContext → Database. Dependencies flow downward only. Services are registered via dependency injection and accessed through interfaces (IUserService, ITaskService, IProjectService, INotificationService, IDashboardService). Direct DbContext access from pages is prohibited.

### III. Role-Based Access Control
Authorization follows a hierarchical role model: Administrator > ProjectManager > TeamLead > Employee. Policies are cumulative — higher roles inherit all permissions of lower roles. Every data-access operation must validate the requesting user's relationship to the resource (assigned user, creator, project member, or project manager).

### IV. Code Quality Standards
C# with nullable reference types enabled. Implicit usings enabled for cleaner code. Entity Framework Core code-first approach with explicit relationship configuration and performance indexes. All foreign keys must specify delete behavior explicitly. Seed data provides a consistent development baseline with representative test scenarios.

### V. Simplicity and Maintainability
Follow YAGNI principles — implement only what is needed. Blazor Server for real-time UI updates via WebSocket. Bootstrap 5.3 for responsive, mobile-first layouts. No unnecessary abstractions. Clear naming conventions following .NET standards. Each component, service, and model has a single, well-defined responsibility.

## Security Requirements

### Authentication & Session Management
- Cookie-based authentication with 8-hour sliding expiration
- Mock login system for training (dropdown user selection, no passwords)
- Infrastructure prepared for Microsoft Entra ID integration (OpenIdConnect packages included)
- Custom `AuthenticationStateProvider` for Blazor authentication state synchronization

### HTTP Security Headers
All responses must include:
- `X-Content-Type-Options: nosniff` — prevent MIME type sniffing
- `X-Frame-Options: DENY` — clickjacking protection
- `X-XSS-Protection: 1; mode=block` — XSS filtering
- `Referrer-Policy: strict-origin-when-cross-origin` — referrer control
- `Content-Security-Policy` — restrict resource loading (self, cdn.jsdelivr.net for Bootstrap)
- HSTS enabled in all environments

### Data Protection
- IDOR prevention: services return null for unauthorized access attempts instead of throwing exceptions
- Entity relationships use `DeleteBehavior.Restrict` on critical foreign keys to prevent cascading data loss
- Unique constraints on email addresses to prevent duplicate accounts

## Technical Standards

### Technology Stack
- **Runtime**: .NET 8.0 (ASP.NET Core)
- **UI Framework**: Blazor Server with Razor Pages
- **ORM**: Entity Framework Core 8.0 with code-first migrations
- **Database**: SQLite for local development; SQL Server LocalDB as alternative
- **CSS**: Bootstrap 5.3 with Bootstrap Icons
- **Hosting**: Kestrel on localhost:5000 (HTTP) / localhost:5001 (HTTPS)

### Database Conventions
- Primary keys use `[Entity]Id` naming (UserId, TaskId, ProjectId)
- Indexes on frequently queried columns (AssignedUserId, Status, DueDate, Email)
- Composite indexes for common query patterns (UserId + IsRead for notifications)
- Seed data in `ApplicationDbContext.OnModelCreating` for development consistency

### Project Structure
```
ContosoDashboard/
├── Data/           # DbContext and data access
├── Models/         # Entity definitions and enums
├── Pages/          # Blazor components and Razor pages
├── Services/       # Business logic (interface + implementation)
├── Shared/         # Shared Blazor layout components
├── wwwroot/        # Static files (CSS, JS, images)
└── Program.cs      # Application entry point and configuration
```

## Development Workflow

### Spec-Driven Development
This project uses GitHub Spec Kit for structured development. The workflow follows:
1. `/speckit.constitution` — Establish project principles (this document)
2. `/speckit.specify` — Create feature specifications
3. `/speckit.plan` — Generate implementation plans
4. `/speckit.tasks` — Break plans into actionable tasks
5. `/speckit.implement` — Execute implementation with AI assistance

### Version Control
- Main branch protection with meaningful commit messages
- All Spec Kit artifacts committed alongside code changes
- `.specify/` directory contains development workflow configuration
- `.github/agents/` and `.github/prompts/` contain Copilot integration files

## Governance

This constitution defines the authoritative standards for the ContosoDashboard project. All code changes, reviews, and architectural decisions must comply with these principles. Amendments require documentation of rationale, team review, and an updated version number below.

**Version**: 1.0.0 | **Ratified**: 2026-05-15 | **Last Amended**: 2026-05-15
