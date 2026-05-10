using ENB_project.Controls;
using Microsoft.EntityFrameworkCore;

namespace ENB_project
{
    /// <summary>
    /// Репозиторий пользователей и их файловых деревьев поверх Entity Framework.
    /// </summary>
    public class UserList
    {
        public User? GetUser(string login)
        {
            using var db = new AppDbContext();
            var entity = db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Login == login);

            return entity == null ? null : MapUser(entity, LoadTree(entity.Id));
        }

        public bool AddUser(string login, string password,
            string email, string theme, string language)
        {
            using var db = new AppDbContext();

            if (db.Users.Any(u => u.Login == login))
                return false;

            db.Users.Add(new UserEntity
            {
                Login    = login,
                Password = password,
                Email    = email,
                Theme    = theme,
                Language = language
            });
            db.SaveChanges();
            return true;
        }

        public void DelUser(string login)
        {
            using var db = new AppDbContext();
            var entity = db.Users.FirstOrDefault(u => u.Login == login);
            if (entity == null) return;
            db.Users.Remove(entity);
            db.SaveChanges();
        }

        public bool EditUser(string login, string propertyName, string value)
        {
            using var db = new AppDbContext();
            var entity = db.Users.FirstOrDefault(u => u.Login == login);
            if (entity == null) return false;

            switch (propertyName)
            {
                case nameof(User.Password): entity.Password = value; break;
                case nameof(User.Email):    entity.Email    = value; break;
                case nameof(User.Theme):    entity.Theme    = value; break;
                case nameof(User.Language): entity.Language = value; break;
                default: return false;
            }

            db.SaveChanges();
            return true;
        }

        public FileSystemTree GetTree(string login)
        {
            var entity = GetUserEntity(login, "UserList.GetTree");
            return LoadTree(entity.Id);
        }

        public FileSystemNode AddNodeToRoot(string login, string nodeName,
            FileItemType type = FileItemType.File)
        {
            var entity    = GetUserEntity(login, "UserList.AddNodeToRoot");
            var sortOrder = CountRoots(entity.Id);
            return CreateNode(entity.Id, null, nodeName, type, sortOrder);
        }

        public FileSystemNode AddNodeToFolder(string login,
            FileSystemNode parent, string nodeName,
            FileItemType type = FileItemType.File)
        {
            var entity    = GetUserEntity(login, "UserList.AddNodeToFolder");
            var parentDb  = GetNodeEntity(parent.Name, entity.Id, "UserList.AddNodeToFolder");
            var sortOrder = parentDb.Children.Count;
            return CreateNode(entity.Id, parentDb.Id, nodeName, type, sortOrder);
        }

        public FileSystemNode InsertNode(string login,
            FileSystemNode parent, int index, string nodeName,
            FileItemType type = FileItemType.File)
        {
            var entity   = GetUserEntity(login, "UserList.InsertNode");
            var parentDb = GetNodeEntity(parent.Name, entity.Id, "UserList.InsertNode");
            return CreateNode(entity.Id, parentDb.Id, nodeName, type, index);
        }

        public bool RemoveNode(string login, FileSystemNode node)
        {
            var entity   = GetUserEntity(login, "UserList.RemoveNode");
            using var db = new AppDbContext();

            var nodeDb = db.FileSystemNodes
                .Include(n => n.Children)
                .FirstOrDefault(n => n.Name == node.Name && n.UserId == entity.Id);

            if (nodeDb == null) return false;

            DeleteNodeRecursive(db, nodeDb);
            db.SaveChanges();
            return true;
        }

        public void MoveNode(string login, FileSystemNode node, FileSystemNode? newParent)
        {
            var entity   = GetUserEntity(login, "UserList.MoveNode");
            using var db = new AppDbContext();

            var nodeDb = db.FileSystemNodes
                .FirstOrDefault(n => n.Name == node.Name && n.UserId == entity.Id)
                ?? throw MyExceptions.Navigation(
                    $"Узел «{node.Name}» не найден", "UserList.MoveNode");

            if (newParent == null)
            {
                nodeDb.ParentId  = null;
                nodeDb.SortOrder = CountRoots(entity.Id);
            }
            else
            {
                var parentDb = db.FileSystemNodes
                    .FirstOrDefault(n => n.Name == newParent.Name && n.UserId == entity.Id)
                    ?? throw MyExceptions.Navigation(
                        $"Узел «{newParent.Name}» не найден", "UserList.MoveNode");

                nodeDb.ParentId  = parentDb.Id;
                nodeDb.SortOrder = db.FileSystemNodes.Count(n => n.ParentId == parentDb.Id);
            }

            db.SaveChanges();
        }

        public FileSystemNode? FindNode(string login, string nodeName)
        {
            var entity   = GetUserEntity(login, "UserList.FindNode");
            using var db = new AppDbContext();

            var nodeDb = db.FileSystemNodes
                .AsNoTracking()
                .FirstOrDefault(n => n.Name == nodeName && n.UserId == entity.Id);

            if (nodeDb == null) return null;

            var content = db.NoteContents
                .AsNoTracking()
                .FirstOrDefault(c => c.NodeId == nodeDb.Id)?.Content;

            return new FileSystemNode(nodeDb.Name,
                nodeDb.ItemType == "Folder" ? FileItemType.Folder : FileItemType.File)
            {
                Content = content
            };
        }

        public void ClearTree(string login)
        {
            var entity   = GetUserEntity(login, "UserList.ClearTree");
            using var db = new AppDbContext();

            var roots = db.FileSystemNodes
                .Where(n => n.UserId == entity.Id && n.ParentId == null)
                .ToList();

            foreach (var root in roots)
                DeleteNodeRecursive(db, root);

            db.SaveChanges();
        }

        /// <summary>
        /// Сохраняет текст заметки. Создаёт запись NoteContent, если её ещё нет.
        /// </summary>
        public void SaveNoteContent(string login, string nodeName, string content)
        {
            var entity   = GetUserEntity(login, "UserList.SaveNoteContent");
            using var db = new AppDbContext();

            var nodeDb = db.FileSystemNodes
                .Include(n => n.NoteContent)
                .FirstOrDefault(n => n.Name == nodeName && n.UserId == entity.Id);

            if (nodeDb == null) return;

            if (nodeDb.NoteContent == null)
                nodeDb.NoteContent = new NoteContentEntity { NodeId = nodeDb.Id, Content = content };
            else
                nodeDb.NoteContent.Content = content;

            db.SaveChanges();
        }

        public void RenameNode(string login, string oldName, string newName)
        {
            var entity   = GetUserEntity(login, "UserList.RenameNode");
            using var db = new AppDbContext();

            var nodeDb = db.FileSystemNodes
                .FirstOrDefault(n => n.Name == oldName && n.UserId == entity.Id);

            if (nodeDb == null) return;

            nodeDb.Name = newName;
            db.SaveChanges();
        }

        private static UserEntity GetUserEntity(string login, string location)
        {
            using var db = new AppDbContext();
            return db.Users.AsNoTracking().FirstOrDefault(u => u.Login == login)
                ?? throw MyExceptions.Navigation(
                    $"Пользователь «{login}» не найден", location);
        }

        private static FileSystemNodeEntity GetNodeEntity(
            string name, int userId, string location)
        {
            using var db = new AppDbContext();
            return db.FileSystemNodes
                .Include(n => n.Children)
                .FirstOrDefault(n => n.Name == name && n.UserId == userId)
                ?? throw MyExceptions.Navigation($"Узел «{name}» не найден", location);
        }

        private static int CountRoots(int userId)
        {
            using var db = new AppDbContext();
            return db.FileSystemNodes.Count(n => n.UserId == userId && n.ParentId == null);
        }

        private static FileSystemNode CreateNode(
            int userId, int? parentId, string name, FileItemType type, int sortOrder)
        {
            using var db = new AppDbContext();

            var entity = new FileSystemNodeEntity
            {
                UserId    = userId,
                ParentId  = parentId,
                Name      = name,
                ItemType  = type == FileItemType.Folder ? "Folder" : "File",
                SortOrder = sortOrder
            };

            db.FileSystemNodes.Add(entity);

            if (type == FileItemType.File)
                entity.NoteContent = new NoteContentEntity { Content = "" };

            db.SaveChanges();

            return new FileSystemNode(name, type);
        }

        /// <summary>
        /// Строит FileSystemTree из БД, восстанавливая Parent-ссылки в памяти.
        /// Контент заметок загружается сразу одним запросом.
        /// </summary>
        private static FileSystemTree LoadTree(int userId)
        {
            using var db = new AppDbContext();

            var allNodes = db.FileSystemNodes
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.SortOrder)
                .ToList();

            var nodeIds    = allNodes.Select(n => n.Id).ToList();
            var contentMap = db.NoteContents
                .AsNoTracking()
                .Where(c => nodeIds.Contains(c.NodeId))
                .ToDictionary(c => c.NodeId, c => c.Content);

            var nodeMap = allNodes.ToDictionary(
                n => n.Id,
                n => new FileSystemNode(n.Name,
                    n.ItemType == "Folder" ? FileItemType.Folder : FileItemType.File)
                {
                    Content = contentMap.GetValueOrDefault(n.Id)
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

        private static void DeleteNodeRecursive(AppDbContext db, FileSystemNodeEntity node)
        {
            var children = db.FileSystemNodes
                .Where(n => n.ParentId == node.Id)
                .ToList();

            foreach (var child in children)
                DeleteNodeRecursive(db, child);

            db.FileSystemNodes.Remove(node);
        }

        private static User MapUser(UserEntity entity, FileSystemTree tree) => new()
        {
            Login    = entity.Login,
            Password = entity.Password,
            Email    = entity.Email,
            Theme    = entity.Theme,
            Language = entity.Language,
            Tree     = tree
        };
    }

    public class User
    {
        public required string Login     { get; init; }
        public required string Password  { get; set; }
        public required string Email     { get; set; }
        public required string Theme     { get; set; }
        public required string Language  { get; set; }
        public FileSystemTree  Tree      { get; set; } = new();
    }
}