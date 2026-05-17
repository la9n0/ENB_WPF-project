using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ENB_project;

namespace ENB_project.Controls
{
    public partial class UserMenu : UserControl
    {
        private readonly string   _login;
        private readonly UserList _userList = new();
        private User?             _user;

        public UserMenu(string login)
        {
            InitializeComponent();
            _login = login;

            ThemeDropList.SelectionChanged    += ThemeOrLanguage_Changed;
            LanguageDropList.SelectionChanged += ThemeOrLanguage_Changed;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _user = _userList.GetUser(_login);
            }
            catch (Exception ex)
            {
                MyExceptions.IO("Failed to load profile data", "UserMenu.LoadData", ex);
                _user = null;
            }

            if (_user == null) return;

            LoginBox.Text  = _login;
            EmailBox.Text  = _user.Email;

            ThemeDropList.SelectionChanged    -= ThemeOrLanguage_Changed;
            LanguageDropList.SelectionChanged -= ThemeOrLanguage_Changed;

            ThemeDropList.Items.Clear();
            ThemeDropList.Items.Add((string)TryFindResource("UserMenuThemeLight") ?? "Light");
            ThemeDropList.Items.Add((string)TryFindResource("UserMenuThemeDark")  ?? "Dark");

            LanguageDropList.Items.Clear();
            LanguageDropList.Items.Add((string)TryFindResource("UserMenuLangRu") ?? "Русский");
            LanguageDropList.Items.Add((string)TryFindResource("UserMenuLangEn") ?? "English");

            ThemeDropList.SelectedIndex    = _user.Theme    == "Light" ? 0 : 1;
            LanguageDropList.SelectedIndex = _user.Language == "ru"    ? 0 : 1;

            ThemeDropList.SelectionChanged    += ThemeOrLanguage_Changed;
            LanguageDropList.SelectionChanged += ThemeOrLanguage_Changed;
        }

        private void PasswordEdit_Click(object sender, RoutedEventArgs e)
        {
            PasswordEditBtn.Visibility     = Visibility.Collapsed;
            PasswordDialog.Visibility      = Visibility.Visible;
            PasswordDialogError.Visibility = Visibility.Collapsed;
            NewPasswordError.Visibility    = Visibility.Collapsed;
            RepeatPasswordError.Visibility = Visibility.Collapsed;
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            RepeatPasswordBox.Clear();
            CurrentPasswordBox.Focus();
        }

        private void PasswordDialogCancel_Click(object sender, RoutedEventArgs e)
        {
            PasswordDialog.Visibility  = Visibility.Collapsed;
            PasswordEditBtn.Visibility = Visibility.Visible;
        }

        private void NewPasswordSizeControl(object sender, RoutedEventArgs e)
        {
            NewPasswordError.Visibility = Visibility.Collapsed;

            var pb = (PasswordBox)sender;

            switch (pb.Password.Length)
            {
                case < 8:
                    NewPasswordError.Text       = (string)TryFindResource("UserPasswordMinSize")
                                                  ?? "Password must contain at least 8 characters";
                    NewPasswordError.Visibility = Visibility.Visible;
                    break;
                case > 30:
                    NewPasswordError.Text       = (string)TryFindResource("UserPasswordMaxSize")
                                                  ?? "Password must contain no more than 30 characters";
                    NewPasswordError.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void RepeatPasswordSizeControl(object sender, RoutedEventArgs e)
        {
            RepeatPasswordError.Visibility = Visibility.Collapsed;

            var pb = (PasswordBox)sender;

            switch (pb.Password.Length)
            {
                case < 8:
                    RepeatPasswordError.Text       = (string)TryFindResource("UserPasswordMinSize")
                                                     ?? "Password must contain at least 8 characters";
                    RepeatPasswordError.Visibility = Visibility.Visible;
                    break;
                case > 30:
                    RepeatPasswordError.Text       = (string)TryFindResource("UserPasswordMaxSize")
                                                     ?? "Password must contain no more than 30 characters";
                    RepeatPasswordError.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void PasswordDialogConfirm_Click(object sender, RoutedEventArgs e)
        {
            PasswordDialogError.Visibility = Visibility.Collapsed;

            var current = CurrentPasswordBox.Password;
            var next    = NewPasswordBox.Password;
            var repeat  = RepeatPasswordBox.Password;

            if (NewPasswordError.Visibility    == Visibility.Visible ||
                RepeatPasswordError.Visibility == Visibility.Visible)
                return;

            if (string.IsNullOrWhiteSpace(current) ||
                string.IsNullOrWhiteSpace(next)    ||
                string.IsNullOrWhiteSpace(repeat))
            {
                ShowPasswordError(
                    (string)TryFindResource("UserMenuErrorAllFieldsRequired") ?? "All fields are required.");
                return;
            }

            if (current != _user?.Password)
            {
                ShowPasswordError(
                    (string)TryFindResource("UserMenuErrorWrongPassword") ?? "Current password is incorrect.");
                return;
            }

            if (next != repeat)
            {
                ShowPasswordError(
                    (string)TryFindResource("UserMenuErrorPasswordMismatch") ?? "New password and confirmation do not match.");
                return;
            }

            _userList.EditUser(_login, nameof(User.Password), next);
            _user = _userList.GetUser(_login);

            PasswordDialog.Visibility  = Visibility.Collapsed;
            PasswordEditBtn.Visibility = Visibility.Visible;
        }

        private void ShowPasswordError(string message)
        {
            PasswordDialogError.Text       = message;
            PasswordDialogError.Visibility = Visibility.Visible;
        }

        private void FieldEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not string tag || tag != "Email") return;

            if (EmailBox.IsReadOnly)
            {
                EmailBox.IsReadOnly  = false;
                EmailEditBtn.Content = TryFindResource("UserMenuSave") ?? "Save";
                EmailBox.Focus();
                EmailBox.SelectAll();
            }
            else CommitEmail();
        }

        private void CommitEmail()
        {
            if (string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                EmailError.Text       = (string)TryFindResource("UserMenuErrorFieldEmpty") ?? "Field cannot be empty.";
                EmailError.Visibility = Visibility.Visible;
                return;
            }

            if (EmailError.Visibility == Visibility.Visible)
                return;

            _userList.EditUser(_login, nameof(User.Email), EmailBox.Text.Trim());
            _user = _userList.GetUser(_login);

            EmailBox.IsReadOnly  = true;
            EmailEditBtn.Content = TryFindResource("UserMenuEdit") ?? "Edit";
        }

        private void ThemeOrLanguage_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_user == null) return;

            var theme    = ThemeDropList.SelectedIndex    == 0 ? "Light" : "Dark";
            var language = LanguageDropList.SelectedIndex == 0 ? "ru"    : "en";

            _userList.EditUser(_login, nameof(User.Theme),    theme);
            _userList.EditUser(_login, nameof(User.Language), language);

            EnbFunctional.ApplyTheme(theme);
            EnbFunctional.ApplyLanguage(language);

            _user = _userList.GetUser(_login);
            LoadData();
        }

        private void OnlyText(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9.@_\-]+$");

        private void EmailCheck(object sender, RoutedEventArgs e)
        {
            EmailError.Visibility = Visibility.Hidden;
            var email = ((TextBox)sender).Text;

            if (email.Count(c => c == '@') != 1)
            {
                EmailError.Visibility = Visibility.Visible;
                return;
            }

            var domain = new string(email.SkipWhile(c => c != '@').Skip(1).ToArray());

            if (domain is "gmail.com" or "yahoo.com" or "icloud.com"
                       or "outlook.com" or "mail.ru"
                       or "yandex.ru"  or "yandex.by")
                return;

            EmailError.Visibility = Visibility.Visible;
        }
    }
}