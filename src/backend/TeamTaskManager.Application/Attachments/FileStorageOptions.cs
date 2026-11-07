namespace TeamTaskManager.Application.Attachments;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; init; } = "uploads";
    public long MaxFileSizeBytes { get; init; } = 5_242_880;
    public IReadOnlyCollection<string> AllowedExtensions { get; init; } = [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx", ".txt"];
}
