# Document Upload and Management — Feature Specification

## Overview

Add document upload and management capabilities to ContosoDashboard, enabling employees to upload, organize, browse, search, share, and manage work-related documents within the existing dashboard application.

**Source**: `StakeholderDocs/document-upload-and-management-feature.md`
**Feature Branch**: `feature/document-upload-and-management`
**Status**: Clarified
**Created**: 2026-05-15

---

## User Roles and Permissions

| Role | Upload Own | View Own | View Team | View Project | Manage Project Docs | Manage All |
|---|---|---|---|---|---|---|
| Employee | ✅ | ✅ | ❌ | ✅ (if member) | ❌ | ❌ |
| Team Lead | ✅ | ✅ | ✅ | ✅ (if member) | ❌ | ❌ |
| Project Manager | ✅ | ✅ | ✅ | ✅ (own projects) | ✅ | ❌ |
| Administrator | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## Acceptance Scenarios

### 1. Document Upload

#### Scenario 1.1: Successful single file upload
```
Given the user is logged in as an Employee
And the user navigates to the Documents page
When the user clicks "Upload Document"
And selects a PDF file of 5 MB
And enters a title "Q4 Report"
And selects category "Reports"
And clicks "Upload"
Then the file is saved to local storage at {userId}/{personal}/{guid}.pdf
And a database record is created with the file metadata
And a success message "Document uploaded successfully" is displayed
And the document appears in the user's document list
```

#### Scenario 1.2: Upload with project association
```
Given the user is logged in as an Employee
And the user is a member of "ContosoDashboard Development" project
When the user uploads a document
And selects "ContosoDashboard Development" as the associated project
Then the document is stored at {userId}/{projectId}/{guid}.{ext}
And the document appears in the project's document list
And all project team members can view the document
And project members receive an in-app notification about the new document
```

#### Scenario 1.3: File exceeds 25 MB size limit
```
Given the user is logged in
When the user selects a file that is 30 MB
And attempts to upload it
Then the upload is rejected
And an error message "File size exceeds the maximum limit of 25 MB" is displayed
And no file is saved to disk
And no database record is created
```

#### Scenario 1.4: Unsupported file type rejected
```
Given the user is logged in
When the user selects a file with extension .exe
And attempts to upload it
Then the upload is rejected
And an error message "File type not supported. Supported types: PDF, Word, Excel, PowerPoint, text, JPEG, PNG" is displayed
```

#### Scenario 1.5: Supported file types accepted
```
Given the user is logged in
When the user uploads files with the following extensions
Then each file type is accepted:
  | Extension | MIME Type                                                                 |
  | .pdf      | application/pdf                                                           |
  | .doc      | application/msword                                                        |
  | .docx     | application/vnd.openxmlformats-officedocument.wordprocessingml.document    |
  | .xls      | application/vnd.ms-excel                                                  |
  | .xlsx     | application/vnd.openxmlformats-officedocument.spreadsheetml.sheet         |
  | .ppt      | application/vnd.ms-powerpoint                                            |
  | .pptx     | application/vnd.openxmlformats-officedocument.presentationml.presentation |
  | .txt      | text/plain                                                                |
  | .jpg      | image/jpeg                                                                |
  | .jpeg     | image/jpeg                                                                |
  | .png      | image/png                                                                 |
```

#### Scenario 1.6: Upload progress indicator
```
Given the user is logged in
When the user initiates a file upload
Then a progress indicator is displayed during the upload
And the user can see the upload status (uploading/complete/error)
```

#### Scenario 1.7: Required metadata validation
```
Given the user is on the upload form
When the user attempts to upload without entering a title
Then the form shows a validation error "Document title is required"
And when the user attempts to upload without selecting a category
Then the form shows a validation error "Category is required"
```

### 2. Document Organization and Browsing

#### Scenario 2.1: View My Documents list
```
Given the user is logged in as "Ni Kang"
And has uploaded 3 documents
When the user navigates to "My Documents"
Then a list of 3 documents is displayed
And each document shows: title, category, upload date, file size, associated project
```

#### Scenario 2.2: Sort documents
```
Given the user is viewing their document list
When the user clicks the "Upload Date" column header
Then documents are sorted by upload date (newest first)
And when the user clicks "Title" column header
Then documents are sorted alphabetically by title
And when the user clicks "File Size" column header
Then documents are sorted by file size
And when the user clicks "Category" column header
Then documents are sorted by category name
```

#### Scenario 2.3: Filter documents by category
```
Given the user has documents in categories "Reports" and "Personal Files"
When the user selects "Reports" from the category filter
Then only documents in the "Reports" category are displayed
```

#### Scenario 2.4: Filter documents by project
```
Given the user has documents associated with "ContosoDashboard Development"
And documents not associated with any project
When the user filters by "ContosoDashboard Development"
Then only documents linked to that project are displayed
```

#### Scenario 2.5: View project documents
```
Given the user is a member of "ContosoDashboard Development"
When the user views the project details page
Then a "Documents" section shows all documents associated with the project
And the user can download any project document
```

### 3. Document Search

#### Scenario 3.1: Search by document title
```
Given the user has a document titled "Q4 Financial Report"
When the user searches for "Financial"
Then the "Q4 Financial Report" document appears in the results
And results load within 2 seconds
```

#### Scenario 3.2: Search respects permissions
```
Given User A has uploaded a personal document "Private Notes"
And User A has not shared it with User B
When User B searches for "Private Notes"
Then no results are returned for User B
```

#### Scenario 3.3: Search by multiple fields
```
Given the user searches for a term
Then the search checks: title, description, tags, uploader name, associated project
And returns matching documents the user has permission to view
```

### 4. Document Access and Management

#### Scenario 4.1: Download a document
```
Given the user has access to a document
When the user clicks "Download" on the document
Then the file is downloaded to the user's computer
And the download activity is logged
```

#### Scenario 4.2: Preview PDF in browser
```
Given the user has access to a PDF document
When the user clicks "Preview"
Then the PDF is displayed in the browser without downloading
```

#### Scenario 4.3: Preview image in browser
```
Given the user has access to a JPEG or PNG image
When the user clicks "Preview"
Then the image is displayed in the browser without downloading
```

#### Scenario 4.4: Edit document metadata
```
Given the user uploaded a document
When the user clicks "Edit" on the document
And changes the title from "Old Title" to "New Title"
And changes the category from "Reports" to "Presentations"
And clicks "Save"
Then the document metadata is updated
And a success message is displayed
```

#### Scenario 4.5: Delete own document
```
Given the user uploaded a document
When the user clicks "Delete" on the document
Then a confirmation dialog appears "Are you sure you want to permanently delete this document?"
And when the user confirms
Then the file is removed from storage
And the database record is deleted
And the document no longer appears in any lists
```

#### Scenario 4.6: Project Manager deletes project document
```
Given the user is a Project Manager for "ContosoDashboard Development"
And another user uploaded a document to this project
When the Project Manager clicks "Delete" on that document
Then the document is deleted after confirmation
```

#### Scenario 4.7: Unauthorized deletion prevented
```
Given User A uploaded a document
And User B is not a Project Manager or Administrator
When User B attempts to delete User A's document
Then the deletion is denied
And an error message is displayed
```

### 5. Document Sharing

#### Scenario 5.1: Share document with specific user
```
Given the user uploaded a document
When the user clicks "Share" on the document
And selects "Ni Kang" from the user list
And clicks "Share"
Then the document appears in Ni Kang's "Shared with Me" section
And Ni Kang receives an in-app notification "You have a new shared document"
```

#### Scenario 5.2: View shared documents
```
Given "Camille Nicole" shared a document with the user
When the user navigates to "Shared with Me"
Then the shared document is displayed
And shows who shared it and when
```

### 6. Integration with Existing Features

#### Scenario 6.1: Attach document to task
```
Given the user is viewing a task detail page
When the user clicks "Attach Document"
And uploads or selects an existing document
Then the document is associated with the task
And the document is automatically linked to the task's project
```

#### Scenario 6.2: Dashboard recent documents widget
```
Given the user has uploaded documents
When the user views the dashboard home page
Then a "Recent Documents" section shows the last 5 uploaded documents
And the dashboard summary cards include a document count
```

#### Scenario 6.3: Notification on project document upload
```
Given User A is a member of "ContosoDashboard Development"
When User B uploads a document to that project
Then User A receives an in-app notification
```

### 7. Performance Requirements

#### Scenario 7.1: Upload performance
```
Given a file of 25 MB
When the user uploads the file on a typical network
Then the upload completes within 30 seconds
```

#### Scenario 7.2: Document list performance
```
Given a user has 500 documents
When the user navigates to "My Documents"
Then the page loads within 2 seconds
```

#### Scenario 7.3: Search performance
```
Given 500+ documents in the system
When the user performs a search query
Then results are returned within 2 seconds
```

#### Scenario 7.4: Preview performance
```
Given a document under 25 MB
When the user clicks "Preview"
Then the preview loads within 3 seconds
```

---

## Clarifications (Edge Cases and Ambiguity Resolution)

_The following clarifications resolve ambiguous areas identified during specification review._

### C1: Documents when user is removed from a project
**Question**: What happens to documents a user uploaded to a project when that user is removed from the project?
**Resolution**: Documents remain associated with the project. The original uploader retains ownership (can still edit/delete via "My Documents") but project access is governed by current membership. The document continues to be visible to current project members.

### C2: Special characters in filenames
**Question**: How should the system handle special characters, unicode, or very long filenames?
**Resolution**: Original filenames are stored in the `FileName` metadata field for display purposes only. The actual stored file uses a GUID-based name (`{guid}.{ext}`), so special characters in the original filename never affect storage. Display of the original filename should be truncated to 100 characters in list views with a tooltip showing the full name.

### C3: Concurrent uploads of the same file
**Question**: What happens if the same user uploads the same file twice?
**Resolution**: Each upload creates a separate document record with a unique GUID-based storage path. Duplicate files are allowed — the system does not deduplicate. Users may upload the same file multiple times (e.g., to different projects or categories).

### C4: Document access after sharing is revoked
**Question**: Can document owners revoke sharing? What happens to the shared user's access?
**Resolution**: Document owners can revoke sharing at any time by removing users from the share list. Once revoked, the document immediately disappears from the recipient's "Shared with Me" view and they can no longer download or preview it. No notification is sent on share revocation.

### C5: Maximum number of documents per user
**Question**: Is there a limit on total documents per user or total storage?
**Resolution**: No per-user document limit or storage quota in the initial release. Storage quotas are explicitly out of scope. However, the document list view must handle up to 500 documents efficiently (pagination required if more than 50 documents per page).

### C6: File upload interruption/failure recovery
**Question**: What happens if the upload fails midway (e.g., network timeout, browser crash)?
**Resolution**: The upload workflow saves the file to disk before creating the database record. If the file save fails, no database record is created (no orphaned records). If the database insert fails after file save, the orphaned file on disk is acceptable for the training implementation — a cleanup mechanism is out of scope. The user sees an error message and can retry.

### C7: Handling of zero-byte files
**Question**: Should the system accept zero-byte (empty) files?
**Resolution**: No. The system should reject files with 0 bytes with an error message "File is empty. Please select a valid file." Minimum file size is 1 byte.

### C8: Category management — can users add custom categories?
**Question**: Are the predefined categories fixed, or can users create custom categories?
**Resolution**: Categories are fixed to the predefined list (Project Documents, Team Resources, Personal Files, Reports, Presentations, Other). Users cannot create custom categories in the initial release. Custom categories may be added in a future version.

### C9: Documents and task deletion
**Question**: What happens to documents attached to a task when the task is deleted?
**Resolution**: Documents remain in the system — they are not deleted when a task is deleted. The task-document association is removed, but the document continues to exist in the user's "My Documents" and in the project's document list (if project-linked). The document's project association is preserved independently of the task link.

### C10: Pagination for document lists
**Question**: How should large document lists be handled for performance?
**Resolution**: Document list views must use server-side pagination with 20 documents per page by default. The page displays total document count and page navigation controls. Sorting and filtering apply across the full dataset (not just the current page).

---

## Technical Design

### Data Model

#### Document Entity
| Field | Type | Constraints |
|---|---|---|
| DocumentId | int | Primary Key, auto-increment |
| Title | string | Required, max 200 chars |
| Description | string | Optional, max 2000 chars |
| Category | string | Required (text value, not enum) |
| FileName | string | Required, original filename |
| FilePath | string | Required, GUID-based storage path (max 500 chars) |
| FileSize | long | Required, bytes |
| FileType | string | Required, MIME type (max 255 chars) |
| UploadedByUserId | int | Required, FK → Users |
| ProjectId | int? | Optional, FK → Projects |
| Tags | string | Optional, comma-separated |
| UploadDate | DateTime | Required, auto-set |
| UpdatedDate | DateTime | Required, auto-set |

#### DocumentShare Entity
| Field | Type | Constraints |
|---|---|---|
| DocumentShareId | int | Primary Key, auto-increment |
| DocumentId | int | FK → Documents |
| SharedWithUserId | int | FK → Users |
| SharedByUserId | int | FK → Users |
| SharedDate | DateTime | Required, auto-set |

### Storage Architecture

```
IFileStorageService (interface)
├── UploadAsync(Stream, fileName, contentType) → filePath
├── DeleteAsync(filePath)
├── DownloadAsync(filePath) → Stream
└── GetUrlAsync(filePath, expiration) → url

LocalFileStorageService : IFileStorageService
└── Stores files at: AppData/uploads/{userId}/{projectId|personal}/{guid}.{ext}

Future: AzureBlobStorageService : IFileStorageService
└── Uses same path pattern as Azure blob names
```

### Upload Workflow (Blazor Server)
1. User selects file via `InputFile` component
2. Extract metadata (name, size, contentType) into local variables
3. Copy `IBrowserFile` stream to `MemoryStream` (prevent disposal issues)
4. Clear `IBrowserFile` reference (set to null)
5. Validate file size (≤ 25 MB) and extension (whitelist)
6. Authorize user for target project (if applicable)
7. Generate unique GUID-based filename
8. Save file to disk via `IFileStorageService`
9. Create database record with file path
10. Send notifications to project members (if project-linked)

### Category Values (Predefined)
- Project Documents
- Team Resources
- Personal Files
- Reports
- Presentations
- Other

---

## Out of Scope

- Real-time collaborative editing
- Version history and rollback
- Advanced document workflows (approval processes)
- External system integration (SharePoint, OneDrive)
- Mobile app support
- Document templates or generation
- Storage quotas
- Soft delete/trash with recovery

---

## Success Criteria

| Metric | Target |
|---|---|
| User adoption (3 months) | 70% of active users uploaded ≥1 document |
| Document retrieval time | < 30 seconds average |
| Document categorization rate | 90% properly categorized |
| Security incidents | Zero related to document access |

**Version**: 1.0.0 | **Created**: 2026-05-15
