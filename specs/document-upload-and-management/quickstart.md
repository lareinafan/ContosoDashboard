# Document Upload and Management — Quickstart Guide

## Overview

This guide provides step-by-step instructions to get started implementing the document upload and management feature for ContosoDashboard.

---

## Prerequisites

- .NET 8.0 SDK installed
- Visual Studio Code with C# Dev Kit and GitHub Copilot extensions
- ContosoDashboard project cloned and building successfully
- Feature branch: `feature/document-upload-and-management`

---

## Quick Setup

### 1. Switch to the Feature Branch

```powershell
cd C:\TrainingProjects\ContosoDashboard
git checkout feature/document-upload-and-management
```

### 2. Verify the App Builds

```powershell
cd ContosoDashboard
dotnet restore && dotnet build
```

### 3. Verify the App Runs

```powershell
dotnet run
```

Navigate to `http://localhost:5000`, log in as any user, and confirm the dashboard loads.

---

## Implementation Order

Follow these phases in sequence. Each phase builds on the previous one.

### Phase 1: Data Model (Start Here)

**Files to create:**
1. `Models/Document.cs` — See `data-model.md` for complete entity definition
2. `Models/DocumentShare.cs` — See `data-model.md` for complete entity definition
3. `Services/IFileStorageService.cs` — Storage abstraction interface
4. `Services/LocalFileStorageService.cs` — Local filesystem implementation

**Files to modify:**
1. `Data/ApplicationDbContext.cs` — Add DbSets and relationship configuration
2. `Models/Notification.cs` — Add DocumentShared, ProjectDocumentUploaded to enum
3. `Program.cs` — Register IFileStorageService

**Verify:** Delete `ContosoDashboard.db`, run app, check that Documents and DocumentShares tables are created in startup logs.

### Phase 2: Document Service

**Files to create:**
1. `Services/IDocumentService.cs` — Service interface
2. `Services/DocumentService.cs` — Business logic implementation

**Files to modify:**
1. `Program.cs` — Register IDocumentService

**Verify:** Service compiles and resolves via dependency injection.

### Phase 3: UI Pages

**Files to create:**
1. `Pages/Documents.razor` — Main document management page
2. `Pages/DocumentUpload.razor` — Upload modal component (or inline in Documents.razor)
3. `Pages/DocumentDownload.cshtml` + `.cshtml.cs` — File download/preview endpoint

**Files to modify:**
1. `Shared/NavMenu.razor` — Add "Documents" navigation link

**Verify:** Navigate to `/documents`, upload a file, see it in the list, download it.

### Phase 4: Integrations

**Files to modify:**
1. `Pages/Index.razor` — Add "Recent Documents" widget
2. `Pages/ProjectDetails.razor` — Add project documents section

**Verify:** Dashboard shows recent documents, project pages show associated documents.

---

## Key Patterns to Follow

### Follow Existing Code Patterns

| Pattern | Example in Codebase | Apply to Documents |
|---|---|---|
| Model structure | `Models/TaskItem.cs` | `Models/Document.cs` |
| Service interface | `Services/ITaskService.cs` | `Services/IDocumentService.cs` |
| Service implementation | `Services/TaskService.cs` | `Services/DocumentService.cs` |
| Blazor page | `Pages/Tasks.razor` | `Pages/Documents.razor` |
| Authorization checks | `TaskService.GetTaskByIdAsync()` | `DocumentService.GetDocumentByIdAsync()` |
| DI registration | `Program.cs` services block | Add document services |

### File Upload Pattern (Critical)

```csharp
// In your Blazor component:
private IBrowserFile? SelectedFile;
private int inputFileKey = 0; // For @key reset

private async Task HandleUpload()
{
    if (SelectedFile == null) return;

    // 1. Extract metadata FIRST
    var fileName = SelectedFile.Name;
    var fileSize = SelectedFile.Size;
    var contentType = SelectedFile.ContentType;

    // 2. Copy to MemoryStream
    using var memoryStream = new MemoryStream();
    using (var stream = SelectedFile.OpenReadStream(maxAllowedSize: 25 * 1024 * 1024))
    {
        await stream.CopyToAsync(memoryStream);
    }
    memoryStream.Position = 0;

    // 3. Clear file reference
    SelectedFile = null;
    inputFileKey++; // Force InputFile re-render
    StateHasChanged();

    // 4. Call service
    await DocumentService.UploadDocumentAsync(memoryStream, fileName, contentType, ...);
}
```

---

## Validation Rules

| Rule | Value | Error Message |
|---|---|---|
| Max file size | 25 MB (26,214,400 bytes) | "File size exceeds the maximum limit of 25 MB" |
| Supported extensions | .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .jpg, .jpeg, .png | "File type not supported" |
| Min file size | 1 byte | "File is empty. Please select a valid file." |
| Title required | Non-empty string | "Document title is required" |
| Category required | From predefined list | "Category is required" |

---

## Testing Checklist

After implementing each phase, verify:

- [ ] App builds without errors (`dotnet build`)
- [ ] App runs and database tables are created
- [ ] Can upload a PDF file with title and category
- [ ] Uploaded document appears in "My Documents" list
- [ ] Can download the uploaded file
- [ ] Can preview PDF and image files
- [ ] Can edit document metadata
- [ ] Can delete a document (with confirmation)
- [ ] Can share a document with another user
- [ ] Shared document appears in recipient's "Shared with Me"
- [ ] Dashboard shows "Recent Documents" widget
- [ ] Project page shows project documents
- [ ] Unauthorized access returns null (IDOR prevention)
- [ ] Files > 25 MB are rejected
- [ ] Unsupported file types are rejected

---

## Common Issues

| Issue | Solution |
|---|---|
| `IBrowserFile` disposed error | Use MemoryStream copy pattern (see above) |
| Database tables not created | Delete `ContosoDashboard.db` and restart |
| File not found on download | Check AppData/uploads directory exists |
| Authorization failure | Ensure all claims (NameIdentifier, Role) are set in Login.cshtml.cs |
| MIME type too long | FileType column is 255 chars — sufficient for Office types |

**Version**: 1.0.0 | **Created**: 2026-05-15
