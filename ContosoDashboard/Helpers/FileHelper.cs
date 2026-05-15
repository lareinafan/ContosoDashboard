namespace ContosoDashboard.Helpers;

public static class FileHelper
{
    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    public static string GetFileIcon(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "bi bi-file-earmark-pdf",
            ".doc" or ".docx" => "bi bi-file-earmark-word",
            ".xls" or ".xlsx" => "bi bi-file-earmark-excel",
            ".ppt" or ".pptx" => "bi bi-file-earmark-ppt",
            ".txt" => "bi bi-file-earmark-text",
            ".jpg" or ".jpeg" or ".png" => "bi bi-file-earmark-image",
            _ => "bi bi-file-earmark"
        };
    }
}
