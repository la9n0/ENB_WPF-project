using ENB_project.Controls;

namespace ENB_project.Data
{
    public interface IUserRepository
    {
        UserEntity? GetByLogin(string login);
        bool        Exists(string login);
        void        Add(UserEntity user);
        void        Remove(UserEntity user);
        bool        Edit(string login, string propertyName, string value);
        void        SetBlocked(string login, bool isBlocked);
        List<UserEntity> GetAll();
    }

    public interface INodeRepository
    {
        FileSystemNodeEntity? GetByName(string name, int userId);
        FileSystemNodeEntity? GetByNameWithChildren(string name, int userId);
        FileSystemNodeEntity? GetByNameWithReminder(string name, int userId);
        List<FileSystemNodeEntity> GetAllForUser(int userId);
        int CountRoots(int userId);
        int CountChildren(int parentId);
        void Add(FileSystemNodeEntity node);
        void Remove(FileSystemNodeEntity node);
        void RemoveRecursive(FileSystemNodeEntity node);
    }

    public interface INoteContentRepository
    {
        NoteContentEntity? GetByNodeId(int nodeId);
        Dictionary<int, string> GetContentMapForNodes(List<int> nodeIds);
        void Add(NoteContentEntity content);
    }

    public interface ICategoryRepository
    {
        List<CategoryEntity> GetAllForUser(int userId);
        CategoryEntity?      GetById(int id);
        void Add(CategoryEntity category);
        void Remove(CategoryEntity category);
    }

    public interface IReminderRepository
    {
        ReminderEntity? GetByNodeId(int nodeId);
        List<ReminderEntity> GetTriggered(int userId, DateTime before);
        void Add(ReminderEntity reminder);
        void Remove(ReminderEntity reminder);
    }

    public interface IUnitOfWork : IDisposable
    {
        IUserRepository     Users     { get; }
        INodeRepository     Nodes     { get; }
        INoteContentRepository NoteContents { get; }
        ICategoryRepository Categories { get; }
        IReminderRepository Reminders  { get; }

        void Commit();
        void Rollback();
        System.Threading.Tasks.Task CommitAsync();
        System.Threading.Tasks.Task RollbackAsync();
    }
}