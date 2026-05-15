using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string title, string description, string category, int uploadedByUserId, int? projectId, string? tags);
    Task<IEnumerable<Document>> GetDocumentsForUserAsync(int userId);
    Task<IEnumerable<Document>> GetDocumentsForProjectAsync(int projectId, int requestingUserId);
    Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId);
    Task<Document> UpdateDocumentAsync(int documentId, string title, string description, string category, string? tags, int requestingUserId);
    Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm, int userId);
}
