using ENB_project.Controls;
using ENB_project.Data;

namespace ENB_project
{
    public class UserList
    {
        public User? GetUser(string login)
        {
            using var uow = new UnitOfWork();
            var entity = uow.Users.GetByLogin(login);
            if (entity == null) return null;

            return new User
            {
                Login     = entity.Login,
                Password  = entity.Password,
                Email     = entity.Email,
                Theme     = entity.Theme,
                Language  = entity.Language,
                IsBlocked = entity.IsBlocked,
                Tree      = LoadTree(uow, entity.Id)
            };
        }

        public bool AddUser(string login, string password,
            string email, string theme, string language)
        {
            using var uow = new UnitOfWork();
            if (uow.Users.Exists(login)) return false;

            uow.Users.Add(new UserEntity
            {
                Login    = login,
                Password = password,
                Email    = email,
                Theme    = theme,
                Language = language
            });
            uow.Commit();
            return true;
        }

        public void DelUser(string login)
        {
            using var uow = new UnitOfWork();
            var entity = uow.Users.GetByLogin(login);
            if (entity == null) return;

            uow.Users.Remove(entity);
            uow.Commit();
        }

        public bool EditUser(string login, string propertyName, string value)
        {
            using var uow = new UnitOfWork();
            var result = uow.Users.Edit(login, propertyName, value);
            if (result) uow.Commit();
            return result;
        }

        public List<AdminUserInfo> GetAllUsers()
        {
            using var uow = new UnitOfWork();
            return uow.Users.GetAll()
                .Select(u => new AdminUserInfo
                {
                    Id        = u.Id,
                    Login     = u.Login,
                    Email     = u.Email,
                    IsBlocked = u.IsBlocked,
                    NoteCount = uow.Nodes.CountChildren(u.Id)
                })
                .ToList();
        }

        public bool SetUserBlocked(string login, bool isBlocked)
        {
            using var uow = new UnitOfWork();
            uow.Users.SetBlocked(login, isBlocked);
            uow.Commit();
            return true;
        }

        public FileSystemNode AddNodeToRoot(string login, string nodeName,
            FileItemType type = FileItemType.File)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.AddNodeToRoot");
            var sortOrder = uow.Nodes.CountRoots(entity.Id);
            var node      = BuildNodeEntity(entity.Id, null, nodeName, type, sortOrder);
            uow.Nodes.Add(node);
            uow.Commit();
            return MapNode(node);
        }

        public FileSystemNode AddNodeToFolder(string login,
            FileSystemNode parent, string nodeName,
            FileItemType type = FileItemType.File)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.AddNodeToFolder");
            var parentDb  = uow.Nodes.GetByNameWithChildren(parent.Name, entity.Id)
                            ?? throw MyExceptions.Navigation(
                                $"Узел «{parent.Name}» не найден", "UserList.AddNodeToFolder");
            var sortOrder = parentDb.Children.Count;
            var node      = BuildNodeEntity(entity.Id, parentDb.Id, nodeName, type, sortOrder);
            uow.Nodes.Add(node);
            uow.Commit();
            return MapNode(node);
        }

        public bool RemoveNode(string login, FileSystemNode node)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.RemoveNode");
            var nodeDb    = uow.Nodes.GetByNameWithChildren(node.Name, entity.Id);
            if (nodeDb == null) return false;

            uow.Nodes.RemoveRecursive(nodeDb);
            uow.Commit();
            return true;
        }

        public void MoveNode(string login, FileSystemNode node, FileSystemNode? newParent)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.MoveNode");

            var nodeDb = uow.Nodes.GetByName(node.Name, entity.Id)
                         ?? throw MyExceptions.Navigation(
                             $"Узел «{node.Name}» не найден", "UserList.MoveNode");

            if (newParent == null)
            {
                nodeDb.ParentId  = null;
                nodeDb.SortOrder = uow.Nodes.CountRoots(entity.Id);
            }
            else
            {
                var parentDb = uow.Nodes.GetByName(newParent.Name, entity.Id)
                               ?? throw MyExceptions.Navigation(
                                   $"Узел «{newParent.Name}» не найден", "UserList.MoveNode");
                nodeDb.ParentId  = parentDb.Id;
                nodeDb.SortOrder = uow.Nodes.CountChildren(parentDb.Id);
            }

            uow.Commit();
        }

        public async Task SaveNoteContentAsync(string login, string nodeName, string content)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.SaveNoteContentAsync");

            var nodeDb = uow.Nodes.GetByName(nodeName, entity.Id);
            if (nodeDb == null) return;

            var noteContent = uow.NoteContents.GetByNodeId(nodeDb.Id);
            if (noteContent == null)
                uow.NoteContents.Add(new NoteContentEntity { NodeId = nodeDb.Id, Content = content });
            else
                noteContent.Content = content;

            nodeDb.EditTime = DateOnly.FromDateTime(DateTime.Now);

            try   { await uow.CommitAsync(); }
            catch { uow.Rollback(); throw;   }
        }

        public void RenameNode(string login, string oldName, string newName)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.RenameNode");
            var nodeDb    = uow.Nodes.GetByName(oldName, entity.Id);
            if (nodeDb == null) return;

            nodeDb.Name = newName;
            uow.Commit();
        }

        public void SetNodeCategory(string login, string nodeName, int? categoryId)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.SetNodeCategory");
            var nodeDb    = uow.Nodes.GetByName(nodeName, entity.Id);
            if (nodeDb == null) return;

            nodeDb.CategoryId = categoryId;
            uow.Commit();
        }

        public List<Category> GetCategories(string login)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.GetCategories");
            return uow.Categories.GetAllForUser(entity.Id)
                .Select(c => new Category { Id = c.Id, Name = c.Name, Color = c.Color })
                .ToList();
        }

        public Category AddCategory(string login, string name, string color)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.AddCategory");
            var cat       = new CategoryEntity { UserId = entity.Id, Name = name, Color = color };
            uow.Categories.Add(cat);
            uow.Commit();
            return new Category { Id = cat.Id, Name = cat.Name, Color = cat.Color };
        }

        public bool EditCategory(int categoryId, string name, string color)
        {
            using var uow = new UnitOfWork();
            var cat       = uow.Categories.GetById(categoryId);
            if (cat == null) return false;

            cat.Name  = name;
            cat.Color = color;
            uow.Commit();
            return true;
        }

        public bool DeleteCategory(int categoryId)
        {
            using var uow = new UnitOfWork();
            var cat       = uow.Categories.GetById(categoryId);
            if (cat == null) return false;

            uow.Categories.Remove(cat);
            uow.Commit();
            return true;
        }

        public Reminder? GetReminder(string login, string nodeName)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.GetReminder");
            var nodeDb    = uow.Nodes.GetByName(nodeName, entity.Id);
            if (nodeDb == null) return null;

            var reminder = uow.Reminders.GetByNodeId(nodeDb.Id);
            return reminder == null ? null : MapReminder(reminder);
        }

        public Reminder SetReminder(string login, string nodeName, DateTime remindAt, string note)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.SetReminder");

            var nodeDb = uow.Nodes.GetByName(nodeName, entity.Id)
                         ?? throw MyExceptions.Navigation(
                             $"Узел «{nodeName}» не найден", "UserList.SetReminder");

            var existing = uow.Reminders.GetByNodeId(nodeDb.Id);

            if (existing == null)
            {
                var reminder = new ReminderEntity
                {
                    UserId      = entity.Id,
                    NodeId      = nodeDb.Id,
                    RemindAt    = remindAt,
                    Note        = note,
                    IsTriggered = false
                };
                uow.Reminders.Add(reminder);
                uow.Commit();
                return MapReminder(reminder);
            }
            else
            {
                existing.RemindAt    = remindAt;
                existing.Note        = note;
                existing.IsTriggered = false;
                uow.Commit();
                return MapReminder(existing);
            }
        }

        public bool DeleteReminder(string login, string nodeName)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.DeleteReminder");
            var nodeDb    = uow.Nodes.GetByName(nodeName, entity.Id);
            if (nodeDb == null) return false;

            var reminder = uow.Reminders.GetByNodeId(nodeDb.Id);
            if (reminder == null) return false;

            uow.Reminders.Remove(reminder);
            uow.Commit();
            return true;
        }

        public List<TriggeredReminder> GetAndMarkTriggeredReminders(string login)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.GetAndMarkTriggeredReminders");

            var triggers = uow.Reminders.GetTriggered(entity.Id, DateTime.Now);
            if (triggers.Count == 0) return new List<TriggeredReminder>();

            foreach (var t in triggers)
                t.IsTriggered = true;

            uow.Commit();

            return triggers.Select(t => new TriggeredReminder
            {
                NoteName = t.Node.Name,
                Note     = t.Note,
                RemindAt = t.RemindAt
            }).ToList();
        }

        public FileSystemTree LoadTreeForUser(string login)
        {
            using var uow = new UnitOfWork();
            var entity    = GetUserEntityOrThrow(uow, login, "UserList.LoadTreeForUser");
            return LoadTree(uow, entity.Id);
        }

        private static FileSystemTree LoadTree(IUnitOfWork uow, int userId)
        {
            var allNodes   = uow.Nodes.GetAllForUser(userId);
            var nodeIds    = allNodes.Select(n => n.Id).ToList();
            var contentMap = uow.NoteContents.GetContentMapForNodes(nodeIds);

            var now     = DateTime.Now;
            var nodeMap = allNodes.ToDictionary(
                n => n.Id,
                n => new FileSystemNode(n.Name,
                    n.ItemType == "Folder" ? FileItemType.Folder : FileItemType.File)
                {
                    Content           = contentMap.GetValueOrDefault(n.Id),
                    CategoryId        = n.CategoryId,
                    CategoryColor     = n.Category?.Color,
                    CreateTime        = n.CreateTime,
                    EditTime          = n.EditTime,
                    ReminderId        = n.Reminder?.Id,
                    ReminderAt        = n.Reminder?.RemindAt,
                    ReminderNote      = n.Reminder?.Note,
                    ReminderTriggered = n.Reminder != null
                                        && n.Reminder.IsTriggered
                                        && n.Reminder.RemindAt <= now
                });

            var tree = new FileSystemTree();

            foreach (var entity in allNodes)
            {
                var node = nodeMap[entity.Id];
                if (entity.ParentId == null)
                    tree.Roots.Add(node);
                else if (nodeMap.TryGetValue(entity.ParentId.Value, out var parentNode))
                {
                    node.Parent = parentNode;
                    parentNode.Children.Add(node);
                }
            }

            return tree;
        }

        private static UserEntity GetUserEntityOrThrow(IUnitOfWork uow, string login, string location)
            => uow.Users.GetByLogin(login)
               ?? throw MyExceptions.Navigation($"Пользователь «{login}» не найден", location);

        private static FileSystemNodeEntity BuildNodeEntity(
            int userId, int? parentId, string name, FileItemType type, int sortOrder)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var node  = new FileSystemNodeEntity
            {
                UserId     = userId,
                ParentId   = parentId,
                Name       = name,
                ItemType   = type == FileItemType.Folder ? "Folder" : "File",
                SortOrder  = sortOrder,
                CreateTime = today,
                EditTime   = today
            };

            if (type == FileItemType.File)
                node.NoteContent = new NoteContentEntity { Content = "" };

            return node;
        }

        private static FileSystemNode MapNode(FileSystemNodeEntity e) =>
            new(e.Name, e.ItemType == "Folder" ? FileItemType.Folder : FileItemType.File)
            {
                CreateTime = e.CreateTime,
                EditTime   = e.EditTime
            };

        private static Reminder MapReminder(ReminderEntity r) => new()
        {
            Id          = r.Id,
            NodeId      = r.NodeId,
            RemindAt    = r.RemindAt,
            Note        = r.Note,
            IsTriggered = r.IsTriggered
        };
    }

    public class User
    {
        public required string Login     { get; init; }
        public required string Password  { get; set; }
        public required string Email     { get; set; }
        public required string Theme     { get; set; }
        public required string Language  { get; set; }
        public bool            IsBlocked { get; set; }
        public FileSystemTree  Tree      { get; set; } = new();
    }

    public class Category
    {
        public int    Id    { get; set; }
        public string Name  { get; set; } = string.Empty;
        public string Color { get; set; } = "#7C6FCD";
    }

    public class AdminUserInfo
    {
        public int    Id        { get; set; }
        public string Login     { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public bool   IsBlocked { get; set; }
        public int    NoteCount { get; set; }
    }

    public class Reminder
    {
        public int      Id          { get; set; }
        public int      NodeId      { get; set; }
        public DateTime RemindAt    { get; set; }
        public string   Note        { get; set; } = string.Empty;
        public bool     IsTriggered { get; set; }
    }

    public class TriggeredReminder
    {
        public string   NoteName { get; set; } = string.Empty;
        public string   Note     { get; set; } = string.Empty;
        public DateTime RemindAt { get; set; }
    }
}