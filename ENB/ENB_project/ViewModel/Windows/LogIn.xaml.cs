using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ENB_project
{
    public partial class LogIn
    {
        private string _language;

        private enum ErrorType
        {
            User,
            Login,
            Password
        }

        public LogIn(string lang = "ru")
        {
            InitializeComponent();
            _language = lang;

            LanguageDrop.Items.Add("Русский");
            LanguageDrop.Items.Add("English");
            LanguageDrop.SelectedIndex = _language == "ru" ? 0 : 1;

            LanguageDrop.SelectionChanged += LanguageCombo_SelectionChanged;
            EnbFunctional.LanguageChanged += OnLanguageChanged;
            Closed += (_, _) => EnbFunctional.LanguageChanged -= OnLanguageChanged;

            EnbFunctional.ApplyLanguage(_language);
            EnbFunctional.ApplyTheme("Dark");
        }

        private void OnLanguageChanged(string lang)
            => ErrorText.Visibility = Visibility.Collapsed;

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _language = LanguageDrop.SelectedIndex == 0 ? "ru" : "en";
            EnbFunctional.ApplyLanguage(_language);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var login    = LoginBox.Text.Trim();
            var password = PasswordBox.Password;

            if (LoginError.Visibility == Visibility.Visible ||
                PasswordError.Visibility == Visibility.Visible)
                return;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError(ErrorType.User,
                    (string)TryFindResource("FillAllFields") ?? "Fill in all fields!");
                return;
            }

            if (login == "admin" && password == "adminadmin")
            {
                ErrorText.Visibility = Visibility.Collapsed;
                EnbFunctional.ApplyTheme("Dark");
                new AdminWindow().Show();
                Close();
                return;
            }

            try
            {
                var userList = new UserList();
                var user     = userList.GetUser(login);

                if (user == null)
                {
                    ShowError(ErrorType.User,
                        (string)TryFindResource("LogInUserNotFound") ?? "User not found");
                    return;
                }

                if (user.Password != password)
                {
                    ShowError(ErrorType.User,
                        (string)TryFindResource("LogInErrorText") ?? "Invalid credentials");
                    return;
                }

                if (user.IsBlocked)
                {
                    ShowError(ErrorType.User,
                        (string)TryFindResource("LogInUserBlocked") ?? "Account is blocked");
                    return;
                }

                ErrorText.Visibility = Visibility.Collapsed;
                EnbFunctional.ApplyTheme(user.Theme);
                new MainWindow(login).Show();
                Close();
            }
            catch (MyExceptions)
            {
                ShowError(ErrorType.User,
                    (string)TryFindResource("LogInUserNotFound") ?? "User not found");
            }
        }

        private void ShowError(ErrorType type, string message)
        {
            switch (type)
            {
                case ErrorType.User:
                    ErrorText.Text       = message;
                    ErrorText.Visibility = Visibility.Visible;
                    break;
                case ErrorType.Login:
                    LoginError.Text       = message;
                    LoginError.Visibility = Visibility.Visible;
                    break;
                case ErrorType.Password:
                    PasswordError.Text       = message;
                    PasswordError.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void RegistrationButton_click(object sender, RoutedEventArgs e)
        {
            new Register(_language).Show();
            Close();
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
                    ShowError(ErrorType.Password,
                        (string)TryFindResource("UserPasswordMinSize")
                        ?? "Password must contain at least 8 characters");
                    break;
                case > 30:
                    ShowError(ErrorType.Password,
                        (string)TryFindResource("UserPasswordMaxSize")
                        ?? "Password must contain no more than 30 characters");
                    break;
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}