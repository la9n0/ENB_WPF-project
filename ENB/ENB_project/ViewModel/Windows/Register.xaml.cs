using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ENB_project
{
    public partial class Register : Window
    {
        private string _language;

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
                       or "yandex.ru" or "yandex.by")
                return;

            EmailError.Visibility = Visibility.Visible;
        }

        private void IsPasswordsMatch(object sender, RoutedEventArgs e)
        {
            PasswordsError.Visibility =
                PasswordBox1.Password != PasswordBox2.Password
                && !string.IsNullOrEmpty(PasswordBox1.Password)
                && !string.IsNullOrEmpty(PasswordBox2.Password)
                    ? Visibility.Visible
                    : Visibility.Hidden;
        }

        private void CreateButton_click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UsernameBox.Text)       ||
                string.IsNullOrEmpty(PasswordBox1.Password)  ||
                string.IsNullOrEmpty(PasswordBox2.Password)  ||
                string.IsNullOrEmpty(EmailBox.Text)          ||
                EmailError.Visibility     == Visibility.Visible ||
                PasswordsError.Visibility == Visibility.Visible)
            {
                MessageBox.Show((string)FindResource("FillAllFields"));
                return;
            }

            var list = new UserList();

            if (!list.AddUser(UsernameBox.Text, PasswordBox1.Password,
                    EmailBox.Text, "Dark", _language))
            {
                MessageBox.Show((string)FindResource("RegisterAlreadyExists"));
                return;
            }

            EnbFunctional.ApplyTheme("Dark");
            new MainWindow(UsernameBox.Text).Show();
            Close();
        }
    }
}