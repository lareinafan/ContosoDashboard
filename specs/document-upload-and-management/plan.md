# Document Upload and Management — Implementation Plan

## Overview

This plan defines the phased implementation strategy for the document upload and management feature, broken into sequential phases with clear deliverables and dependencies.

**Feature Branch**: `feature/document-upload-and-management`
**Estimated Phases**: 5
**Architecture**: Layered (Pages → Services → Data → Storage)

---

## Phase 1: Data Model and Storage Foundation

**Goal**: Create the database entities, storage service interface, and local implementation.

### Tasks

1. **Create Document model** (`Models/Document.cs`)
   - Fields: DocumentId (int, PK), Title, Description, Category (string), FileName, FilePath, FileSize (long), FileType (string, 255 chars), UploadedByUserId (int, FK), ProjectId (int?, FK), Tags (string?), UploadDate, UpdatedDate
   - Navigation properties: UploadedByUser, Project, Shares
   - Data annotations: [Required], [MaxLength] consistent with existing models

2. **Create DocumentShare model** (`Models/DocumentShare.cs`)
   - Fields: DocumentShareId (int, PK), DocumentId (int, FK), SharedWithUserId (int, FK), SharedByUserId (int, FK), SharedDate
   - Navigation properties: Document, SharedWithUser, SharedByUser

3. **Update ApplicationDbContext** (`Data/ApplicationDbContext.cs`)
   - Add `DbSet<Document> Documents`
   - Add `DbSet<DocumentShare> DocumentShares`
   - Configure relationships in `OnModelCreating`:
     - Document → User (Restrict delete)
     - Document → Project (Restrict delete)
     - DocumentShare → Document (Cascade delete)
   - Add indexes: UploadedByUserId, ProjectId, Category

4. **Create IFileStorageService interface** (`Services/IFileStorageService.cs`)
   - `Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)`
   - `Task DeleteAsync(string filePath)`
   - `Task<Stream> DownloadAsync(string filePath)`
   - `Task<string> GetUrlAsync(string filePath, TimeSpan expiration)`

5. **Create LocalFileStorageService** (`Services/LocalFileStorageService.cs`)
   - Implements IFileStorageService
   - Stores files at `AppData/uploads/{userId}/{projectId|personal}/{guid}.{ext}`
   - Creates directory structure on first upload
   - Handles file deletion and download

6. **Register services in Program.cs**
   - `builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>()`

**Deliverable**: Database tables created on app startup, file storage ready.

---

## Phase 2: Document Service (Business Logic)

**Goal**: Implement the document service layer with authorization and CRUD operations.

**Depends on**: Phase 1

### Tasks

1. **Create IDocumentService interface** (`Services/IDocumentService.cs`)
   - `Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string title, string? description, string category, int? projectId, string? tags, int uploadedByUserId)`
   - `Task<List<Document>> GetUserDocumentsAsync(int userId, string? category, int? projectId, string? sortBy, bool ascending)`
   - `Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)`
   - `Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId)`
   - `Task<List<Document>> SearchDocumentsAsync(string query, int requestingUserId)`
   - `Task<bool> UpdateDocumentAsync(int documentId, string title, string? description, string category, string? tags, int requestingUserId)`
   - `Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)`
   - `Task<bool> ShareDocumentAsync(int documentId, int sharedWithUserId, int sharedByUserId)`
   - `Task<bool> RevokeShareAsync(int documentId, int sharedWithUserId, int requestingUserId)`
   - `Task<List<Document>> GetSharedDocumentsAsync(int userId)`
   - `Task<List<Document>> GetRecentDocumentsAsync(int userId, int count)`
   - `Task<Stream?> DownloadDocumentAsync(int documentId, int requestingUserId)`

2. **Create DocumentService implementation** (`Services/DocumentService.cs`)
   - Constructor injection: ApplicationDbContext, IFileStorageService, INotificationService
   - Upload workflow: validate → authorize → generate GUID path → save file → save metadata → notify
   - Authorization checks in every method (IDOR prevention)
   - Search using EF Core LIKE across title, description, tags, uploader name
   - Permission filtering: owner OR project member OR shared with OR admin

3. **Update NotificationType enum** (`Models/Notification.cs`)
   - Add `DocumentShared` and `ProjectDocumentUploaded` values

4. **Register DocumentService in Program.cs**
   - `builder.Services.AddScoped<IDocumentService, DocumentService>()`

**Deliverable**: Full document CRUD with authorization and notifications.

---

## Phase 3: Document Pages (UI)

**Goal**: Create Blazor pages for document management.

**Depends on**: Phase 2

### Tasks

1. **Create Documents page** (`Pages/Documents.razor`)
   - "My Documents" tab: list view with title, category, date, size, project columns
   - "Shared with Me" tab: documents shared by others
   - Sorting: click column headers (title, date, category, size)
   - Filtering: category dropdown, project dropdown
   - Pagination: 20 documents per page, server-side
   - Action buttons: Download, Preview, Edit, Delete, Share

2. **Create Upload modal component** (`Pages/DocumentUpload.razor`)
   - InputFile component with `@key` for re-render after upload
   - Form fields: Title (required), Description, Category dropdown (predefined), Project dropdown (user's projects), Tags input
   - File validation: size ≤ 25 MB, extension whitelist
   - Progress indicator during upload
   - MemoryStream copy pattern for Blazor Server
   - Success/error message display

3. **Create Document Preview/Download endpoint** (`Pages/DocumentDownload.cshtml` + `.cshtml.cs`)
   - Razor Page handler for file serving
   - Authorization check before serving
   - Preview mode: inline Content-Disposition for PDF/images
   - Download mode: attachment Content-Disposition

4. **Create Document Edit modal** (`Pages/DocumentEdit.razor`)
   - Edit title, description, category, tags
   - Save button with validation

5. **Create Document Share modal** (`Pages/DocumentShare.razor`)
   - User search/selection
   - Current shares list with revoke option

6. **Update NavMenu.razor** (`Shared/NavMenu.razor`)
   - Add "Documents" navigation link with icon

**Deliverable**: Full document UI with upload, browse, search, edit, delete, share.

---

## Phase 4: Integration with Existing Features

**Goal**: Connect documents to tasks, projects, and dashboard.

**Depends on**: Phase 3

### Tasks

1. **Update Dashboard (Index.razor)**
   - Add "Recent Documents" widget showing last 5 documents
   - Add document count to summary cards

2. **Update ProjectDetails.razor**
   - Add "Documents" tab showing project documents
   - Add "Upload Document" button for project context

3. **Create Task Document attachment**
   - Add "Attach Document" button to task detail view
   - Auto-associate with task's project
   - Display attached documents list on task

4. **Update notification handling**
   - Create notifications when documents are shared
   - Create notifications when documents uploaded to projects

**Deliverable**: Documents integrated into dashboard, projects, and tasks.

---

## Phase 5: Testing and Polish

**Goal**: Verify all scenarios, edge cases, and performance targets.

**Depends on**: Phase 4

### Tasks

1. **Manual testing of all acceptance scenarios** (spec.md scenarios 1.1–7.4)
2. **Edge case testing** (clarifications C1–C10)
3. **Performance verification**
   - Upload 25 MB file: ≤ 30 seconds
   - Document list with 500 records: ≤ 2 seconds
   - Search: ≤ 2 seconds
   - Preview: ≤ 3 seconds
4. **Security verification**
   - IDOR prevention across all endpoints
   - File type validation
   - Path traversal prevention
5. **UI polish**
   - Responsive layout verification
   - Error message clarity
   - Loading states

**Deliverable**: Feature ready for merge to main.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────┐
│              Blazor Pages                    │
│  Documents.razor  DocumentUpload.razor       │
│  DocumentEdit.razor  DocumentShare.razor     │
├─────────────────────────────────────────────┤
│           Service Layer                      │
│  IDocumentService ──→ DocumentService        │
│  IFileStorageService ──→ LocalFileStorage    │
│  INotificationService (existing)             │
├─────────────────────────────────────────────┤
│           Data Layer                         │
│  ApplicationDbContext                        │
│  ├── Documents (DbSet<Document>)             │
│  └── DocumentShares (DbSet<DocumentShare>)   │
├─────────────────────────────────────────────┤
│           Storage                            │
│  AppData/uploads/{userId}/{context}/{guid}   │
└─────────────────────────────────────────────┘
```

---

## Risk Mitigation

| Risk | Mitigation |
|---|---|
| Large file memory pressure | 25 MB cap, MemoryStream pattern |
| Orphaned files on DB failure | File-first save order, manual cleanup acceptable |
| IDOR vulnerabilities | Service-layer auth checks on every operation |
| Path traversal attacks | GUID-only filenames, never use user input in paths |
| Blazor stream disposal | MemoryStream copy, null reference after copy |

**Version**: 1.0.0 | **Created**: 2026-05-15
