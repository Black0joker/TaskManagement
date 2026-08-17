namespace TaskManagement.Application.Features.Comments;

public sealed record CommentResponse(
    string Id,
    string TaskId,
    string AuthorId,
    string AuthorName,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);
