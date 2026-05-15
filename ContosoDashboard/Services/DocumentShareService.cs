using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class DocumentShareService : IDocumentShareService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public DocumentShareService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<DocumentShare> ShareDocumentAsync(int documentId, int sharedWithUserId, int sharedByUserId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null)
        {
            throw new ArgumentException($"Document with ID {documentId} not found.");
        }

        // Authorization: only owner can share
        if (document.UploadedByUserId != sharedByUserId)
        {
            throw new UnauthorizedAccessException("Only the document owner can share this document.");
        }

        // Check not already shared with this user
        var existingShare = await _context.DocumentShares
            .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.SharedWithUserId == sharedWithUserId);

        if (existingShare != null)
        {
            throw new ArgumentException("Document is already shared with this user.");
        }

        var share = new DocumentShare
        {
            DocumentId = documentId,
            SharedWithUserId = sharedWithUserId,
            SharedByUserId = sharedByUserId,
            SharedDate = DateTime.UtcNow
        };

        _context.DocumentShares.Add(share);
        await _context.SaveChangesAsync();

        // Create notification for the user the document was shared with
        await _notificationService.CreateNotificationAsync(new Notification
        {
            UserId = sharedWithUserId,
            Title = "Document Shared With You",
            Message = $"'{document.Title}' has been shared with you.",
            Type = NotificationType.DocumentShared,
            Priority = NotificationPriority.Informational
        });

        return share;
    }

    public async Task<bool> RevokeShareAsync(int documentShareId, int requestingUserId)
    {
        var share = await _context.DocumentShares
            .Include(ds => ds.Document)
            .FirstOrDefaultAsync(ds => ds.DocumentShareId == documentShareId);

        if (share == null) return false;

        // Authorization: only the document owner can revoke shares
        if (share.Document.UploadedByUserId != requestingUserId)
        {
            return false;
        }

        _context.DocumentShares.Remove(share);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<DocumentShare>> GetSharesForDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null)
        {
            return Enumerable.Empty<DocumentShare>();
        }

        // Authorization: only owner can view shares
        if (document.UploadedByUserId != requestingUserId)
        {
            return Enumerable.Empty<DocumentShare>();
        }

        return await _context.DocumentShares
            .Where(ds => ds.DocumentId == documentId)
            .Include(ds => ds.SharedWithUser)
            .Include(ds => ds.SharedByUser)
            .OrderByDescending(ds => ds.SharedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetDocumentsSharedWithUserAsync(int userId)
    {
        return await _context.DocumentShares
            .Where(ds => ds.SharedWithUserId == userId)
            .Include(ds => ds.Document)
                .ThenInclude(d => d.UploadedByUser)
            .Include(ds => ds.Document)
                .ThenInclude(d => d.Project)
            .Select(ds => ds.Document)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();
    }

    public async Task<bool> HasAccessAsync(int documentId, int userId)
    {
        // Owner check
        var isOwner = await _context.Documents
            .AnyAsync(d => d.DocumentId == documentId && d.UploadedByUserId == userId);

        if (isOwner) return true;

        // Share check
        return await _context.DocumentShares
            .AnyAsync(ds => ds.DocumentId == documentId && ds.SharedWithUserId == userId);
    }
}
