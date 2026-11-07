using TeamTaskManager.Application.Tasks;

namespace TeamTaskManager.Application.Comments;

public interface ITaskCommentService
{
    Task<IReadOnlyCollection<TaskCommentDto>?> GetCommentsAsync(TaskAccessContext access, int taskId, CancellationToken cancellationToken = default);
    Task<CommentOperationResult<TaskCommentDto>> CreateAsync(TaskAccessContext access, int taskId, CreateTaskCommentRequest request, CancellationToken cancellationToken = default);
    Task<CommentOperationResult<TaskCommentDto>> UpdateAsync(TaskAccessContext access, int commentId, UpdateTaskCommentRequest request, CancellationToken cancellationToken = default);
    Task<CommentOperationResult<TaskCommentDto>> DeleteAsync(TaskAccessContext access, int commentId, CancellationToken cancellationToken = default);
}
