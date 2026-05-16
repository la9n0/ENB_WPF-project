using System.Windows;
using System.Windows.Input;

namespace ENB_project
{
    public partial class ReminderNotification : Window
    {
        public string NoteName { get; }
        public bool   OpenNote { get; private set; }

        public ReminderNotification(TriggeredReminder reminder)
        {
            InitializeComponent();

            NoteName          = reminder.NoteName;
            NoteNameText.Text = reminder.NoteName;
            TimeText.Text     = reminder.RemindAt.ToString("dd.MM.yyyy HH:mm");

            if (!string.IsNullOrEmpty(reminder.Note))
            {
                NoteText.Text       = reminder.Note;
                NoteText.Visibility = Visibility.Visible;
            }

            PositionBottomRight();
        }

        private void PositionBottomRight()
        {
            Loaded += (_, _) =>
            {
                var area = SystemParameters.WorkArea;
                Left = area.Right  - ActualWidth  - 16;
                Top  = area.Bottom - ActualHeight - 16;
            };
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenNote = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => Close();

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}