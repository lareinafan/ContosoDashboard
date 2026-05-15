using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        INotificationService notificationService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    public async Task<Document> UploadDocumentAsync(
        Stream fileStream, string fileName, string contentType,
        string title, string description, string category,
        int uploadedByUserId, int? projectId, string? tags)
    {
        // Validation
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateCategory(category);

        var extension = Path.GetExtension(fileName);
        if (!SupportedFileTypes.IsSupported(extension))
        {
            throw new ArgumentException($"File type '{extension}' is not supported.");
        }

        if (fileStream.Length > SupportedFileTypes.MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds the maximum allowed size of {SupportedFileTypes.MaxFileSizeBytes / (1024 * 1024)}MB.");
        }

        // Upload file to storage
        var filePath = await _fileStorageService.UploadAsync(fileStream, fileName, contentType);

        // Create document record
        var document = new Document
        {
            Title = title,
            Description = description,
            Category = category,
            FileName = fileName,
            FilePath = filePath,
            FileSize = fileStream.Length,
            FileType = extension,
            UploadedByUserId = uploadedByUserId,
            ProjectId = projectId,
            Tags = tags,
            UploadDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Notify project members if this is a project document
        if (projectId.HasValue)
        {
            await NotifyProjectMembersAsync(document, uploadedByUserId);
        }

        return document;
    }

    public async Task<IEnumerable<Document>> GetDocumentsForUserAsync(int userId)
    {
        var ownedDocuments = _context.Documents
            .Where(d => d.UploadedByUserId == userId);

        var sharedDocumentIds = _context.DocumentShares
            .Where(ds => ds.SharedWithUserId == userId)
            .Select(ds => ds.DocumentId);

        var sharedDocuments = _context.Documents
            .Where(d => sharedDocumentIds.Contains(d.DocumentId));

        return await ownedDocuments
            .Union(sharedDocuments)
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetDocumentsForProjectAsync(int projectId, int requestingUserId)
    {
        // Check user has access to the project (is manager or member)
        var hasProjectAccess = await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId && p.ProjectManagerId == requestingUserId)
            || await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == requestingUserId);

        if (!hasProjectAccess)
        {
            return Enumerable.Empty<Document>();
        }

        return await _context.Documents
            .Where(d => d.ProjectId == projectId)
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.Shares)
                .ThenInclude(s => s.SharedWithUser)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null) return null;

        // Authorization: user must be owner or have a share
        var isOwner = document.UploadedByUserId == requestingUserId;
        var isSharedWith = document.Shares.Any(s => s.SharedWithUserId == requestingUserId);

        if (!isOwner && !isSharedWith)
        {
            return null;
        }

        return document;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null) return false;

        // Authorization: only owner can delete
        if (document.UploadedByUserId != requestingUserId)
        {
            return false;
        }

        // Delete file from storage
        await _fileStorageService.DeleteAsync(document.FilePath);

        // Delete document record (shares cascade)
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Document> UpdateDocumentAsync(
        int documentId, string title, string description,
        string category, string? tags, int requestingUserId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null)
        {
            throw new ArgumentException($"Document with ID {documentId} not found.");
        }

        // Authorization: only owner can update
        if (document.UploadedByUserId != requestingUserId)
        {
            throw new UnauthorizedAccessException("Only the document owner can update this document.");
        }

        // Validation
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateCategory(category);

        document.Title = title;
        document.Description = description;
        document.Category = category;
        document.Tags = tags;
        document.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return document;
    }

    public async Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm, int userId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetDocumentsForUserAsync(userId);
        }

        var lowerSearch = searchTerm.ToLower();

        // Get IDs the user can access (owned + shared)
        var ownedIds = _context.Documents
            .Where(d => d.UploadedByUserId == userId)
            .Select(d => d.DocumentId);

        var sharedIds = _context.DocumentShares
            .Where(ds => ds.SharedWithUserId == userId)
            .Select(ds => ds.DocumentId);

        var accessibleIds = ownedIds.Union(sharedIds);

        return await _context.Documents
            .Where(d => accessibleIds.Contains(d.DocumentId))
            .Where(d =>
                d.Title.ToLower().Contains(lowerSearch) ||
                (d.Description != null && d.Description.ToLower().Contains(lowerSearch)) ||
                (d.Tags != null && d.Tags.ToLower().Contains(lowerSearch)) ||
                d.FileName.ToLower().Contains(lowerSearch))
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }
        if (title.Length > 200)
        {
            throw new ArgumentException("Title must be 200 characters or fewer.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description != null && description.Length > 2000)
        {
            throw new ArgumentException("Description must be 2000 characters or fewer.");
        }
    }

    private static void ValidateCategory(string category)
    {
        if (!DocumentCategories.All.Contains(category))
        {
            throw new ArgumentException($"Category '{category}' is not valid. Must be one of: {string.Join(", ", DocumentCategories.All)}");
        }
    }

    private async Task NotifyProjectMembersAsync(Document document, int uploadedByUserId)
    {
        // Get project members (manager + members) excluding the uploader
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == document.ProjectId);

        if (project == null) return;

        var memberUserIds = project.ProjectMembers
            .Select(pm => pm.UserId)
            .ToList();

        // Include the project manager
        if (!memberUserIds.Contains(project.ProjectManagerId))
        {
            memberUserIds.Add(project.ProjectManagerId);
        }

        // Exclude the uploader
        memberUserIds = memberUserIds.Where(id => id != uploadedByUserId).ToList();

        foreach (var userId in memberUserIds)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = userId,
                Title = "New Project Document",
                Message = $"A new document '{document.Title}' was uploaded to project '{project.Name}'.",
                Type = NotificationType.ProjectDocumentUploaded,
                Priority = NotificationPriority.Informational
            });
        }
    }
}
