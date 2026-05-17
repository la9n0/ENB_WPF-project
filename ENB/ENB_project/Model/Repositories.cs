using ENB_project.Controls;
using Microsoft.EntityFrameworkCore;

namespace ENB_project.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db) => _db = db;

        public UserEntity? GetByLogin(string login)
            => _db.Users.AsNoTracking().FirstOrDefault(u => u.Login == login);

        public bool Exists(string login)
            => _db.Users.Any(u => u.Login == login);

        public void Add(UserEntity user)
            => _db.Users.Add(user);

        public void Remove(UserEntity user)
            => _db.Users.Remove(user);

        public bool Edit(string login, string propertyName, string value)
        {
            var entity = _db.Users.FirstOrDefault(u => u.Login == login);
            if (entity == null) return false;

            switch (propertyName)
            {
                case nameof(User.Password): entity.Password = value; break;
                case nameof(User.Email):    entity.Email    = value; break;
                case nameof(User.Theme):    entity.Theme    = value; break;
                case nameof(User.Language): entity.Language = value; break;
                default: return false;
            }

            return true;
        }

        public void SetBlocked(string login, bool isBlocked)
        {
            var entity = _db.Users.FirstOrDefault(u => u.Login == login);
            if (entity != null) entity.IsBlocked = isBlocked;
        }

        public List<UserEntity> GetAll()
            => _db.Users.AsNoTracking().OrderBy(u => u.Login).ToList();
    }

    public class NodeRepository : INodeRepository
    {
        private readonly AppDbContext _db;

        public NodeRepository(AppDbContext db) => _db = db;

        public FileSystemNodeEntity? GetByName(string name, int userId)
            => _db.FileSystemNodes
                .FirstOrDefault(n => n.Name == name && n.UserId == userId);

        public FileSystemNodeEntity? GetByNameWithChildren(string name, int userId)
            => _db.FileSystemNodes
                .Include(n => n.Children)
                .FirstOrDefault(n => n.Name == name && n.UserId == userId);

        public FileSystemNodeEntity? GetByNameWithReminder(string name, int userId)
            => _db.FileSystemNodes
                .Include(n => n.Reminder)
                .FirstOrDefault(n => n.Name == name && n.UserId == userId);

        public List<FileSystemNodeEntity> GetAllForUser(int userId)
            => _db.FileSystemNodes
                .AsNoTracking()
                .Include(n => n.Category)
                .Include(n => n.Reminder)
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.SortOrder)
                .ToList();

        public int CountRoots(int userId)
            => _db.FileSystemNodes.Count(n => n.UserId == userId && n.ParentId == null);

        public int CountChildren(int parentId)
            => _db.FileSystemNodes.Count(n => n.ParentId == parentId);

        public void Add(FileSystemNodeEntity node)
            => _db.FileSystemNodes.Add(node);

        public void Remove(FileSystemNodeEntity node)
            => _db.FileSystemNodes.Remove(node);

        public void RemoveRecursive(FileSystemNodeEntity node)
        {
            var children = _db.FileSystemNodes
                .Where(n => n.ParentId == node.Id)
                .ToList();

            foreach (var child in children)
                RemoveRecursive(child);

            _db.FileSystemNodes.Remove(node);
        }
    }

    public class NoteContentRepository : INoteContentRepository
    {
        private readonly AppDbContext _db;

        public NoteContentRepository(AppDbContext db) => _db = db;

        public NoteContentEntity? GetByNodeId(int nodeId)
            => _db.NoteContents.FirstOrDefault(c => c.NodeId == nodeId);

        public Dictionary<int, string> GetContentMapForNodes(List<int> nodeIds)
            => _db.NoteContents
                .AsNoTracking()
                .Where(c => nodeIds.Contains(c.NodeId))
                .ToDictionary(c => c.NodeId, c => c.Content);

        public void Add(NoteContentEntity content)
            => _db.NoteContents.Add(content);
    }

    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _db;

        public CategoryRepository(AppDbContext db) => _db = db;

        public List<CategoryEntity> GetAllForUser(int userId)
            => _db.Categories
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToList();

        public CategoryEntity? GetById(int id)
            => _db.Categories.FirstOrDefault(c => c.Id == id);

        public void Add(CategoryEntity category)
            => _db.Categories.Add(category);

        public void Remove(CategoryEntity category)
            => _db.Categories.Remove(category);
    }

    public class ReminderRepository : IReminderRepository
    {
        private readonly AppDbContext _db;

        public ReminderRepository(AppDbContext db) => _db = db;

        public ReminderEntity? GetByNodeId(int nodeId)
            => _db.Reminders
                .AsNoTracking()
                .FirstOrDefault(r => r.NodeId == nodeId);

        public List<ReminderEntity> GetTriggered(int userId, DateTime before)
            => _db.Reminders
                .Include(r => r.Node)
                .Where(r => r.UserId == userId && !r.IsTriggered && r.RemindAt <= before)
                .ToList();

        public void Add(ReminderEntity reminder)
            => _db.Reminders.Add(reminder);

        public void Remove(ReminderEntity reminder)
            => _db.Reminders.Remove(reminder);
    }
}