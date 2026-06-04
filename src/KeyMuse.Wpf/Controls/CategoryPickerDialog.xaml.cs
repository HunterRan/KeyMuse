using System.Linq;
using System.Windows;
using Application = System.Windows.Application;

namespace KeyMuse.Wpf.Controls;

public partial class CategoryPickerDialog : Window
{
    private readonly App _app;

    public string? SelectedCategory { get; private set; }

    public CategoryPickerDialog()
    {
        InitializeComponent();
        _app = (App)Application.Current;
        LoadCategories();
    }

    private void LoadCategories()
    {
        var categories = _app.RecordingManager.ListCategories();
        CategoryList.ItemsSource = categories;
        if (categories.Length > 0)
            CategoryList.SelectedIndex = 0;
    }

    private void CategoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is string cat)
        {
            NewCategoryBox.Text = cat;
        }
    }

    private void NewCategoryBtn_Click(object sender, RoutedEventArgs e)
    {
        var name = NewCategoryBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;
        _app.RecordingManager.CreateCategory(name);
        LoadCategories();
        CategoryList.SelectedItem = name;
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        var fromList = CategoryList.SelectedItem as string;
        var fromBox = NewCategoryBox.Text.Trim();

        var category = fromBox;
        if (string.IsNullOrWhiteSpace(category))
            category = fromList;

        if (string.IsNullOrWhiteSpace(category))
        {
            DarkMessageBox.Show("请选择或输入一个分类", "提示", DarkMessageBoxIcon.Info);
            return;
        }

        SelectedCategory = category;
        DialogResult = true;
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
