namespace TaskManagement.Domain.Authorization;

public static class ApplicationPermissions
{
    public static class Projects
    {
        public const string Create = "Project.Create";
        public const string Read = "Project.Read";
        public const string Update = "Project.Update";
        public const string Delete = "Project.Delete";
    }

    public static class Tasks
    {
        public const string Create = "Task.Create";
        public const string Read = "Task.Read";
        public const string Update = "Task.Update";
        public const string Delete = "Task.Delete";
    }

    public static class Comments
    {
        public const string Create = "Comment.Create";
        public const string Update = "Comment.Update";
        public const string Delete = "Comment.Delete";
    }

    public static readonly IReadOnlyList<string> All = new[]
    {
        Projects.Create, Projects.Read, Projects.Update, Projects.Delete,
        Tasks.Create, Tasks.Read, Tasks.Update, Tasks.Delete,
        Comments.Create, Comments.Update, Comments.Delete
    };
}
