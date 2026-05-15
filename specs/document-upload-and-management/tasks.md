# Document Upload and Management — Tasks Breakdown

## Overview

Phased, actionable implementation tasks derived from `spec.md` and `plan.md`. Tasks are ordered: foundation → backend → frontend → integration → testing.

**Feature Branch**: `feature/document-upload-and-management`
**Total Tasks**: T001–T045
**Created**: 2026-05-15

---

## MVP Implementation Strategy

The MVP encompasses tasks **T001–T045** across 5 phases. Each phase must be completed before the next begins. Within each phase, tasks should be executed in order unless noted as parallelizable.

---

## Phase 1: Data Model and Storage Foundation

_Estimated: 8 tasks | Dependencies: None_

### T001 — Create Document model
- **File**: `Models/Document.cs`
- **Action**: Create Document entity class with properties: DocumentId (int, PK), Title (string, required, max 200), Description (string?, max 2000), Category (string, required, max 50), FileName (string, required, max 255), FilePath (string, required, max 500), FileSize (long), FileType (string, required, max 255), UploadedByUserId (int, FK), ProjectId (int?, FK), Tags (string?, max 500), UploadDate (DateTime), UpdatedDate (DateTime)
- **Pattern**: Follow `Models/TaskItem.cs` structure with [Key], [Required], [MaxLength] annotations and navigation properties
- **Acceptance**: Model compiles, follows existing naming conventions

### T002 — Create DocumentShare model
- **File**: `Models/DocumentShare.cs`
- **Action**: Create DocumentShare entity with: DocumentShareId (int, PK), DocumentId (int, FK), SharedWithUserId (int, FK), SharedByUserId (int, FK), SharedDate (DateTime)
- **Pattern**: Follow `Models/ProjectMember.cs` as join-table pattern
- **Acceptance**: Model compiles with navigation properties to Document and User

### T003 — Create DocumentCategories constants class
- **File**: `Models/Document.cs` (append) or `Models/DocumentCategories.cs`
- **Action**: Create static class with predefined category constants: "Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other" and a string[] All property
- **Acceptance**: Constants accessible throughout the application

### T004 — Create SupportedFileTypes constants class
- **File**: `Models/Document.cs` (append) or `Models/SupportedFileTypes.cs`
- **Action**: Create static class with Dictionary<string, string> mapping extensions to MIME types (.pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .jpg, .jpeg, .png), MaxFileSizeBytes constant (25 * 1024 * 1024), and IsSupported(string extension) method
- **Acceptance**: File type validation works for all 11 supported extensions

### T005 — Update NotificationType enum
- **File**: `Models/Notification.cs`
- **Action**: Add `DocumentShared` and `ProjectDocumentUploaded` values to the existing NotificationType enum
- **Acceptance**: Enum compiles, existing notification types unchanged

### T006 — Update ApplicationDbContext with Document entities
- **File**: `Data/ApplicationDbContext.cs`
- **Action**: Add `DbSet<Document> Documents` and `DbSet<DocumentShare> DocumentShares`. Configure relationships in OnModelCreating: Document→User (Restrict), Document→Project (Restrict), DocumentShare→Document (Cascade), DocumentShare→User (Restrict). Add indexes on UploadedByUserId, ProjectId, Category, UploadDate. Add unique composite index on (DocumentId, SharedWithUserId) for DocumentShares
- **Pattern**: Follow existing relationship configuration patterns (see User→TaskItem relationships)
- **Acceptance**: Delete ContosoDashboard.db, run app, verify Documents and DocumentShares tables created in logs

### T007 — Create IFileStorageService interface
- **File**: `Services/IFileStorageService.cs`
- **Action**: Define interface with methods: `Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)`, `Task DeleteAsync(string filePath)`, `Task<Stream> DownloadAsync(string filePath)`, `Task<string> GetUrlAsync(string filePath, TimeSpan expiration)`
- **Acceptance**: Interface compiles, follows existing service interface naming pattern

### T008 — Create LocalFileStorageService implementation
- **File**: `Services/LocalFileStorageService.cs`
- **Action**: Implement IFileStorageService using local filesystem. Store files at `AppData/uploads/{path}`. UploadAsync: create directories, generate GUID filename, write file, return relative path. DeleteAsync: remove file if exists. DownloadAsync: open file stream. GetUrlAsync: return local path (placeholder for Azure migration)
- **Pattern**: Use Path.Combine for cross-platform paths, FileStream for I/O
- **Acceptance**: Can upload a file to disk and read it back

---

## Phase 2: Document Service (Business Logic)

_Estimated: 10 tasks | Dependencies: Phase 1 complete_

### T009 — Create IDocumentService interface
- **File**: `Services/IDocumentService.cs`
- **Action**: Define interface with all document operations: UploadDocumentAsync, GetUserDocumentsAsync, GetDocumentByIdAsync, GetProjectDocumentsAsync, SearchDocumentsAsync, UpdateDocumentAsync, DeleteDocumentAsync, ShareDocumentAsync, RevokeShareAsync, GetSharedDocumentsAsync, GetRecentDocumentsAsync, DownloadDocumentAsync, GetDocumentCountAsync
- **Pattern**: Follow `Services/ITaskService.cs` method signature patterns (include requestingUserId for auth)
- **Acceptance**: Interface compiles with all 13 methods

### T010 — Implement UploadDocumentAsync
- **File**: `Services/DocumentService.cs`
- **Action**: Validate file size (≤ 25 MB) and extension (whitelist). Authorize user for project (if projectId specified, verify membership). Generate GUID-based path: `{userId}/{projectId|personal}/{guid}.{ext}`. Save file via IFileStorageService. Create Document record. Notify project members if project-linked
- **Spec**: Scenarios 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7
- **Acceptance**: Upload creates file on disk and database record

### T011 — Implement GetUserDocumentsAsync with filtering and sorting
- **File**: `Services/DocumentService.cs`
- **Action**: Query documents where UploadedByUserId matches. Support optional filters: category, projectId. Support sorting by: title, uploadDate, category, fileSize (with ascending/descending). Include pagination (skip/take)
- **Spec**: Scenarios 2.1, 2.2, 2.3, 2.4
- **Acceptance**: Returns filtered, sorted, paginated document list

### T012 — Implement GetDocumentByIdAsync with authorization
- **File**: `Services/DocumentService.cs`
- **Action**: Fetch document by ID. Authorize: return document only if user is owner, project member, shared recipient, or administrator. Return null for unauthorized (IDOR prevention)
- **Spec**: Scenario 4.7
- **Pattern**: Follow TaskService.GetTaskByIdAsync authorization pattern
- **Acceptance**: Authorized users get document, unauthorized get null

### T013 — Implement GetProjectDocumentsAsync
- **File**: `Services/DocumentService.cs`
- **Action**: Query documents where ProjectId matches. Authorize: user must be project member or project manager. Include uploader info via .Include()
- **Spec**: Scenario 2.5
- **Acceptance**: Returns project documents for authorized members only

### T014 — Implement SearchDocumentsAsync
- **File**: `Services/DocumentService.cs`
- **Action**: Search across title, description, tags, uploader DisplayName using EF Core LIKE queries. Filter results to only documents user has permission to view (owner, project member, shared, admin)
- **Spec**: Scenarios 3.1, 3.2, 3.3
- **Acceptance**: Search returns permission-filtered results within 2 seconds

### T015 — Implement UpdateDocumentAsync
- **File**: `Services/DocumentService.cs`
- **Action**: Update title, description, category, tags. Authorize: only document owner can edit. Set UpdatedDate to UTC now
- **Spec**: Scenario 4.4
- **Acceptance**: Owner can update metadata, non-owners are rejected

### T016 — Implement DeleteDocumentAsync
- **File**: `Services/DocumentService.cs`
- **Action**: Authorize: owner, project manager (for project docs), or administrator can delete. Delete file via IFileStorageService. Delete database record (cascade removes DocumentShares)
- **Spec**: Scenarios 4.5, 4.6, 4.7
- **Acceptance**: Authorized users can delete, file removed from disk and database

### T017 — Implement ShareDocumentAsync and RevokeShareAsync
- **File**: `Services/DocumentService.cs`
- **Action**: ShareDocumentAsync: create DocumentShare record, create notification for recipient. RevokeShareAsync: delete DocumentShare record. Authorize: only document owner can share/revoke
- **Spec**: Scenarios 5.1, Clarification C4
- **Acceptance**: Shared documents appear in recipient's shared list, revoked documents disappear

### T018 — Implement GetSharedDocumentsAsync and GetRecentDocumentsAsync
- **File**: `Services/DocumentService.cs`
- **Action**: GetSharedDocumentsAsync: query documents via DocumentShares for user. GetRecentDocumentsAsync: query user's documents ordered by UploadDate descending, take N
- **Spec**: Scenarios 5.2, 6.2
- **Acceptance**: Shared documents list correct, recent documents returns last N

---

## Phase 3: Document Pages (UI)

_Estimated: 12 tasks | Dependencies: Phase 2 complete_

### T019 — Register services in Program.cs
- **File**: `Program.cs`
- **Action**: Add `builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>()` and `builder.Services.AddScoped<IDocumentService, DocumentService>()` to the service registration block
- **Acceptance**: Services resolve via DI, app starts without errors

### T020 — Add Documents link to NavMenu
- **File**: `Shared/NavMenu.razor`
- **Action**: Add "Documents" navigation link with document icon, positioned after "My Projects" in the nav list. Route to `/documents`
- **Pattern**: Follow existing NavMenu link structure
- **Acceptance**: Documents link visible in navigation, navigates to /documents

### T021 — Create Documents page — layout and routing
- **File**: `Pages/Documents.razor`
- **Action**: Create Blazor page with `@page "/documents"` and `@attribute [Authorize]`. Add page title, tabs for "My Documents" and "Shared with Me". Inject IDocumentService, AuthenticationStateProvider
- **Pattern**: Follow `Pages/Tasks.razor` page structure
- **Acceptance**: Page loads at /documents with correct layout

### T022 — Create Documents page — My Documents tab
- **File**: `Pages/Documents.razor`
- **Action**: Implement "My Documents" tab with table displaying: title, category, upload date, file size (formatted), associated project. Add column header click handlers for sorting. Add category dropdown filter and project dropdown filter
- **Spec**: Scenarios 2.1, 2.2, 2.3, 2.4
- **Acceptance**: Documents listed with sorting and filtering working

### T023 — Create Documents page — pagination
- **File**: `Pages/Documents.razor`
- **Action**: Implement server-side pagination with 20 items per page. Show total count and page navigation controls. Apply sorting/filtering across full dataset
- **Spec**: Clarification C10
- **Acceptance**: Pagination works, navigating pages preserves sort/filter state

### T024 — Create Documents page — Shared with Me tab
- **File**: `Pages/Documents.razor`
- **Action**: Implement "Shared with Me" tab showing documents shared by others. Display: title, shared by (name), shared date, file size. Action buttons: Download, Preview
- **Spec**: Scenario 5.2
- **Acceptance**: Shared documents displayed with sharer info

### T025 — Create Documents page — search functionality
- **File**: `Pages/Documents.razor`
- **Action**: Add search input field above document list. On input, call SearchDocumentsAsync with debounce (300ms). Display results replacing the current list view
- **Spec**: Scenarios 3.1, 3.2, 3.3
- **Acceptance**: Search filters documents in real-time, respects permissions

### T026 — Create Upload Document modal
- **File**: `Pages/Documents.razor` (component or section)
- **Action**: Create upload form with: InputFile component (with @key for re-render), Title input (required), Description textarea, Category dropdown (predefined values), Project dropdown (user's projects, optional), Tags input. Implement MemoryStream copy pattern. Add file size and type validation with error messages. Add progress indicator
- **Spec**: Scenarios 1.1, 1.3, 1.4, 1.5, 1.6, 1.7, Clarification C7
- **Acceptance**: Can upload a valid file, validation errors shown for invalid files

### T027 — Create Edit Document modal
- **File**: `Pages/Documents.razor` (component or section)
- **Action**: Create edit form pre-populated with current values: Title, Description, Category, Tags. Save button calls UpdateDocumentAsync. Only shown for documents owned by current user
- **Spec**: Scenario 4.4
- **Acceptance**: Can edit and save document metadata

### T028 — Create Share Document modal
- **File**: `Pages/Documents.razor` (component or section)
- **Action**: Create share form with user search/selection. Display current shares with revoke buttons. Only shown for documents owned by current user
- **Spec**: Scenarios 5.1, Clarification C4
- **Acceptance**: Can share with users and revoke shares

### T029 — Create Delete Document confirmation
- **File**: `Pages/Documents.razor`
- **Action**: Delete button shows confirmation dialog "Are you sure you want to permanently delete this document?". On confirm, calls DeleteDocumentAsync. Show for owners, project managers (project docs), and admins
- **Spec**: Scenarios 4.5, 4.6
- **Acceptance**: Confirmation required, document deleted on confirm

### T030 — Create Document Download/Preview endpoint
- **File**: `Pages/DocumentDownload.cshtml` + `Pages/DocumentDownload.cshtml.cs`
- **Action**: Create Razor Page handler. Accept documentId and mode (download/preview) parameters. Authorize user via IDocumentService.GetDocumentByIdAsync. For preview: set Content-Disposition to inline (PDF, images only). For download: set Content-Disposition to attachment. Stream file content from IFileStorageService
- **Spec**: Scenarios 4.1, 4.2, 4.3
- **Acceptance**: PDF previews in browser, files download with correct names

---

## Phase 4: Integration with Existing Features

_Estimated: 8 tasks | Dependencies: Phase 3 complete_

### T031 — Add Recent Documents widget to Dashboard
- **File**: `Pages/Index.razor`
- **Action**: Add "Recent Documents" section below announcements. Call GetRecentDocumentsAsync(userId, 5). Display document title, category, and upload date with links to /documents
- **Spec**: Scenario 6.2
- **Acceptance**: Dashboard shows last 5 uploaded documents

### T032 — Add document count to Dashboard summary cards
- **File**: `Pages/Index.razor`
- **Action**: Add "Documents" summary card alongside Active Tasks, Due Today, etc. Call GetDocumentCountAsync for current user
- **Spec**: Scenario 6.2
- **Acceptance**: Dashboard shows document count card

### T033 — Add Documents tab to ProjectDetails page
- **File**: `Pages/ProjectDetails.razor`
- **Action**: Add "Documents" tab/section to project detail view. Call GetProjectDocumentsAsync for current project. Display document list with download/preview actions. Add "Upload Document" button that pre-selects the current project
- **Spec**: Scenario 2.5
- **Acceptance**: Project page shows associated documents

### T034 — Create task-document association model
- **File**: `Models/TaskDocument.cs` (or extend existing models)
- **Action**: Create join entity for Task-Document association: TaskDocumentId, TaskId (FK), DocumentId (FK). Update ApplicationDbContext with DbSet and relationships
- **Spec**: Scenario 6.1
- **Acceptance**: Database table created, relationships configured

### T035 — Add document attachment to task detail view
- **File**: `Pages/Tasks.razor` or task detail component
- **Action**: Add "Attach Document" button to task detail. Allow uploading new document or selecting existing. Auto-associate document with task's project. Display attached documents list
- **Spec**: Scenario 6.1
- **Acceptance**: Can attach documents to tasks, auto-links to project

### T036 — Implement document-shared notification
- **File**: `Services/DocumentService.cs`
- **Action**: In ShareDocumentAsync, create notification: Type=DocumentShared, Title="Document shared with you", Message="{sharer} shared '{docTitle}' with you"
- **Spec**: Scenario 5.1
- **Acceptance**: Recipient sees notification after document is shared

### T037 — Implement project-document-uploaded notification
- **File**: `Services/DocumentService.cs`
- **Action**: In UploadDocumentAsync (when projectId is set), create notification for all project members: Type=ProjectDocumentUploaded, Title="New project document", Message="{uploader} uploaded '{docTitle}' to {projectName}"
- **Spec**: Scenario 6.3
- **Acceptance**: Project members receive notification on new document upload

### T038 — Add document activity logging
- **File**: `Services/DocumentService.cs`
- **Action**: Log all document operations (upload, download, delete, share, revoke) using ILogger. Include: userId, documentId, action type, timestamp
- **Spec**: Reporting and Audit requirement 9.1
- **Acceptance**: Document activities appear in application logs

---

## Phase 5: Testing and Polish

_Estimated: 7 tasks | Dependencies: Phase 4 complete_

### T039 — Test all upload scenarios (1.1–1.7)
- **Action**: Manually test: successful upload, project association, 25 MB limit rejection, unsupported type rejection, all 11 supported types, progress indicator, required field validation, zero-byte file rejection
- **Acceptance**: All upload scenarios pass per spec.md

### T040 — Test document browsing and search (2.1–3.3)
- **Action**: Test: My Documents list display, sort by all 4 columns, filter by category, filter by project, project documents view, search by title/description/tags, permission-filtered search results, pagination
- **Acceptance**: All browsing and search scenarios pass

### T041 — Test document management (4.1–5.2)
- **Action**: Test: download file, preview PDF in browser, preview image in browser, edit metadata, delete own document with confirmation, PM delete project document, unauthorized deletion blocked, share document, shared document notification, Shared with Me view, share revocation
- **Acceptance**: All access and management scenarios pass

### T042 — Test integration features (6.1–6.3)
- **Action**: Test: attach document to task, auto-project association, dashboard recent documents widget, dashboard document count card, project document upload notification
- **Acceptance**: All integration scenarios pass

### T043 — Test edge cases (C1–C10)
- **Action**: Test all clarification items: documents after project removal, special character filenames, duplicate uploads, share revocation access, no storage limits, upload failure recovery, zero-byte rejection, fixed categories only, task deletion preserves documents, pagination at 500 documents
- **Acceptance**: All edge cases handled per clarifications

### T044 — Performance verification
- **Action**: Verify: upload 25 MB file ≤ 30 seconds, document list with 500 records ≤ 2 seconds, search ≤ 2 seconds, preview ≤ 3 seconds
- **Spec**: Scenarios 7.1–7.4
- **Acceptance**: All performance targets met

### T045 — Security verification
- **Action**: Verify: IDOR prevention on all endpoints (test accessing other users' documents), file type validation cannot be bypassed, path traversal prevention (verify GUID-only filenames), authorization on download endpoint, no sensitive data in client-side code
- **Acceptance**: Zero security vulnerabilities found

---

## Task Dependencies

```
Phase 1: T001 → T002 → T003 → T004 → T005 → T006 → T007 → T008
                                                  ↓
Phase 2: T009 → T010 → T011 → T012 → T013 → T014 → T015 → T016 → T017 → T018
                                                                          ↓
Phase 3: T019 → T020 → T021 → T022 → T023 → T024 → T025 → T026 → T027 → T028 → T029 → T030
                                                                                          ↓
Phase 4: T031 → T032 → T033 → T034 → T035 → T036 → T037 → T038
                                                              ↓
Phase 5: T039 → T040 → T041 → T042 → T043 → T044 → T045
```

**Parallelizable within phases:**
- Phase 1: T001–T005 can be done in parallel (independent model files)
- Phase 2: T011–T018 can be partially parallelized (independent service methods)
- Phase 3: T022–T025 can be partially parallelized (independent UI sections)
- Phase 5: T039–T043 can be fully parallelized (independent test suites)

---

## Summary

| Phase | Tasks | Range | Description |
|---|---|---|---|
| 1 — Foundation | 8 | T001–T008 | Models, constants, DbContext, storage service |
| 2 — Backend | 10 | T009–T018 | Document service with full CRUD and authorization |
| 3 — Frontend | 12 | T019–T030 | Blazor pages, modals, download endpoint |
| 4 — Integration | 8 | T031–T038 | Dashboard, projects, tasks, notifications |
| 5 — Testing | 7 | T039–T045 | Scenario testing, edge cases, performance, security |
| **Total** | **45** | **T001–T045** | **Complete MVP implementation** |

**Version**: 1.0.0 | **Created**: 2026-05-15
