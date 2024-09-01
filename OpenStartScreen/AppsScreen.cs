using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenStartScreen;

public partial class MainWindow
{
    private void DisplayCategories(Dictionary<string, ProgramCategory> categories)
    {
        bool isFirstCategory = true;

        foreach (var category in categories.Values)
        {
            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Vertical,
                MaxHeight = 500,
                Margin = new Thickness(10, 0, 0, 0)
            };

            if (category.Name != "Other Programs")
            {
                if (!isFirstCategory)
                {
                    var separator = CreateSeparator();
                    CategoriesPanel.Children.Add(separator);
                }

                var titleButton = CreateCategoryButton(category.Name);
                wrapPanel.Children.Add(titleButton);
            }

            foreach (var file in category.Items)
            {
                var button = CreateProgramButton(file);
                if (button != null)
                {
                    wrapPanel.Children.Add(button);
                }
            }

            var border = new Border
            {
                Padding = new Thickness(10),
                Child = wrapPanel
            };

            CategoriesPanel.Children.Add(border);
            isFirstCategory = false;
        }
    }

    private Button CreateCategoryButton(string categoryName)
    {
        var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var textBlock = new TextBlock
        {
            Text = categoryName,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Left,
            FontSize = 16,
            Opacity = 0.25,
        };
        stackPanel.Children.Add(textBlock);

        var button = new Button
        {
            Content = stackPanel,
            Margin = new Thickness(5),
            Foreground = Brushes.White,
            Padding = new Thickness(10),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(1)
        };

        return button;
    }

    private UIElement CreateSeparator()
    {
        return new Border
        {
            Width = 50,
            Margin = new Thickness(0, 100, 0, 100),
            Background = Brushes.Transparent
        };
    }

    private Button CreateProgramButton(string file)
    {
        string targetPath = ShortcutResolver.Resolve(file);
        if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
            return null;

        var icon = IconExtractor.Extract(targetPath, 0);
        var bitmapImage = icon != null ? IconExtractor.ToBitmapSource(icon) : null;

        var grid = new Grid
        {
            Margin = new Thickness(5),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

        var iconGrid = new Grid
        {
            Width = 40,
            Height = 40,
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        if (bitmapImage != null)
        {
            var image = new Image
            {
                Source = bitmapImage,
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconGrid.Children.Add(image);
        }

        var textBlock = new TextBlock
        {
            Text = Path.GetFileNameWithoutExtension(file),
            Foreground = Brushes.White,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(10, 0, 0, 0)
        };

        Grid.SetColumn(iconGrid, 0);
        Grid.SetColumn(textBlock, 1);

        grid.Children.Add(iconGrid);
        grid.Children.Add(textBlock);

        var button = new Button
        {
            Content = grid,
            Tag = file,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Left,
            BorderBrush = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Top
        };
        button.Click += ProgramButton_Click;

        var contextMenu = new ContextMenu();
        var pinMenuItem = new MenuItem { Header = "Pin to Start" };
        pinMenuItem.Click += (s, e) => PinToStartMenu(file);
        contextMenu.Items.Add(pinMenuItem);
        button.ContextMenu = contextMenu;

        return button;
    }

    private void LaunchProgram(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start program: {ex.Message}");
        }
    }

    private void PinToStartMenu(string filePath)
    {
        try
        {
            var pinnedFilePath = Path.Combine(PinnedStartMenuPath, Path.GetFileName(filePath));
            File.Copy(filePath, pinnedFilePath, true);
            LoadPinnedItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to pin to Start Menu: {ex.Message}");
        }
    }

    private void ProgramButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start program: {ex.Message}");
            }
        }
    }
}