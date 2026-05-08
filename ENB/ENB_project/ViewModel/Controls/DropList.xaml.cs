using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ENB_project.Controls;

/// <summary>
/// Кастомный выпадающий список на основе ToggleButton + Popup.
/// Поддерживает строковые элементы и двустороннюю синхронизацию выбранного индекса.
/// </summary>
public partial class DropList : UserControl
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<string>),
            typeof(DropList),
            new PropertyMetadata(null));

    public ObservableCollection<string> Items
    {
        get => (ObservableCollection<string>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(string),
            typeof(DropList),
            new PropertyMetadata(null));

    public string SelectedItem
    {
        get => (string)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(DropList),
            new PropertyMetadata(-1, OnSelectedIndexChanged));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public event SelectionChangedEventHandler? SelectionChanged;

    private static void OnSelectedIndexChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not DropList control) return;
        var index = (int)e.NewValue;

        if (index >= 0 && index < control.Items.Count)
            control.SelectedItem = control.Items[index];
    }

    public DropList()
    {
        Items = new ObservableCollection<string>();
        InitializeComponent();
        List.SelectionChanged += (s, e) => SelectionChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Добавляет элемент в список. Первый добавленный элемент автоматически выбирается.
    /// </summary>
    public void AddItem(string item)
    {
        Items.Add(item);
        if (Items.Count == 1)
            SelectedIndex = 0;
    }
}