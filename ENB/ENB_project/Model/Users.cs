using System.IO;
using System.Text.Json;
using ENB_project.Controls;

namespace ENB_project
{
    /// <summary>
    /// Репозиторий пользователей. Загружает и сохраняет список в JSON,
    /// предоставляет методы для CRUD-операций над пользователями и их деревьями файлов.
    /// </summary>
    public class UserList
    {
        private List<User> _users = [];
        private readonly string _path;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
        };

        public UserList()
        {
            var basePath = AppContext.BaseDirectory;
            _path = Path.Combine(basePath, "app_data", "userList.json");
        }

        public void SaveJson()
        {
            try
            {
                var dir = Path.GetDirectoryName(_path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_users, _jsonOptions);
                File.WriteAllText(_path, json);
            }
            catch (Exception ex)
            {
                throw MyExceptions.IO("Не удалось сохранить данные пользователей", "UserList.SaveJson", ex);
            }
        }

        /// <summary>
        /// Загружает список пользователей из JSON и восстанавливает Parent-ссылки
        /// в деревьях файлов (они не сериализуются).
        /// </summary>
        public void LoadJson()
        {
            try
            {
                var json     = File.ReadAllText(_path);
                var restored = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions);

                _users = restored ?? throw MyExceptions.Data("Список пользователей пуст", "UserList.LoadJson");

                foreach (var user in _users)
                    RestoreParents(user.Tree.Roots, parent: null);
            }
            catch (FileNotFoundException ex)
            {
                throw MyExceptions.IO("Файл данных не найден", "UserList.LoadJson", ex);
            }
            catch (JsonException ex)
            {
                throw MyExceptions.Data("Некорректный формат JSON", "UserList.LoadJson", ex);
            }
        }

        /// <summary>
        /// Рекурсивно восстанавливает поле Parent у всех узлов дерева после десериализации.
        /// </summary>
        private static void RestoreParents(
            IEnumerable<FileSystemNode> nodes, FileSystemNode? parent)
        {
            foreach (var node in nodes)
            {
                node.Parent = parent;
                RestoreParents(node.Children, node);
            }
        }

        public User? GetUser(string username)
            => _users.Find(u => u?.Username == username);

        /// <summary>
        /// Добавляет нового пользователя. Возвращает false, если имя уже занято.
        /// </summary>
        public bool AddUser(string username, string password,
            string email, string theme, string language)
        {
            if (_users.Any(u => u?.Username == username))
                return false;

            _users.Add(new User
            {
                Username = username,
                Password = password,
                Email    = email,
                Theme    = theme,
                Language = language
            });
            return true;
        }

        public void DelUser(string name)
        {
            var user = _users.Find(u => u.Username == name);
            if (user != null)
                _users.Remove(user);
        }

        public bool EditUser(string name, string changedPropertyName, string changed)
        {
            if (GetUser(name) == null) return false;
            var index = _users.IndexOf(_users.FirstOrDefault(x => x.Username == name));

            switch (changedPropertyName)
            {
                case nameof(User.Password): _users[index].Password = changed; break;
                case nameof(User.Email):    _users[index].Email    = changed; break;
                case nameof(User.Theme):    _users[index].Theme    = changed; break;
                case nameof(User.Language): _users[index].Language = changed; break;
                default: return false;
            }
            return true;
        }

        public FileSystemTree GetTree(string username)
            => GetUser(username)?.Tree
               ?? throw MyExceptions.Navigation($"Пользователь «{username}» не найден", "UserList.GetTree");

        public FileSystemNode AddNodeToRoot(string username, string nodeName,
            FileItemType type = FileItemType.File)
            => GetUser(username)?.Tree.AddToRoot(nodeName, type)
               ?? throw MyExceptions.Navigation($"Пользователь «{username}» не найден", "UserList.AddNodeToRoot");

        public FileSystemNode AddNodeToFolder(string username,
            FileSystemNode parent, string nodeName,
            FileItemType type = FileItemType.File)
            => GetUser(username)?.Tree.AddChild(parent, nodeName, type)
               ?? throw MyExceptions.Navigation($"Пользователь «{username}» не найден", "UserList.AddNodeToFolder");

        public FileSystemNode InsertNode(string username,
            FileSystemNode parent, int index, string nodeName,
            FileItemType type = FileItemType.File)
            => GetUser(username)?.Tree.InsertChild(parent, index, nodeName, type)
               ?? throw MyExceptions.Navigation($"Пользователь «{username}» не найден", "UserList.InsertNode");

        public bool RemoveNode(string username, FileSystemNode node)
        {
            var user = GetUser(username)
                ?? throw MyExceptions.Navigation($"Пользователь «{username}» не найден", "UserList.RemoveNode");
            return user.Tree.Remove(node);
        }

        public void MoveNode(string username,
            FileSystemNode node, FileSystemNode? newParent)
        {
            var user = GetUser(username)
                ?? throw MyExceptions.Navigation($"Пользователь «{username}» не найден", "UserList.MoveNode");
            user.Tree.Move(node, newParent);
        }

        public FileSystemNode? FindNode(string username, string nodeName)
            => GetUser(username)?.Tree.FindByName(nodeName)
               ?? throw MyExceptions.Navigation($"Пользователь «{username}» не найден", "UserList.FindNode");

        public void ClearTree(string username)
        {
            var user = GetUser(username)
                ?? throw MyExceptions.Navigation($"Пользователь «{username}» не найден", "UserList.ClearTree");
            user.Tree.Clear();
        }
    }

    /// <summary>
    /// Модель пользователя. Хранит учётные данные, настройки и дерево файлов.
    /// </summary>
    public class User
    {
        public required string Username  { get; init; }
        public required string Password  { get; set; }
        public required string Email     { get; set; }
        public required string Theme     { get; set; }
        public required string Language  { get; set; }
        public FileSystemTree  Tree      { get; set; } = new();
    }
}