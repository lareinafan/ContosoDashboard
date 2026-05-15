using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentShareService
{
    Task<DocumentShare> ShareDocumentAsync(int documentId, int sharedWithUserId, int sharedByUserId);
    Task<bool> RevokeShareAsync(int documentShareId, int requestingUserId);
    Task<IEnumerable<DocumentShare>> GetSharesForDocumentAsync(int documentId, int requestingUserId);
    Task<IEnumerable<Document>> GetDocumentsSharedWithUserAsync(int userId);
    Task<bool> HasAccessAsync(int documentId, int userId);
}
