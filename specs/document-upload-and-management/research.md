# Document Upload and Management — Technical Research

## Overview

This document captures technology decisions, alternatives evaluated, and rationale for the document upload and management feature implementation.

**Feature**: Document Upload and Management
**Date**: 2026-05-15

---

## 1. File Storage Strategy

### Decision: Local Filesystem with IFileStorageService Abstraction

**Options Evaluated:**

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| Local Filesystem | Offline-capable, simple, no dependencies | Not scalable, single-server only | ✅ Selected (training) |
| Azure Blob Storage | Scalable, CDN-ready, managed | Requires internet, Azure account | Future migration |
| Database BLOB storage | Simple deployment | Poor performance, DB bloat | ❌ Rejected |
| Embedded object store (MinIO) | Cloud-compatible API | Extra dependency, complex setup | ❌ Rejected |

**Rationale**: The training environment requires offline operation. The `IFileStorageService` interface pattern enables future Azure Blob Storage migration without code changes to business logic, pages, or database schema.

### File Organization Pattern
```
AppData/uploads/
├── {userId}/
│   ├── personal/
│   │   └── {guid}.{ext}
│   └── {projectId}/
│       └── {guid}.{ext}
```

**Security considerations:**
- Files stored OUTSIDE `wwwroot` (prevents direct URL access)
- GUID-based filenames prevent path traversal attacks
- User-supplied filenames never used in file paths
- Download endpoint enforces authorization checks

---

## 2. File Upload in Blazor Server

### Decision: MemoryStream Copy Pattern with InputFile Component

**Challenge**: Blazor Server's `IBrowserFile` streams can be disposed between render cycles due to the server-side SignalR architecture.

**Solution Pattern:**
```csharp
// 1. Extract metadata BEFORE opening stream
var fileName = SelectedFile.Name;
var fileSize = SelectedFile.Size;
var contentType = SelectedFile.ContentType;

// 2. Copy to MemoryStream immediately
using var memoryStream = new MemoryStream();
using (var fileStream = SelectedFile.OpenReadStream(maxAllowedSize: 25 * 1024 * 1024))
{
    await fileStream.CopyToAsync(memoryStream);
}
memoryStream.Position = 0;

// 3. Clear reference to prevent reuse errors
SelectedFile = null;
StateHasChanged();

// 4. Pass MemoryStream to service layer
await _documentService.UploadAsync(memoryStream, fileName, contentType, ...);
```

**Why not streaming directly to disk?**
- `IBrowserFile` stream lifecycle tied to Blazor component
- Network interruptions during SignalR transfer cause disposal
- MemoryStream decouples file data from component lifecycle

**File size limit**: 25 MB is within acceptable memory bounds for MemoryStream approach.

---

## 3. Database Design Decisions

### Document Entity — Key Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Primary Key Type | `int` (auto-increment) | Consistent with User, Task, Project tables |
| Category Storage | `string` (text) | Stakeholder requirement — simpler than enum |
| FileType Field Length | 255 chars | Office MIME types can be very long |
| FilePath Field Length | 500 chars | GUID paths with directory structure |
| Tags Storage | Comma-separated string | Simple for initial release, searchable with LIKE |

### Relationships
- `Document → User` (UploadedBy): Required, Restrict delete
- `Document → Project`: Optional, Cascade delete consideration → **Restrict** (preserve documents if project deleted)
- `DocumentShare → Document`: Required, Cascade delete (remove shares when document deleted)
- `DocumentShare → User`: Required, Restrict delete

---

## 4. Authorization Strategy

### Decision: Service-Layer Authorization (Consistent with Existing Pattern)

The existing codebase enforces authorization at the service layer (e.g., `TaskService.GetTaskByIdAsync` checks `requestingUserId`). The document feature follows this same pattern:

```csharp
public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)
{
    var doc = await _context.Documents.FindAsync(documentId);
    if (doc == null) return null;

    // Check: owner, project member, shared with user, or admin
    var isOwner = doc.UploadedByUserId == requestingUserId;
    var isProjectMember = doc.ProjectId.HasValue && 
        await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == doc.ProjectId && pm.UserId == requestingUserId);
    var isSharedWith = await _context.DocumentShares.AnyAsync(ds => ds.DocumentId == documentId && ds.SharedWithUserId == requestingUserId);
    var isAdmin = await IsAdminAsync(requestingUserId);

    if (!isOwner && !isProjectMember && !isSharedWith && !isAdmin)
        return null; // IDOR protection

    return doc;
}
```

---

## 5. Search Implementation

### Decision: EF Core LINQ with Database-Level Filtering

**Options Evaluated:**

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| EF Core LINQ | No extra deps, consistent | Limited full-text | ✅ Selected |
| SQLite FTS5 | Full-text search | SQLite-specific, complex | Future enhancement |
| Lucene.NET | Powerful search | Heavy dependency | ❌ Rejected |
| Elasticsearch | Enterprise search | Requires separate service | ❌ Rejected |

**Implementation**: Use `EF.Functions.Like()` for pattern matching across title, description, tags, and uploader name. Permission filtering applied in the same query to ensure authorized results only.

---

## 6. Notification Integration

### Decision: Reuse Existing INotificationService

The existing `NotificationService` handles notification creation and delivery. Document events will create notifications using the same pattern as task notifications:

**Document notification types to add to `NotificationType` enum:**
- `DocumentShared` — when a document is shared with a user
- `ProjectDocumentUploaded` — when a new document is added to a project

---

## 7. Preview Strategy

### Decision: Controller Endpoint Serving Files with Content-Type Headers

- **PDF Preview**: Serve with `Content-Type: application/pdf` — browsers render natively
- **Image Preview**: Serve with `Content-Type: image/jpeg` or `image/png` — browsers render natively
- **Other files**: Download only (no in-browser preview for Office documents in initial release)

The download/preview endpoint requires authentication and authorization checks before serving file content.

---

## References

- [ASP.NET Core Blazor file uploads](https://learn.microsoft.com/en-us/aspnet/core/blazor/file-uploads)
- [EF Core - SQLite Provider](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/)
- [Azure Blob Storage migration patterns](https://learn.microsoft.com/en-us/azure/storage/blobs/)
