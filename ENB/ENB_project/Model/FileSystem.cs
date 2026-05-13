using ENB_project.Controls;

namespace ENB_project
{
    public class FileSystemNode
    {
        private string _name = string.Empty;

        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        public FileItemType ItemType { get; set; } = FileItemType.File;

        public bool IsFolder => ItemType == FileItemType.Folder;

        public string? Content       { get; set; }
        public int?    CategoryId    { get; set; }
        public string? CategoryColor { get; set; }
        public DateOnly EditTime { get; set; }
        public DateOnly CreateTime { get; set; }

        public List<FileSystemNode> Children { get; set; } = new();

        public FileSystemNode? Parent { get; internal set; }

        public FileSystemNode() { }

        public FileSystemNode(string name, FileItemType itemType = FileItemType.File)
        {
            Name     = name;
            ItemType = itemType;
        }
    }

    /// <summary>
    /// Дерево файловой системы пользователя. Управляет корневыми узлами и предоставляет
    /// методы для добавления, удаления, перемещения и обхода узлов.
    /// Является in-memory представлением — все изменения дополнительно персистируются через UserList.
    /// </summary>
    public class FileSystemTree
    {
        public List<FileSystemNode> Roots { get; set; } = new();

        public FileSystemNode AddToRoot(string name, FileItemType type = FileItemType.File)
        {
            var node = new FileSystemNode(name, type);
            Roots.Add(node);
            return node;
        }

        public FileSystemNode AddChild(FileSystemNode parent, string name,
            FileItemType type = FileItemType.File)
        {
            if (!parent.IsFolder)
                throw MyExceptions.Validation(
                    $"Узел «{parent.Name}» не является папкой", "FileSystemTree.AddChild");

            var node = new FileSystemNode(name, type) { Parent = parent };
            parent.Children.Add(node);
            return node;
        }

        public FileSystemNode InsertChild(FileSystemNode parent, int index,
            string name, FileItemType type = FileItemType.File)
        {
            if (!parent.IsFolder)
                throw MyExceptions.Validation(
                    $"Узел «{parent.Name}» не является папкой", "FileSystemTree.InsertChild");

            var node      = new FileSystemNode(name, type) { Parent = parent };
            int safeIndex = Math.Clamp(index, 0, parent.Children.Count);
            parent.Children.Insert(safeIndex, node);
            return node;
        }

        public FileSystemNode InsertToRoot(int index, string name,
            FileItemType type = FileItemType.File)
        {
            var node      = new FileSystemNode(name, type);
            int safeIndex = Math.Clamp(index, 0, Roots.Count);
            Roots.Insert(safeIndex, node);
            return node;
        }

        public bool Remove(FileSystemNode node)
        {
            if (node.Parent != null)
            {
                bool removed = node.Parent.Children.Remove(node);
                if (removed) node.Parent = null;
                return removed;
            }

            return Roots.Remove(node);
        }

        public void Clear()
        {
            foreach (var root in Roots)
                ClearRecursive(root);

            Roots.Clear();
        }

        private static void ClearRecursive(FileSystemNode node)
        {
            foreach (var child in node.Children)
            {
                child.Parent = null;
                ClearRecursive(child);
            }
        }

        public void Move(FileSystemNode node, FileSystemNode? newParent)
        {
            Remove(node);

            if (newParent == null)
            {
                node.Parent = null;
                Roots.Add(node);
            }
            else
            {
                if (!newParent.IsFolder)
                    throw MyExceptions.Validation(
                        $"Узел «{newParent.Name}» не является папкой", "FileSystemTree.Move");

                node.Parent = newParent;
                newParent.Children.Add(node);
            }
        }

        public FileSystemNode? FindByName(string name)
            => FindByNameRecursive(Roots, name);

        private static FileSystemNode? FindByNameRecursive(
            IEnumerable<FileSystemNode> nodes, string name)
        {
            foreach (var node in nodes)
            {
                if (string.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase))
                    return node;

                var found = FindByNameRecursive(node.Children, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Обходит всё дерево в глубину. Второй параметр action — глубина узла (0 для корневых).
        /// </summary>
        public void Traverse(Action<FileSystemNode, int> action)
            => TraverseRecursive(Roots, action, depth: 0);

        private static void TraverseRecursive(IEnumerable<FileSystemNode> nodes,
            Action<FileSystemNode, int> action, int depth)
        {
            foreach (var node in nodes)
            {
                action(node, depth);
                TraverseRecursive(node.Children, action, depth + 1);
            }
        }
    }
}