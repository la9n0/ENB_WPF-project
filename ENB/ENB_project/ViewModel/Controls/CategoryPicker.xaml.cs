using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ENB_project
{
    public class CategoryViewModel
    {
        public int    Id    { get; set; }
        public string Name  { get; set; } = string.Empty;
        public string Color { get; set; } = "#7C6FCD";

        public SolidColorBrush ColorBrush
        {
            get
            {
                try { return new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(Color)); }
                catch { return new SolidColorBrush(Colors.Gray); }
            }
        }
    }

    public partial class CategoryPicker : Window
    {
        private static readonly string[] Palette =
        {
            "#E06C75", "#E5C07B", "#98C379", "#56B6C2",
            "#61AFEF", "#C678DD", "#7C6FCD", "#ABB2BF",
            "#FF7F50", "#20B2AA", "#DDA0DD", "#6495ED"
        };

        private readonly string   _login;
        private readonly UserList _userList;
        private List<CategoryViewModel> _categories;

        private int?   _editingId    = null;
        private string _selectedColor = Palette[6];

        public int?   SelectedCategoryId    { get; private set; }
        public string? SelectedCategoryColor { get; private set; }

        public CategoryPicker(
            List<Category> categories, int? currentCategoryId,
            string login, UserList userList)
        {
            InitializeComponent();
            _login    = login;
            _userList = userList;

            SelectedCategoryId = currentCategoryId;

            _categories = categories
                .Select(c => new CategoryViewModel { Id = c.Id, Name = c.Name, Color = c.Color })
                .ToList();

            BuildColorPicker();
            RefreshList();
        }

        private void BuildColorPicker()
        {
            ColorPicker.Children.Clear();

            foreach (var hex in Palette)
            {
                var rect = new Rectangle
                {
                    Width        = 22,
                    Height       = 22,
                    RadiusX      = 4,
                    RadiusY      = 4,
                    Margin       = new Thickness(0, 0, 4, 4),
                    Cursor       = Cursors.Hand,
                    Tag          = hex,
                    Stroke       = hex == _selectedColor
                        ? Brushes.White
                        : Brushes.Transparent,
                    StrokeThickness = 2
                };

                try { rect.Fill = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(hex)); }
                catch { rect.Fill = Brushes.Gray; }

                rect.MouseLeftButtonDown += ColorSwatch_Click;
                ColorPicker.Children.Add(rect);
            }
        }

        private void ColorSwatch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Rectangle rect) return;
            _selectedColor = (string)rect.Tag;
            BuildColorPicker();
        }

        private void RefreshList()
        {
            CatList.ItemsSource = null;
            CatList.ItemsSource = _categories;
        }

        private void NoneRow_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedCategoryId    = null;
            SelectedCategoryColor = null;
            DialogResult          = true;
        }

        private void CatRow_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not CategoryViewModel vm) return;

            if (e.OriginalSource is Button) return;

            SelectedCategoryId    = vm.Id;
            SelectedCategoryColor = vm.Color;
            DialogResult          = true;
        }

        private void AddCat_Click(object sender, RoutedEventArgs e)
        {
            _editingId      = null;
            CatNameBox.Text = string.Empty;
            _selectedColor  = Palette[6];
            BuildColorPicker();
            EditorPanel.Visibility = Visibility.Visible;
            AddCatBtn.Visibility   = Visibility.Collapsed;
            CatNameBox.Focus();
        }

        private void EditCat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not CategoryViewModel vm) return;

            _editingId      = vm.Id;
            CatNameBox.Text = vm.Name;
            _selectedColor  = vm.Color;
            BuildColorPicker();
            EditorPanel.Visibility = Visibility.Visible;
            AddCatBtn.Visibility   = Visibility.Collapsed;
            CatNameBox.Focus();
        }

        private void DeleteCat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not CategoryViewModel vm) return;

            _userList.DeleteCategory(vm.Id);
            _categories.RemoveAll(c => c.Id == vm.Id);

            if (SelectedCategoryId == vm.Id)
            {
                SelectedCategoryId    = null;
                SelectedCategoryColor = null;
            }

            RefreshList();
        }

        private void EditorSave_Click(object sender, RoutedEventArgs e)
        {
            var name = CatNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            if (_editingId == null)
            {
                var created = _userList.AddCategory(_login, name, _selectedColor);
                _categories.Add(new CategoryViewModel
                {
                    Id    = created.Id,
                    Name  = created.Name,
                    Color = created.Color
                });
            }
            else
            {
                _userList.EditCategory(_editingId.Value, name, _selectedColor);
                var vm = _categories.FirstOrDefault(c => c.Id == _editingId.Value);
                if (vm != null) { vm.Name = name; vm.Color = _selectedColor; }

                if (SelectedCategoryId == _editingId.Value)
                    SelectedCategoryColor = _selectedColor;
            }

            EditorPanel.Visibility = Visibility.Collapsed;
            AddCatBtn.Visibility   = Visibility.Visible;
            RefreshList();
        }

        private void EditorCancel_Click(object sender, RoutedEventArgs e)
        {
            EditorPanel.Visibility = Visibility.Collapsed;
            AddCatBtn.Visibility   = Visibility.Visible;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}