using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.Application.Comments;

public sealed class UpdateTaskCommentRequest
{
    [Required, StringLength(1500, MinimumLength = 1)] public string Content { get; init; } = string.Empty;
}
