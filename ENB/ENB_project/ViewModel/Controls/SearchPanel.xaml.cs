using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ENB_project.Controls;

public class SearchResultItem
{
    public string  Name              { get; set; } = string.Empty;
    public string  Snippet           { get; set; } = string.Empty;
    public string? CategoryColor     { get; set; }
    public bool    HasActiveReminder { get; set; }

    public Visibility SnippetVisibility => string.IsNullOrEmpty(Snippet)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility CategoryVisibility => string.IsNullOrEmpty(CategoryColor)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility ReminderVisibility => HasActiveReminder
        ? Visibility.Visible
        : Visibility.Collapsed;

    public SolidColorBrush? CategoryBrush
    {
        get
        {
            if (CategoryColor == null) return null;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(CategoryColor);
                return new SolidColorBrush(color);
            }
            catch { return null; }
        }
    }
}

public class SearchPanelItemSelectedEventArgs : RoutedEventArgs
{
    public string NoteName { get; }

    public SearchPanelItemSelectedEventArgs(RoutedEvent routedEvent, string noteName)
        : base(routedEvent)
    {
        NoteName = noteName;
    }
}

public partial class SearchPanel : UserControl, INotifyPropertyChanged
{
    public static readonly RoutedEvent ItemSelectedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(ItemSelected),
            RoutingStrategy.Bubble,
            typeof(EventHandler<SearchPanelItemSelectedEventArgs>),
            typeof(SearchPanel));

    public event EventHandler<SearchPanelItemSelectedEventArgs> ItemSelected
    {
        add    => AddHandler(ItemSelectedEvent, value);
        remove => RemoveHandler(ItemSelectedEvent, value);
    }

    private FileSystemTree? _tree;

    private string _queryInfo = string.Empty;
    public string QueryInfo
    {
        get => _queryInfo;
        private set { _queryInfo = value; OnPropertyChanged(); }
    }

    private Visibility _queryInfoVisibility = Visibility.Collapsed;
    public Visibility QueryInfoVisibility
    {
        get => _queryInfoVisibility;
        private set { _queryInfoVisibility = value; OnPropertyChanged(); }
    }

    public SearchPanel()
    {
        InitializeComponent();
    }

    public void SetTree(FileSystemTree tree)
    {
        _tree = tree;
        RunSearch(SearchBox.Text);
    }

    private void RunSearch(string query)
    {
        if (_tree == null) return;

        var trimmed = query.Trim();
        var results = new List<SearchResultItem>();

        _tree.Traverse((node, _) =>
        {
            if (node.IsFolder) return;

            if (string.IsNullOrEmpty(trimmed))
            {
                results.Add(new SearchResultItem
                {
                    Name             = node.Name,
                    CategoryColor    = node.CategoryColor,
                    HasActiveReminder = node.ReminderTriggered
                });
                return;
            }

            bool   nameMatch    = node.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
            string snippet      = BuildSnippet(node.Content, trimmed);
            bool   contentMatch = snippet.Length > 0;

            if (nameMatch || contentMatch)
                results.Add(new SearchResultItem
                {
                    Name             = node.Name,
                    Snippet          = snippet,
                    CategoryColor    = node.CategoryColor,
                    HasActiveReminder = node.ReminderTriggered
                });
        });

        ResultsList.ItemsSource = results;

        if (string.IsNullOrEmpty(trimmed))
        {
            QueryInfoVisibility = Visibility.Collapsed;
        }
        else
        {
            var template = (string)TryFindResource("SearchFound") ?? "Найдено: {0}";
            QueryInfo           = string.Format(template, results.Count);
            QueryInfoVisibility = Visibility.Visible;
        }
    }

    private static string BuildSnippet(string? content, string query)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        int idx = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return string.Empty;

        int end   = Math.Min(content.Length, idx + query.Length + 80);
        var slice = content[idx..end].ReplaceLineEndings(" ");

        if (idx > 0)              slice = "…" + slice;
        if (end < content.Length) slice += "…";

        return slice;
    }

    private void ResultRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.DataContext is not SearchResultItem item) return;

        RaiseEvent(new SearchPanelItemSelectedEventArgs(ItemSelectedEvent, item.Name)
        {
            Source = this
        });

        e.Handled = true;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        RunSearch(SearchBox.Text);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}