using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ENB_project
{
    public partial class Register : Window
    {
        private string _language;

        private enum ErrorType
        {
            User,
            Login,
            Password
        }

        public Register(string lang)
        {
            InitializeComponent();
            _language = lang;

            LanguageDrop.Items.Add("Русский");
            LanguageDrop.Items.Add("English");
            LanguageDrop.SelectedIndex = _language == "ru" ? 0 : 1;

            EnbFunctional.ApplyTheme("Dark");
            EnbFunctional.ApplyLanguage(_language);
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _language = LanguageDrop.SelectedIndex == 0 ? "ru" : "en";
            EnbFunctional.ApplyLanguage(_language);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            new LogIn(_language).Show();
            Close();
        }

        private void OnlyText(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9.@]+$");

        private void EmailCheck(object sender, RoutedEventArgs e)
        {
            EmailError.Visibility = Visibility.Collapsed;

            var email = ((TextBox)sender).Text;

            if (email.Count(c => c == '@') != 1)
            {
                EmailError.Visibility = Visibility.Visible;
                return;
            }

            var domain = new string(
                email.SkipWhile(c => c != '@')
                     .Skip(1)
                     .ToArray()
            );

            if (domain is "gmail.com" or "yahoo.com" or "icloud.com"
                       or "outlook.com" or "mail.ru"
                       or "yandex.ru" or "yandex.by")
                return;

            EmailError.Visibility = Visibility.Visible;
        }

        private void ShowError(ErrorType type, string message)
        {
            switch (type)
            {
                case ErrorType.User:
                    ErrorText.Text = message;
                    ErrorText.Visibility = Visibility.Visible;
                    break;

                case ErrorType.Login:
                    LoginError.Text = message;
                    LoginError.Visibility = Visibility.Visible;
                    break;

                case ErrorType.Password:
                    PasswordError.Text = message;
                    PasswordError.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void IsPasswordsMatch(object sender, RoutedEventArgs e)
        {
            PasswordsError.Visibility =
                PasswordBox1.Password != PasswordBox2.Password
                && !string.IsNullOrEmpty(PasswordBox1.Password)
                && !string.IsNullOrEmpty(PasswordBox2.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void UsernameSizeControl(object sender, TextChangedEventArgs e)
        {
            LoginError.Visibility = Visibility.Collapsed;

            var tb = (TextBox)sender;

            switch (tb.Text.Length)
            {
                case < 4:
                    ShowError(ErrorType.Login,
                        (string)TryFindResource("UserLoginMinSize")
                        ?? "Login must contain at least 4 characters");
                    break;

                case > 25:
                    ShowError(ErrorType.Login,
                        (string)TryFindResource("UserLoginMaxSize")
                        ?? "Login must contain no more than 25 characters");
                    break;
            }
        }

        private void PasswordSizeControl(object sender, RoutedEventArgs e)
        {
            PasswordError.Visibility = Visibility.Collapsed;

            var pb = (PasswordBox)sender;

            switch (pb.Password.Length)
            {
                case < 8:
                    ShowError(
                        ErrorType.Password,
                        (string)TryFindResource("UserPasswordMinSize")
                        ?? "Password must contain at least 8 characters"
                    );
                    break;

                case > 30:
                    ShowError(
                        ErrorType.Password,
                        (string)TryFindResource("UserPasswordMaxSize")
                        ?? "Password must contain no more than 30 characters"
                    );
                    break;
            }
        }

        private void CreateButton_click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(UsernameBox.Text) ||
                string.IsNullOrEmpty(PasswordBox1.Password) ||
                string.IsNullOrEmpty(PasswordBox2.Password) ||
                string.IsNullOrEmpty(EmailBox.Text) ||
                EmailError.Visibility == Visibility.Visible ||
                PasswordsError.Visibility == Visibility.Visible ||
                LoginError.Visibility == Visibility.Visible ||
                PasswordError.Visibility == Visibility.Visible)
            {
                ShowError(
                    ErrorType.User,
                    (string)TryFindResource("FillAllFields")
                    ?? "Fill in all fields!"
                );

                return;
            }

            var list = new UserList();

            if (!list.AddUser(
                    UsernameBox.Text,
                    PasswordBox1.Password,
                    EmailBox.Text,
                    "Dark",
                    _language))
            {
                ShowError(
                    ErrorType.User,
                    (string)TryFindResource("RegisterAlreadyExists")
                    ?? "User already exists"
                );

                return;
            }

            EnbFunctional.ApplyTheme("Dark");

            new MainWindow(UsernameBox.Text).Show();

            Close();
        }
        
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}