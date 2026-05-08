using System.Windows;
using System.Windows.Controls;

namespace ENB_project
{
    public partial class LogIn
    {
        private string _language;

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
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _language = LanguageDrop.SelectedIndex == 0 ? "ru" : "en";
            EnbFunctional.ApplyLanguage(_language);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var login    = LoginBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError((string)TryFindResource("FillAllFields") ?? "Fill in all fields!");
                return;
            }

            try
            {
                var userList = new UserList();
                userList.LoadJson();
                var user = userList.GetUser(login);

                if (user == null)
                {
                    ShowError((string)TryFindResource("LogInUserNotFound") ?? "User not found");
                    return;
                }

                if (user.Password != password)
                {
                    ShowError((string)TryFindResource("LogInErrorText") ?? "Invalid credentials");
                    return;
                }

                ErrorText.Visibility = Visibility.Collapsed;
                EnbFunctional.ApplyTheme(user.Theme);
                new MainWindow(login).Show();
                Close();
            }
            catch (MyExceptions)
            {
                ShowError((string)TryFindResource("LogInUserNotFound") ?? "User not found");
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text       = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void RegistrationButton_click(object sender, RoutedEventArgs e)
        {
            new Register(_language).Show();
            Close();
        }
    }
}