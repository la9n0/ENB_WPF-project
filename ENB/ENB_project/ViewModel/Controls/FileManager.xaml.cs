using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ENB_project.Controls
{
    public enum FileItemType { Folder, File }

    public class FileManagerItem : INotifyPropertyChanged
    {
        private string  _name          = string.Empty;
        private bool    _isExpanded    = false;
        private bool    _isSelected    = false;
        private bool    _isDragOver    = false;
        private string? _categoryColor = null;
        private FileItemType _itemType = FileItemType.File;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public FileItemType ItemType
        {
            get => _itemType;
            set { _itemType = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsFolder)); }
        }

        public bool IsFolder => _itemType == FileItemType.Folder;

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public bool IsDragOver
        {
            get => _isDragOver;
            set { _isDragOver = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// HEX-цвет категории (#RRGGBB) или null если категории нет.
        /// </summary>
        public string? CategoryColor
        {
            get => _categoryColor;
            set
            {
                _categoryColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CategoryBrush));
                OnPropertyChanged(nameof(HasCategory));
            }
        }

        public bool HasCategory => _categoryColor != null;

        public SolidColorBrush? CategoryBrush
        {
            get
            {
                if (_categoryColor == null) return null;
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(_categoryColor);
                    return new SolidColorBrush(color);
                }
                catch { return null; }
            }
        }

        public ObservableCollection<FileManagerItem> Children { get; set; } = new();

        public FileManagerItem() { }

        public FileManagerItem(string name, FileItemType itemType = FileItemType.File)
        {
            Name     = name;
            ItemType = itemType;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class FileManagerFlatItem
    {
        private const int IndentStep = 15;

        public FileManagerItem Item       { get; }
        public int             Depth      { get; }
        public double          IndentWidth => Depth * IndentStep;
        public int             FlatIndex  { get; set; }

        public FileManagerFlatItem(FileManagerItem item, int depth, int flatIndex = 0)
        {
            Item      = item;
            Depth     = depth;
            FlatIndex = flatIndex;
        }
    }

    public class FileManagerItemSelectedEventArgs : RoutedEventArgs
    {
        public FileManagerItem Item          { get; }
        public int             SelectedIndex { get; }

        public FileManagerItemSelectedEventArgs(
            RoutedEvent routedEvent, FileManagerItem item, int selectedIndex)
            : base(routedEvent)
        {
            Item          = item;
            SelectedIndex = selectedIndex;
        }
    }

    public class FileManagerItemMovedEventArgs : RoutedEventArgs
    {
        public FileManagerItem  Dragged { get; }
        public FileManagerItem? Target  { get; }

        public FileManagerItemMovedEventArgs(
            RoutedEvent routedEvent, FileManagerItem dragged, FileManagerItem? target)
            : base(routedEvent)
        {
            Dragged = dragged;
            Target  = target;
        }
    }

    public partial class FileManager : UserControl
    {
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items),
                typeof(ObservableCollection<FileManagerItem>), typeof(FileManager),
                new PropertyMetadata(null, OnItemsChanged));

        public ObservableCollection<FileManagerItem> Items
        {
            get => (ObservableCollection<FileManagerItem>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex),
                typeof(int), typeof(FileManager), new PropertyMetadata(-1));

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            private set => SetValue(SelectedIndexProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem),
                typeof(FileManagerItem), typeof(FileManager), new PropertyMetadata(null));

        public FileManagerItem? SelectedItem
        {
            get => (FileManagerItem?)GetValue(SelectedItemProperty);
            private set => SetValue(SelectedItemProperty, value);
        }

        public static readonly RoutedEvent ItemSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(ItemSelected), RoutingStrategy.Bubble,
                typeof(EventHandler<FileManagerItemSelectedEventArgs>), typeof(FileManager));

        public event EventHandler<FileManagerItemSelectedEventArgs> ItemSelected
        {
            add    => AddHandler(ItemSelectedEvent, value);
            remove => RemoveHandler(ItemSelectedEvent, value);
        }

        public static readonly RoutedEvent ItemMovedEvent =
            EventManager.RegisterRoutedEvent(nameof(ItemMoved), RoutingStrategy.Bubble,
                typeof(EventHandler<FileManagerItemMovedEventArgs>), typeof(FileManager));

        public event EventHandler<FileManagerItemMovedEventArgs> ItemMoved
        {
            add    => AddHandler(ItemMovedEvent, value);
            remove => RemoveHandler(ItemMovedEvent, value);
        }

        private FileManagerItem? _dragItem;
        private Point            _dragStartPoint;

        public ObservableCollection<FileManagerFlatItem> FlatItems { get; }
            = new ObservableCollection<FileManagerFlatItem>();

        public FileManager() => InitializeComponent();

        private static void OnItemsChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fm = (FileManager)d;

            if (e.OldValue is ObservableCollection<FileManagerItem> oldCol)
                oldCol.CollectionChanged -= fm.OnRootCollectionChanged;

            if (e.NewValue is ObservableCollection<FileManagerItem> newCol)
                newCol.CollectionChanged += fm.OnRootCollectionChanged;

            fm.RebuildFlatList();
        }

        private void OnRootCollectionChanged(
            object? sender, NotifyCollectionChangedEventArgs e) => RebuildFlatList();

        private void RebuildFlatList()
        {
            FlatItems.Clear();
            if (Items == null) return;
            int index = 0;
            AppendItems(Items, depth: 0, ref index);
        }

        private void AppendItems(
            IEnumerable<FileManagerItem> items, int depth, ref int index)
        {
            foreach (var item in items)
            {
                FlatItems.Add(new FileManagerFlatItem(item, depth, index));
                index++;

                if (item.IsFolder && item.IsExpanded && item.Children.Count > 0)
                    AppendItems(item.Children, depth + 1, ref index);
            }
        }

        private void Row_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not FileManagerFlatItem flatItem) return;

            _dragStartPoint = e.GetPosition(null);
            _dragItem = flatItem.Item.IsFolder ? null : flatItem.Item;

            var item = flatItem.Item;

            if (SelectedItem != null) SelectedItem.IsSelected = false;

            item.IsSelected = true;
            SelectedItem    = item;
            SelectedIndex   = flatItem.FlatIndex;

            if (item.IsFolder)
            {
                item.IsExpanded = !item.IsExpanded;
                RebuildFlatList();
            }

            RaiseEvent(new FileManagerItemSelectedEventArgs(
                ItemSelectedEvent, item, SelectedIndex) { Source = this });

            e.Handled = true;
        }

        private void Row_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_dragItem == null) return;

            var pos  = e.GetPosition(null);
            var diff = _dragStartPoint - pos;

            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            var captured = _dragItem;
            _dragItem = null;

            var data = new DataObject("FileManagerItem", captured);
            DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
        }

        private void Row_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FileManagerItem"))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            if (sender is not Border border) return;
            if (border.DataContext is not FileManagerFlatItem flatItem) return;

            var dragged = (FileManagerItem)e.Data.GetData("FileManagerItem");

            if (dragged == flatItem.Item)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            flatItem.Item.IsDragOver = true;
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void Row_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is FileManagerFlatItem flatItem)
                flatItem.Item.IsDragOver = false;
        }

        private void Row_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FileManagerItem")) return;
            if (sender is not Border border) return;
            if (border.DataContext is not FileManagerFlatItem flatItem) return;

            var dragged = (FileManagerItem)e.Data.GetData("FileManagerItem");
            flatItem.Item.IsDragOver = false;

            if (dragged == flatItem.Item) return;

            RaiseEvent(new FileManagerItemMovedEventArgs(
                ItemMovedEvent, dragged, flatItem.Item) { Source = this });

            e.Handled = true;
        }

        private void Control_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FileManagerItem")) return;

            var hit = VisualTreeHelper.HitTest(this, e.GetPosition(this));
            if (hit != null && FindParent<Border>(hit.VisualHit)?.DataContext
                is FileManagerFlatItem) return;

            var dragged = (FileManagerItem)e.Data.GetData("FileManagerItem");

            RaiseEvent(new FileManagerItemMovedEventArgs(
                ItemMovedEvent, dragged, null) { Source = this });

            e.Handled = true;
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typed) return typed;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        public void Expand(FileManagerItem folder)
        {
            if (!folder.IsFolder) return;
            folder.IsExpanded = true;
            RebuildFlatList();
        }

        public void Collapse(FileManagerItem folder)
        {
            if (!folder.IsFolder) return;
            folder.IsExpanded = false;
            RebuildFlatList();
        }

        public void CollapseAll()
        {
            if (Items == null) return;
            SetExpandedRecursive(Items, false);
            RebuildFlatList();
        }

        public void LoadFromTree(FileSystemTree tree)
        {
            if (tree == null) throw new ArgumentNullException(nameof(tree));

            var root = new ObservableCollection<FileManagerItem>();
            foreach (var node in tree.Roots)
                root.Add(ConvertNode(node));

            Items = root;
        }

        public FileManagerItem? FindItemByName(string name)
        {
            foreach (var flat in FlatItems)
                if (flat.Item.Name == name)
                    return flat.Item;
            return null;
        }

        private static FileManagerItem ConvertNode(FileSystemNode node)
        {
            var item = new FileManagerItem(node.Name, node.ItemType)
            {
                CategoryColor = node.CategoryColor
            };

            foreach (var child in node.Children)
                item.Children.Add(ConvertNode(child));

            return item;
        }

        private static void SetExpandedRecursive(
            IEnumerable<FileManagerItem> items, bool expanded)
        {
            foreach (var item in items)
            {
                if (!item.IsFolder) continue;
                item.IsExpanded = expanded;
                if (item.Children.Count > 0)
                    SetExpandedRecursive(item.Children, expanded);
            }
        }
    }
}