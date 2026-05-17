using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ENB_project
{
    public class AdminUserViewModel : INotifyPropertyChanged
    {
        private bool _isBlocked;

        public int    Id        { get; set; }
        public string Login     { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public int    NoteCount { get; set; }

        public bool IsBlocked
        {
            get => _isBlocked;
            set
            {
                _isBlocked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BlockButtonText));
                OnPropertyChanged(nameof(BlockedBadgeVisibility));
            }
        }

        public string BlockButtonText
            => IsBlocked
                ? (string)Application.Current.TryFindResource("AdminUnblock") ?? "Разблокировать"
                : (string)Application.Current.TryFindResource("AdminBlock")   ?? "Заблокировать";

        public Visibility BlockedBadgeVisibility
            => IsBlocked ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class AdminWindow : Window
    {
        private readonly UserList _userList = new();
        private string? _pendingDeleteLogin;

        public AdminWindow()
        {
            InitializeComponent();
            EnbFunctional.ApplyTheme("Dark");
            LoadUsers();
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

        private void LoadUsers()
        {
            var users = _userList.GetAllUsers();

            var viewModels = users
                .Select(u => new AdminUserViewModel
                {
                    Id        = u.Id,
                    Login     = u.Login,
                    Email     = u.Email,
                    IsBlocked = u.IsBlocked,
                    NoteCount = u.NoteCount
                })
                .ToList();

            UserList.ItemsSource = viewModels;

            EmptyText.Visibility = viewModels.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            var totalLabel = (string)TryFindResource("AdminTotal") ?? "Всего:";
            UserCountText.Text = $"{totalLabel} {viewModels.Count}";
        }

        private void Block_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn) return;
            if (btn.Tag is not AdminUserViewModel vm) return;

            var newState = !vm.IsBlocked;
            _userList.SetUserBlocked(vm.Login, newState);
            vm.IsBlocked = newState;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn) return;
            if (btn.Tag is not AdminUserViewModel vm) return;

            _pendingDeleteLogin = vm.Login;

            DeleteConfirmText.Text = string.Format(
                (string)TryFindResource("AdminDeleteConfirm") ?? "Удалить пользователя «{0}»?",
                vm.Login);

            DeleteConfirmPanel.Visibility = Visibility.Visible;
        }

        private void DeleteConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;

            if (_pendingDeleteLogin == null) return;

            _userList.DelUser(_pendingDeleteLogin);
            _pendingDeleteLogin = null;
            LoadUsers();
        }

        private void DeleteConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;
            _pendingDeleteLogin = null;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
            => LoadUsers();

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) MaximizeButton_Click(sender, e);
            else if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            new LogIn().Show();
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