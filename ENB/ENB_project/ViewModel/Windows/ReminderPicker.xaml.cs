using System.Windows;
using System.Windows.Input;

namespace ENB_project
{
    public partial class ReminderPicker : Window
    {
        public bool     Saved            { get; private set; }
        public bool     Deleted          { get; private set; }
        public DateTime SelectedDateTime { get; private set; }
        public string   NoteText         { get; private set; } = string.Empty;

        public ReminderPicker(Reminder? existing)
        {
            InitializeComponent();

            if (existing != null)
            {
                DatePick.SelectedDate = existing.RemindAt.Date;
                HourBox.Text          = existing.RemindAt.Hour.ToString("D2");
                MinuteBox.Text        = existing.RemindAt.Minute.ToString("D2");
                NoteBox.Text          = existing.Note;
                DeleteBtn.Visibility  = Visibility.Visible;
            }
            else
            {
                DatePick.SelectedDate = DateTime.Today.AddDays(1);
                DeleteBtn.Visibility  = Visibility.Collapsed;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (DatePick.SelectedDate == null)
            {
                ShowError((string)TryFindResource("ReminderErrorDate") ?? "Выберите дату");
                return;
            }

            if (!int.TryParse(HourBox.Text, out int hour) || hour < 0 || hour > 23)
            {
                ShowError((string)TryFindResource("ReminderErrorHour") ?? "Часы: 0–23");
                return;
            }

            if (!int.TryParse(MinuteBox.Text, out int minute) || minute < 0 || minute > 59)
            {
                ShowError((string)TryFindResource("ReminderErrorMinute") ?? "Минуты: 0–59");
                return;
            }

            var dt = DatePick.SelectedDate.Value.Date
                .AddHours(hour)
                .AddMinutes(minute);

            if (dt <= DateTime.Now)
            {
                ShowError((string)TryFindResource("ReminderErrorPast")
                    ?? "Дата и время должны быть в будущем");
                return;
            }

            SelectedDateTime = dt;
            NoteText         = NoteBox.Text.Trim();
            Saved            = true;
            DialogResult     = true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Deleted      = true;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void ShowError(string message)
        {
            ErrorText.Text       = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void NumberOnly(object sender, TextCompositionEventArgs e)
            => e.Handled = !e.Text.All(char.IsDigit);

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}