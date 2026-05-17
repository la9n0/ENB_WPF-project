using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ENB_project.Controls;

namespace ENB_project
{
    public class NoteTab : INotifyPropertyChanged
    {
        private string _title    = "Новая вкладка";
        private bool   _isActive = false;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public string? NoteName { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MainWindow : Window
    {
        private readonly UserList _userList = new();
        private User _user = null!;

        private readonly ObservableCollection<NoteTab> _tabs = new();
        private NoteTab? _activeTab;

        private bool _isEditMode     = false;
        private bool _isUserMenuOpen = false;
        private bool _isSearchOpen   = false;

        private FileManagerItem? _pendingDeleteItem;

        private readonly System.Windows.Threading.DispatcherTimer _reminderTimer = new();
        private readonly System.Windows.Threading.DispatcherTimer _syncTimer     = new();

        private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(30);

        public MainWindow(string login)
        {
            InitializeComponent();
            TabStrip.ItemsSource = _tabs;
            LoadUser(login);
            BindShortcuts();
            AddTab();

            _reminderTimer.Interval = TimeSpan.FromSeconds(1);
            _reminderTimer.Tick    += ReminderTimer_Tick;
            _reminderTimer.Start();

            _syncTimer.Interval = SyncInterval;
            _syncTimer.Tick    += SyncTimer_Tick;
            _syncTimer.Start();

            EnbFunctional.LanguageChanged      += OnLanguageChanged;
            MyExceptions.CriticalErrorOccurred += OnCriticalError;

            Closed += (_, _) =>
            {
                EnbFunctional.LanguageChanged      -= OnLanguageChanged;
                MyExceptions.CriticalErrorOccurred -= OnCriticalError;
                _reminderTimer.Stop();
                _syncTimer.Stop();
            };
        }

        private void OnLanguageChanged(string lang)
        {
            var newTabText = (string)TryFindResource("MainNewTab") ?? "Новая вкладка";
            var editText   = (string)TryFindResource("MainBtnEdit") ?? "Редактировать";

            foreach (var tab in _tabs.Where(t => t.NoteName == null))
                tab.Title = newTabText;

            if (_activeTab?.NoteName == null)
                PageTitle.Text = newTabText;

            if (!_isEditMode)
                EditSaveBtn.Content = editText;
        }

        private void OnCriticalError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                CriticalBannerText.Text  = message;
                CriticalBanner.Visibility = Visibility.Visible;
            });
        }

        private void CriticalBannerClose_Click(object sender, RoutedEventArgs e)
            => CriticalBanner.Visibility = Visibility.Collapsed;

        private void LoadUser(string login)
        {
            try
            {
                _user = _userList.GetUser(login)
                        ?? throw MyExceptions.Navigation(
                            $"Пользователь «{login}» не найден", "MainWindow.LoadUser");
            }
            catch (MyExceptions)
            {
                _userList.AddUser(login, "", "", "Dark", "ru");
                _user = _userList.GetUser(login)!;
            }
            catch (Exception ex)
            {
                MyExceptions.Critical("Непредвиденная ошибка при загрузке пользователя",
                    "MainWindow.LoadUser", ex);
            }

            EnbFunctional.ApplyTheme(_user.Theme);
            EnbFunctional.ApplyLanguage(_user.Language);

            MyFileManager.LoadFromTree(_user.Tree);
            UserMenuControl.Content = new UserMenu(login);
        }

        private void BindShortcuts()
        {
            var newNote   = new RoutedCommand();
            var newFolder = new RoutedCommand();
            var save      = new RoutedCommand();
            var deleteCmd = new RoutedCommand();
            var renameCmd = new RoutedCommand();

            CommandBindings.Add(new CommandBinding(newNote,   (_, _) => CreateNote()));
            CommandBindings.Add(new CommandBinding(newFolder, (_, _) => CreateFolder()));
            CommandBindings.Add(new CommandBinding(save,      (_, _) => SaveIfEditing()));
            CommandBindings.Add(new CommandBinding(deleteCmd, (_, _) => DeleteSelected()));
            CommandBindings.Add(new CommandBinding(renameCmd, (_, _) => RenameSelected()));

            InputBindings.Add(new KeyBinding(newNote,
                new KeyGesture(Key.N, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(newFolder,
                new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift)));
            InputBindings.Add(new KeyBinding(save,
                new KeyGesture(Key.S, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(deleteCmd,
                new KeyGesture(Key.Delete, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(renameCmd,
                new KeyGesture(Key.F2)));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this)
                as System.Windows.Interop.HwndSource;
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam,
            ref bool handled)
        {
            if (msg == 0x0084)
            {
                var pos   = new System.Drawing.Point(
                    (int)lParam & 0xFFFF, (int)lParam >> 16);
                var local = PointFromScreen(new Point(pos.X, pos.Y));
                const int edge = 6;

                bool left   = local.X <= edge;
                bool right  = local.X >= ActualWidth  - edge;
                bool top    = local.Y <= edge;
                bool bottom = local.Y >= ActualHeight - edge;

                if (top    && left)  { handled = true; return (IntPtr)13; }
                if (top    && right) { handled = true; return (IntPtr)14; }
                if (bottom && left)  { handled = true; return (IntPtr)16; }
                if (bottom && right) { handled = true; return (IntPtr)17; }
                if (left)            { handled = true; return (IntPtr)10; }
                if (right)           { handled = true; return (IntPtr)11; }
                if (top)             { handled = true; return (IntPtr)12; }
                if (bottom)          { handled = true; return (IntPtr)15; }
            }
            return IntPtr.Zero;
        }

        private void ReminderTimer_Tick(object? sender, EventArgs e)
        {
            var triggered = _userList.GetAndMarkTriggeredReminders(_user.Login);
            if (triggered.Count == 0) return;

            _user = _userList.GetUser(_user.Login) ?? _user;
            MyFileManager.LoadFromTree(_user.Tree);

            if (_isSearchOpen)
                MySearchPanel.SetTree(_user.Tree);

            foreach (var reminder in triggered)
            {
                var notif = new ReminderNotification(reminder);
                notif.Closed += (_, _) =>
                {
                    if (notif.OpenNote)
                        Dispatcher.Invoke(() => OpenNoteInTab(notif.NoteName));
                };
                notif.Show();
            }
        }

        private void SyncTimer_Tick(object? sender, EventArgs e)
        {
            if (_isEditMode) return;

            var freshTree = _userList.LoadTreeForUser(_user.Login);

            var currentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            freshTree.Traverse((node, _) => currentNames.Add(node.Name));

            var oldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _user.Tree.Traverse((node, _) => oldNames.Add(node.Name));

            if (currentNames.SetEquals(oldNames)) return;

            _user.Tree = freshTree;

            MyFileManager.LoadFromTree(_user.Tree);

            if (_isSearchOpen)
                MySearchPanel.SetTree(_user.Tree);

            if (_activeTab?.NoteName != null && !currentNames.Contains(_activeTab.NoteName))
            {
                _activeTab.NoteName = null;
                _activeTab.Title    = (string)(TryFindResource("MainNewTab") ?? "Новая вкладка");
                ShowEmpty();
            }
        }

        private void FoldersButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSearchOpen) CloseSearch();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSearchOpen) CloseSearch();
            else OpenSearch();
        }

        private void OpenSearch()
        {
            _isSearchOpen                 = true;
            FileManagerToolbar.Visibility = Visibility.Collapsed;
            MyFileManager.Visibility      = Visibility.Collapsed;
            MySearchPanel.Visibility      = Visibility.Visible;
            MySearchPanel.SetTree(_user.Tree);
        }

        private void CloseSearch()
        {
            _isSearchOpen                 = false;
            MySearchPanel.Visibility      = Visibility.Collapsed;
            MyFileManager.Visibility      = Visibility.Visible;
            FileManagerToolbar.Visibility = Visibility.Visible;
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUserMenuOpen) CloseUserMenu();
            else OpenUserMenu();
        }

        private void OpenUserMenu()
        {
            _isUserMenuOpen = true;

            NoteEditor.Visibility     = Visibility.Collapsed;
            EmptyState.Visibility     = Visibility.Collapsed;
            EditSaveBtn.Visibility    = Visibility.Collapsed;
            CategoryBtn.Visibility    = Visibility.Collapsed;
            ReminderBtn.Visibility    = Visibility.Collapsed;
            PageTitle.Visibility      = Visibility.Collapsed;
            PageTitleEdit.Visibility  = Visibility.Collapsed;
            NoteEditTime.Visibility   = Visibility.Collapsed;
            NoteCreateTime.Visibility = Visibility.Collapsed;

            UserMenuControl.Visibility = Visibility.Visible;
        }

        private void CloseUserMenu()
        {
            _user = _userList.GetUser(_user.Login) ?? _user;

            _isUserMenuOpen            = false;
            UserMenuControl.Visibility = Visibility.Collapsed;
            PageTitle.Visibility       = Visibility.Visible;

            if (_activeTab?.NoteName != null)
            {
                var node = _user.Tree.FindByName(_activeTab.NoteName);
                if (node != null) { ShowNote(node); return; }
            }

            ShowEmpty();
        }

        private NoteTab AddTab(string? noteName = null)
        {
            var newTabText = (string)TryFindResource("MainNewTab") ?? "Новая вкладка";
            var tab = new NoteTab { Title = noteName ?? newTabText, NoteName = noteName };
            _tabs.Add(tab);
            ActivateTab(tab);
            return tab;
        }

        private void ActivateTab(NoteTab tab)
        {
            if (_activeTab != null) _activeTab.IsActive = false;
            _activeTab   = tab;
            tab.IsActive = true;

            if (_isUserMenuOpen) { CloseUserMenu(); return; }

            if (tab.NoteName != null)
            {
                var node = _user.Tree.FindByName(tab.NoteName);
                if (node != null) ShowNote(node);
                else ShowEmpty();
            }
            else ShowEmpty();
        }

        private void Tab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is NoteTab tab)
                ActivateTab(tab);
        }

        private void NewTab_Click(object sender, RoutedEventArgs e) => AddTab();

        private void TabClose_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is NoteTab tab)
                CloseTab(tab);
        }

        private void CloseTab(NoteTab tab)
        {
            if (tab == _activeTab && _isEditMode)
                SaveActiveNote();

            var idx = _tabs.IndexOf(tab);
            _tabs.Remove(tab);

            if (_tabs.Count == 0) { AddTab(); return; }
            if (tab == _activeTab)
                ActivateTab(_tabs[Math.Clamp(idx, 0, _tabs.Count - 1)]);
        }

        private void MyFileManager_ItemSelected(
            object sender, FileManagerItemSelectedEventArgs e)
        {
            if (e.Item.IsFolder) return;
            if (_isUserMenuOpen) CloseUserMenu();
            OpenNoteInTab(e.Item.Name);
        }

        private void MySearchPanel_ItemSelected(
            object sender, SearchPanelItemSelectedEventArgs e)
        {
            if (_isUserMenuOpen) CloseUserMenu();
            OpenNoteInTab(e.NoteName);
        }

        private void OpenNoteInTab(string noteName)
        {
            var existing = _tabs.FirstOrDefault(t => t.NoteName == noteName);
            if (existing != null) { ActivateTab(existing); return; }

            if (_activeTab?.NoteName == null)
            {
                _activeTab!.NoteName = noteName;
                _activeTab.Title     = noteName;
                var node = _user.Tree.FindByName(noteName);
                if (node != null) ShowNote(node);
            }
            else AddTab(noteName);
        }

        private void ShowNote(FileSystemNode node)
        {
            ExitEditMode(save: false);

            PageTitle.Text        = node.Name;
            NoteEditor.Text       = node.Content ?? string.Empty;
            NoteEditor.IsReadOnly = true;

            NoteCreateTime.Text = node.CreateTime == default
                ? string.Empty
                : node.CreateTime.ToString();

            NoteEditTime.Text = node.EditTime == default
                ? string.Empty
                : node.EditTime.ToString();

            UserMenuControl.Visibility = Visibility.Collapsed;
            EmptyState.Visibility      = Visibility.Collapsed;
            NoteEditor.Visibility      = Visibility.Visible;
            PageTitle.Visibility       = Visibility.Visible;
            EditSaveBtn.Visibility     = Visibility.Visible;
            CategoryBtn.Visibility     = Visibility.Visible;
            ReminderBtn.Visibility     = Visibility.Visible;
            NoteCreateTime.Visibility  = Visibility.Visible;
            NoteEditTime.Visibility    = Visibility.Visible;
            EditSaveBtn.Content        = TryFindResource("MainBtnEdit") ?? "Редактировать";

            UpdateReminderButton(node);

            if (_activeTab != null) _activeTab.Title = node.Name;
        }

        private void ShowEmpty()
        {
            ExitEditMode(save: false);
            PageTitle.Text  = (string)(TryFindResource("MainNewTab") ?? "Новая вкладка");
            NoteEditor.Text = string.Empty;

            UserMenuControl.Visibility = Visibility.Collapsed;
            EmptyState.Visibility      = Visibility.Visible;
            NoteEditor.Visibility      = Visibility.Collapsed;
            PageTitle.Visibility       = Visibility.Visible;
            EditSaveBtn.Visibility     = Visibility.Collapsed;
            CategoryBtn.Visibility     = Visibility.Collapsed;
            ReminderBtn.Visibility     = Visibility.Collapsed;
            NoteCreateTime.Visibility  = Visibility.Collapsed;
            NoteEditTime.Visibility    = Visibility.Collapsed;
        }

        private void Reminder_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab?.NoteName == null) return;

            var node = _user.Tree.FindByName(_activeTab.NoteName);
            if (node == null) return;

            Reminder? existing = node.ReminderId.HasValue
                ? _userList.GetReminder(_user.Login, _activeTab.NoteName)
                : null;

            var dialog = new ReminderPicker(existing);
            dialog.Owner = this;
            if (dialog.ShowDialog() != true) return;

            if (dialog.Deleted)
            {
                _userList.DeleteReminder(_user.Login, _activeTab.NoteName);
                node.ReminderId        = null;
                node.ReminderAt        = null;
                node.ReminderNote      = null;
                node.ReminderTriggered = false;

                var item = MyFileManager.FindItemByName(_activeTab.NoteName);
                if (item != null) item.HasActiveReminder = false;
            }
            else if (dialog.Saved)
            {
                var reminder = _userList.SetReminder(
                    _user.Login, _activeTab.NoteName,
                    dialog.SelectedDateTime, dialog.NoteText);

                node.ReminderId        = reminder.Id;
                node.ReminderAt        = reminder.RemindAt;
                node.ReminderNote      = reminder.Note;
                node.ReminderTriggered = false;

                var item = MyFileManager.FindItemByName(_activeTab.NoteName);
                if (item != null) item.HasActiveReminder = false;
            }

            UpdateReminderButton(node);
        }

        private void UpdateReminderButton(FileSystemNode node)
        {
            if (node.ReminderId.HasValue)
            {
                ReminderBtn.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E5C07B"));
            }
            else
            {
                ReminderBtn.ClearValue(ForegroundProperty);
            }
        }

        private void Category_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab?.NoteName == null) return;

            var node = _user.Tree.FindByName(_activeTab.NoteName);
            if (node == null) return;

            var categories = _userList.GetCategories(_user.Login);
            var dialog     = new CategoryPicker(categories, node.CategoryId, _user.Login, _userList);

            dialog.Owner = this;
            if (dialog.ShowDialog() != true) return;

            var selectedId    = dialog.SelectedCategoryId;
            var selectedColor = dialog.SelectedCategoryColor;

            node.CategoryId    = selectedId;
            node.CategoryColor = selectedColor;

            _userList.SetNodeCategory(_user.Login, _activeTab.NoteName, selectedId);

            MyFileManager.LoadFromTree(_user.Tree);

            if (_isSearchOpen)
                MySearchPanel.SetTree(_user.Tree);
        }

        private void PageTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_activeTab?.NoteName == null) return;
            if (_isUserMenuOpen) return;
            if (!_isEditMode) return;

            PageTitle.Visibility     = Visibility.Collapsed;
            PageTitleEdit.Text       = PageTitle.Text;
            PageTitleEdit.Visibility = Visibility.Visible;
            PageTitleEdit.Focus();
            PageTitleEdit.SelectAll();
        }

        private void PageTitleEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)  CommitTitleEdit();
            if (e.Key == Key.Escape) CancelTitleEdit();
        }

        private void PageTitleEdit_LostFocus(object sender, RoutedEventArgs e)
            => CommitTitleEdit();

        private void CommitTitleEdit()
        {
            if (PageTitleEdit.Visibility != Visibility.Visible) return;

            var oldName = _activeTab?.NoteName;
            var newName = PageTitleEdit.Text.Trim();

            if (!string.IsNullOrEmpty(newName) && oldName != null && oldName != newName)
            {
                var resolvedName = GetUniqueName(newName, excludeName: oldName);
                var node         = _user.Tree.FindByName(oldName);

                if (node != null)
                {
                    node.Name = resolvedName;
                    _userList.RenameNode(_user.Login, oldName, resolvedName);

                    foreach (var tab in _tabs.Where(t => t.NoteName == oldName))
                    {
                        tab.NoteName = resolvedName;
                        tab.Title    = resolvedName;
                    }

                    PageTitle.Text = resolvedName;
                    MyFileManager.LoadFromTree(_user.Tree);
                }
            }

            PageTitleEdit.Visibility = Visibility.Collapsed;
            PageTitle.Visibility     = Visibility.Visible;
        }

        private void CancelTitleEdit()
        {
            PageTitleEdit.Visibility = Visibility.Collapsed;
            PageTitle.Visibility     = Visibility.Visible;
        }

        private void EditSave_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditMode) EnterViewMode();
            else EnterEditMode();
        }

        private void EnterEditMode()
        {
            _isEditMode           = true;
            NoteEditor.IsReadOnly = false;
            EditSaveBtn.Content   = TryFindResource("MainBtnSave") ?? "Сохранить";
            NoteEditor.Focus();
        }

        private void EnterViewMode()
        {
            SaveActiveNote();
            ExitEditMode(save: false);
        }

        private void ExitEditMode(bool save)
        {
            if (save) SaveActiveNote();
            _isEditMode           = false;
            NoteEditor.IsReadOnly = true;
            EditSaveBtn.Content   = TryFindResource("MainBtnEdit") ?? "Редактировать";
        }

        private void SaveIfEditing()
        {
            if (_isEditMode) EnterViewMode();
        }

        private void SaveActiveNote()
        {
            if (_activeTab?.NoteName == null) return;

            var node = _user.Tree.FindByName(_activeTab.NoteName);
            if (node == null) return;

            node.Content  = NoteEditor.Text;
            node.EditTime = DateOnly.FromDateTime(DateTime.Now);

            _ = _userList.SaveNoteContentAsync(_user.Login, _activeTab.NoteName, NoteEditor.Text);

            NoteEditTime.Text = node.EditTime.ToString();
        }

        private void MyFileManager_ItemMoved(
            object sender, FileManagerItemMovedEventArgs e)
        {
            if (e.Dragged.IsFolder) return;

            var draggedNode = _user.Tree.FindByName(e.Dragged.Name);
            if (draggedNode == null) return;

            FileSystemNode? targetNode = null;
            if (e.Target != null)
            {
                targetNode = _user.Tree.FindByName(e.Target.Name);
                if (targetNode != null && !targetNode.IsFolder)
                    targetNode = targetNode.Parent;
            }

            _user.Tree.Move(draggedNode, targetNode);
            _userList.MoveNode(_user.Login, draggedNode, targetNode);

            var currentNoteName = _activeTab?.NoteName;
            MyFileManager.LoadFromTree(_user.Tree);

            if (currentNoteName != null)
            {
                var item = MyFileManager.FindItemByName(currentNoteName);
                if (item != null) item.IsSelected = true;
            }
        }

        private string GetUniqueName(string baseName, string? excludeName = null)
        {
            var existing = _user.Tree.FindByName(baseName);
            if (existing == null || string.Equals(existing.Name, excludeName, StringComparison.OrdinalIgnoreCase))
                return baseName;

            int counter = 1;
            while (true)
            {
                var candidate = $"{baseName} {counter}";
                var found     = _user.Tree.FindByName(candidate);
                if (found == null || string.Equals(found.Name, excludeName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
                counter++;
            }
        }

        private void CreateNote()   => CreateItem(FileItemType.File);
        private void CreateFolder() => CreateItem(FileItemType.Folder);

        private void CreateItem(FileItemType type)
        {
            var baseName = type == FileItemType.Folder
                ? (string)(TryFindResource("MainNewFolderName") ?? "Новая папка")
                : (string)(TryFindResource("MainNewNoteName")   ?? "Новая заметка");

            var name = GetUniqueName(baseName);

            var selected = MyFileManager.SelectedItem;
            FileSystemNode newNode;

            if (selected == null)
            {
                newNode = _userList.AddNodeToRoot(_user.Login, name, type);
                _user.Tree.Roots.Add(newNode);
            }
            else if (selected.IsFolder)
            {
                var parentNode = _user.Tree.FindByName(selected.Name);
                if (parentNode == null)
                {
                    newNode = _userList.AddNodeToRoot(_user.Login, name, type);
                    _user.Tree.Roots.Add(newNode);
                }
                else
                {
                    newNode = _userList.AddNodeToFolder(_user.Login, parentNode, name, type);
                    parentNode.Children.Add(newNode);
                    newNode.Parent = parentNode;
                    MyFileManager.Expand(selected);
                }
            }
            else
            {
                var fileNode   = _user.Tree.FindByName(selected.Name);
                var parentNode = fileNode?.Parent;

                if (parentNode != null)
                {
                    newNode = _userList.AddNodeToFolder(_user.Login, parentNode, name, type);
                    parentNode.Children.Add(newNode);
                    newNode.Parent = parentNode;
                }
                else
                {
                    newNode = _userList.AddNodeToRoot(_user.Login, name, type);
                    _user.Tree.Roots.Add(newNode);
                }
            }

            MyFileManager.LoadFromTree(_user.Tree);

            var createdItem = MyFileManager.FindItemByName(name);
            if (createdItem != null)
                BeginRename(createdItem, newNode);
        }

        private void DeleteSelected()
        {
            var selected = MyFileManager.SelectedItem;
            if (selected == null) return;

            _pendingDeleteItem = selected;

            DeleteConfirmText.Text = string.Format(
                (string)(TryFindResource("MainDeleteConfirm") ?? "Удалить «{0}»?"),
                selected.Name);

            DeleteConfirmPanel.Visibility = Visibility.Visible;
        }

        private void DeleteConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;

            if (_pendingDeleteItem == null) return;

            var node = _user.Tree.FindByName(_pendingDeleteItem.Name);
            if (node == null) { _pendingDeleteItem = null; return; }

            _user.Tree.Remove(node);
            _userList.RemoveNode(_user.Login, node);

            foreach (var tab in _tabs.Where(t => t.NoteName == _pendingDeleteItem.Name).ToList())
                CloseTab(tab);

            MyFileManager.LoadFromTree(_user.Tree);
            _pendingDeleteItem = null;
        }

        private void DeleteConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;
            _pendingDeleteItem = null;
        }

        private void RenameSelected()
        {
            var selected = MyFileManager.SelectedItem;
            if (selected == null) return;

            var node = _user.Tree.FindByName(selected.Name);
            if (node == null) return;

            BeginRename(selected, node);
        }

        private void BeginRename(FileManagerItem item, FileSystemNode node)
        {
            RenameBox.Text       = item.Name;
            RenameBox.Tag        = (item, node);
            RenameBox.Visibility = Visibility.Visible;
            RenameBox.Focus();
            RenameBox.SelectAll();
        }

        private void RenameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)  CommitRename();
            if (e.Key == Key.Escape) CancelRename();
        }

        private void RenameBox_LostFocus(object sender, RoutedEventArgs e)
            => CommitRename();

        private void CommitRename()
        {
            if (RenameBox.Visibility != Visibility.Visible) return;
            if (RenameBox.Tag is not (FileManagerItem item, FileSystemNode node)) return;

            var oldName = node.Name;
            var newName = RenameBox.Text.Trim();

            if (!string.IsNullOrEmpty(newName) && oldName != newName)
            {
                var resolvedName = GetUniqueName(newName, excludeName: oldName);

                _userList.RenameNode(_user.Login, oldName, resolvedName);
                node.Name = resolvedName;
                item.Name = resolvedName;

                foreach (var tab in _tabs.Where(t => t.NoteName == oldName))
                {
                    tab.NoteName = resolvedName;
                    tab.Title    = resolvedName;
                }

                if (_activeTab?.NoteName == resolvedName)
                    PageTitle.Text = resolvedName;
            }

            RenameBox.Visibility = Visibility.Collapsed;
        }

        private void CancelRename()
        {
            RenameBox.Visibility = Visibility.Collapsed;

            var newNoteName   = (string)(TryFindResource("MainNewNoteName")   ?? "Новая заметка");
            var newFolderName = (string)(TryFindResource("MainNewFolderName") ?? "Новая папка");

            if (RenameBox.Tag is (FileManagerItem item, FileSystemNode node))
            {
                bool isNewNote   = item.Name == newNoteName   || item.Name.StartsWith(newNoteName   + " ");
                bool isNewFolder = item.Name == newFolderName || item.Name.StartsWith(newFolderName + " ");

                if (isNewNote || isNewFolder)
                {
                    _user.Tree.Remove(node);
                    _userList.RemoveNode(_user.Login, node);
                    MyFileManager.LoadFromTree(_user.Tree);
                }
            }
        }

        private void NewNote_Click    (object sender, RoutedEventArgs e) => CreateNote();
        private void NewFolder_Click  (object sender, RoutedEventArgs e) => CreateFolder();
        private void Delete_Click     (object sender, RoutedEventArgs e) => DeleteSelected();
        private void Rename_Click     (object sender, RoutedEventArgs e) => RenameSelected();
        private void CollapseAll_Click(object sender, RoutedEventArgs e) => MyFileManager.CollapseAll();

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) MaximizeButton_Click(sender, e);
            else if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveActiveNote();
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }
}