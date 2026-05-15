# Document Upload and Management — Data Model

## Overview

This document defines the database entities, relationships, and schema changes required for the document upload and management feature.

**Database**: SQLite (EF Core 8.0, Code-First)
**Naming Convention**: Consistent with existing models (int PKs, [Entity]Id naming)

---

## New Entities

### Document

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UploadedByUserId")]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
}
```

### DocumentShare

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int SharedWithUserId { get; set; }

    [Required]
    public int SharedByUserId { get; set; }

    public DateTime SharedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("SharedWithUserId")]
    public virtual User SharedWithUser { get; set; } = null!;

    [ForeignKey("SharedByUserId")]
    public virtual User SharedByUser { get; set; } = null!;
}
```

---

## Entity Relationship Diagram

```
┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
│    Users     │     │    Documents     │     │   Projects   │
├──────────────┤     ├──────────────────┤     ├──────────────┤
│ UserId (PK)  │◄────│ UploadedByUserId │     │ ProjectId(PK)│
│ Email        │     │ DocumentId (PK)  │────►│ Name         │
│ DisplayName  │     │ Title            │     │ Description  │
│ Role         │     │ Description      │     │ Status       │
│ Department   │     │ Category         │     └──────────────┘
└──────────────┘     │ FileName         │
       ▲             │ FilePath         │
       │             │ FileSize         │
       │             │ FileType         │
       │             │ ProjectId (FK?)  │
       │             │ Tags             │
       │             │ UploadDate       │
       │             │ UpdatedDate      │
       │             └──────────────────┘
       │                      │
       │                      │ 1:N
       │                      ▼
       │             ┌──────────────────┐
       │             │ DocumentShares   │
       │             ├──────────────────┤
       └─────────────│ SharedWithUserId │
       └─────────────│ SharedByUserId   │
                     │ DocumentShareId  │
                     │ DocumentId (FK)  │
                     │ SharedDate       │
                     └──────────────────┘
```

---

## ApplicationDbContext Changes

### New DbSets
```csharp
public DbSet<Document> Documents { get; set; } = null!;
public DbSet<DocumentShare> DocumentShares { get; set; } = null!;
```

### OnModelCreating Configuration

```csharp
// Document relationships
modelBuilder.Entity<Document>()
    .HasOne(d => d.UploadedByUser)
    .WithMany()
    .HasForeignKey(d => d.UploadedByUserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Document>()
    .HasOne(d => d.Project)
    .WithMany()
    .HasForeignKey(d => d.ProjectId)
    .OnDelete(DeleteBehavior.Restrict);

// DocumentShare relationships
modelBuilder.Entity<DocumentShare>()
    .HasOne(ds => ds.Document)
    .WithMany(d => d.Shares)
    .HasForeignKey(ds => ds.DocumentId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<DocumentShare>()
    .HasOne(ds => ds.SharedWithUser)
    .WithMany()
    .HasForeignKey(ds => ds.SharedWithUserId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<DocumentShare>()
    .HasOne(ds => ds.SharedByUser)
    .WithMany()
    .HasForeignKey(ds => ds.SharedByUserId)
    .OnDelete(DeleteBehavior.Restrict);

// Indexes
modelBuilder.Entity<Document>()
    .HasIndex(d => d.UploadedByUserId);

modelBuilder.Entity<Document>()
    .HasIndex(d => d.ProjectId);

modelBuilder.Entity<Document>()
    .HasIndex(d => d.Category);

modelBuilder.Entity<Document>()
    .HasIndex(d => d.UploadDate);

modelBuilder.Entity<DocumentShare>()
    .HasIndex(ds => new { ds.DocumentId, ds.SharedWithUserId })
    .IsUnique();
```

---

## Category Values (Predefined Constants)

```csharp
public static class DocumentCategories
{
    public const string ProjectDocuments = "Project Documents";
    public const string TeamResources = "Team Resources";
    public const string PersonalFiles = "Personal Files";
    public const string Reports = "Reports";
    public const string Presentations = "Presentations";
    public const string Other = "Other";

    public static readonly string[] All = new[]
    {
        ProjectDocuments, TeamResources, PersonalFiles,
        Reports, Presentations, Other
    };
}
```

---

## Supported File Types

```csharp
public static class SupportedFileTypes
{
    public static readonly Dictionary<string, string> Extensions = new()
    {
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".txt", "text/plain" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" }
    };

    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public static bool IsSupported(string extension)
        => Extensions.ContainsKey(extension.ToLowerInvariant());
}
```

---

## NotificationType Enum Updates

```csharp
public enum NotificationType
{
    // Existing values
    TaskAssignment,
    TaskUpdate,
    TaskDueSoon,
    TaskCompleted,
    TaskComment,
    ProjectUpdate,
    SystemAnnouncement,
    // New document values
    DocumentShared,
    ProjectDocumentUploaded
}
```

---

## Migration Notes

- Using `EnsureCreated()` for development (existing pattern)
- To apply changes: delete existing `ContosoDashboard.db` and restart app
- For production: use `dotnet ef migrations add AddDocumentManagement`
- Schema is SQLite-compatible (no SQL Server-specific features used)

**Version**: 1.0.0 | **Created**: 2026-05-15
