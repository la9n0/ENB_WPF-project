using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ENB_project;

namespace ENB_project.Controls
{
    public partial class UserMenu : UserControl
    {
        private readonly string   _username;
        private readonly UserList _userList = new();
        private User?             _user;

        public UserMenu(string username)
        {
            InitializeComponent();
            _username = username;

            ThemeDropList.List.SelectionChanged    += ThemeOrLanguage_Changed;
            LanguageDropList.List.SelectionChanged += ThemeOrLanguage_Changed;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _userList.LoadJson();
                _user = _userList.GetUser(_username);
            }
            catch (MyExceptions)
            {
                _user = null;
            }
            catch (Exception ex)
            {
                MyExceptions.IO("Failed to load profile data", "UserMenu.LoadData", ex);
                _user = null;
            }

            if (_user == null) return;

            UsernameBox.Text = _username;
            EmailBox.Text    = _user.Email;

            ThemeDropList.List.SelectionChanged    -= ThemeOrLanguage_Changed;
            LanguageDropList.List.SelectionChanged -= ThemeOrLanguage_Changed;

            ThemeDropList.Items.Clear();
            ThemeDropList.AddItem((string)TryFindResource("UserMenuThemeLight") ?? "Light");
            ThemeDropList.AddItem((string)TryFindResource("UserMenuThemeDark")  ?? "Dark");

            LanguageDropList.Items.Clear();
            LanguageDropList.AddItem((string)TryFindResource("UserMenuLangRu") ?? "Русский");
            LanguageDropList.AddItem((string)TryFindResource("UserMenuLangEn") ?? "English");

            ThemeDropList.SelectedIndex    = _user.Theme    == "Light" ? 0 : 1;
            LanguageDropList.SelectedIndex = _user.Language == "ru"    ? 0 : 1;

            ThemeDropList.List.SelectionChanged    += ThemeOrLanguage_Changed;
            LanguageDropList.List.SelectionChanged += ThemeOrLanguage_Changed;
        }

        private void PasswordEdit_Click(object sender, RoutedEventArgs e)
        {
            PasswordEditBtn.Visibility     = Visibility.Collapsed;
            PasswordDialog.Visibility      = Visibility.Visible;
            PasswordDialogError.Visibility = Visibility.Collapsed;
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

        /// <summary>
        /// Validates current password, checks new password confirmation and non-empty fields.
        /// Saves the new password on success.
        /// </summary>
        private void PasswordDialogConfirm_Click(object sender, RoutedEventArgs e)
        {
            var current = CurrentPasswordBox.Password;
            var next    = NewPasswordBox.Password;
            var repeat  = RepeatPasswordBox.Password;

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

            _userList.EditUser(_username, nameof(User.Password), next);
            _userList.SaveJson();
            _user = _userList.GetUser(_username);

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
            else
            {
                CommitEmail();
            }
        }

        private void CommitEmail()
        {
            if (EmailError.Visibility == Visibility.Visible)
            {
                MessageBox.Show(
                    (string)TryFindResource("UserMenuErrorEmailInvalid") ?? "Please enter a valid email.",
                    (string)TryFindResource("UserMenuErrorTitle") ?? "Error");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                MessageBox.Show(
                    (string)TryFindResource("UserMenuErrorFieldEmpty") ?? "Field cannot be empty.",
                    (string)TryFindResource("UserMenuErrorTitle") ?? "Error");
                return;
            }

            _userList.EditUser(_username, nameof(User.Email), EmailBox.Text.Trim());
            _userList.SaveJson();
            _user = _userList.GetUser(_username);

            EmailBox.IsReadOnly  = true;
            EmailEditBtn.Content = TryFindResource("UserMenuEdit") ?? "Edit";
        }

        private void ThemeOrLanguage_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_user == null) return;

            var theme    = ThemeDropList.SelectedIndex    == 0 ? "Light" : "Dark";
            var language = LanguageDropList.SelectedIndex == 0 ? "ru"    : "en";

            _userList.EditUser(_username, nameof(User.Theme),    theme);
            _userList.EditUser(_username, nameof(User.Language), language);
            _userList.SaveJson();

            EnbFunctional.ApplyTheme(theme);
            EnbFunctional.ApplyLanguage(language);

            _user = _userList.GetUser(_username);
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