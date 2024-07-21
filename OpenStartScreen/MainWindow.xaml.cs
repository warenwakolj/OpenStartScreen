using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Security.Principal;

namespace OpenStartScreen
{
    public partial class MainWindow : Window
    {
        private double targetVerticalOffset;
        private DateTime animationStartTime;
        private TimeSpan animationDuration;

        public MainWindow()
 {
        InitializeComponent();
        LoadStartMenuItems();
        LoadPinnedItems();
        this.WindowState = WindowState.Maximized;
        GridsPanel.AllowDrop = true;
        GridsPanel.Drop += GridsPanel_Drop;
        DataContext = new UserCard();  
        GridsPanel.DragOver += GridsPanel_DragOver;
    }

        private void GoToApps_Click(object sender, RoutedEventArgs e)
        {
            double newOffset = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight;
            AnimateScrollViewer(scrollViewer, newOffset, TimeSpan.FromSeconds(1));
        }

        private void GoToStart_Click(object sender, RoutedEventArgs e)
        {
            double newOffset = scrollViewer.VerticalOffset - scrollViewer.ViewportHeight;
            AnimateScrollViewer(scrollViewer, newOffset, TimeSpan.FromSeconds(1));
        }

        private void AnimateScrollViewer(ScrollViewer scrollViewer, double toValue, TimeSpan duration)
        {
            targetVerticalOffset = toValue;
            animationDuration = duration;
            animationStartTime = DateTime.Now;

            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            var elapsed = DateTime.Now - animationStartTime;
            if (elapsed >= animationDuration)
            {
                scrollViewer.ScrollToVerticalOffset(targetVerticalOffset);
                CompositionTarget.Rendering -= OnCompositionTargetRendering;
            }
            else
            {
                double progress = elapsed.TotalMilliseconds / animationDuration.TotalMilliseconds;
                double currentOffset = scrollViewer.VerticalOffset;
                double newOffset = currentOffset + (targetVerticalOffset - currentOffset) * progress;
                scrollViewer.ScrollToVerticalOffset(newOffset);
            }
        }
        private const string StartMenuPath = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs";
        private static string PinnedStartMenuPath = Environment.ExpandEnvironmentVariables(@"%AppData%\Microsoft\Internet Explorer\Quick Launch\User Pinned\StartMenu");

        private void GridsPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Tile)))
            {
                Tile tile = e.Data.GetData(typeof(Tile)) as Tile;

                if (tile != null)
                {
                    UniformGrid parentGrid = FindParent<UniformGrid>(tile);
                    if (parentGrid != null)
                    {
                        parentGrid.Children.Remove(tile);
                    }

                    Point dropPosition = e.GetPosition(GridsPanel);
                    bool addedToExistingGrid = false;

                    foreach (UniformGrid grid in GridsPanel.Children)
                    {
                        Rect bounds = VisualTreeHelper.GetDescendantBounds(grid);
                        Point topLeft = grid.TranslatePoint(new Point(0, 0), GridsPanel);

                        if (dropPosition.X >= topLeft.X && dropPosition.X <= topLeft.X + bounds.Width &&
                            dropPosition.Y >= topLeft.Y && dropPosition.Y <= topLeft.Y + bounds.Height)
                        {
                            grid.Children.Add(tile);
                            addedToExistingGrid = true;
                            break;
                        }
                    }

                    if (!addedToExistingGrid)
                    {
                        UniformGrid newGrid = new UniformGrid { Columns = 2, Rows = 4, Margin = new Thickness(10) };

                        GridsPanel.Children.Add(newGrid);
                        newGrid.Children.Add(tile);
                    }
                }
            }
        }

        private void GridsPanel_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Tile)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }


        private void PlaceNewGrid(Point dropPosition, UniformGrid newGrid)
        {
            double x = dropPosition.X;
            double y = dropPosition.Y;
            double maxWidth = 0;
            double maxHeight = 0;

            foreach (UniformGrid grid in GridsPanel.Children)
            {
                Rect bounds = VisualTreeHelper.GetDescendantBounds(grid);
                Point topLeft = grid.TranslatePoint(new Point(0, 0), GridsPanel);
                maxWidth = Math.Max(maxWidth, topLeft.X + bounds.Width);
                maxHeight = Math.Max(maxHeight, topLeft.Y + bounds.Height);
            }

            if (x > maxWidth)
            {
                newGrid.Margin = new Thickness(maxWidth + 10, 0, 0, 0);
            }
            else if (y > maxHeight)
            {
                newGrid.Margin = new Thickness(0, maxHeight + 10, 0, 0);
            }
            else
            {
                newGrid.Margin = new Thickness(x, y, 0, 0);
            }
        }



        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        private bool isFirstGridAdded = false;

        private void AddTileToGrid(Tile tile)
        {
            UniformGrid lastGrid = GridsPanel.Children.OfType<UniformGrid>().LastOrDefault();
            if (lastGrid == null || lastGrid.Children.Count >= lastGrid.Columns * lastGrid.Rows)
            {
                lastGrid = new UniformGrid { Columns = 2, Rows = 4 };

                if (!isFirstGridAdded)
                {
                    lastGrid.Margin = new Thickness(120, 10, 10, 10);
                    isFirstGridAdded = true;
                }
                else
                {
                    lastGrid.Margin = new Thickness(10);
                }

                GridsPanel.Children.Add(lastGrid);
            }
            lastGrid.Children.Add(tile);
        }


        private void LoadStartMenuItems()
        {
            var programFiles = Directory.EnumerateFiles(StartMenuPath, "*.lnk", SearchOption.AllDirectories);
            var rootCategory = new ProgramCategory("Other Programs");

            var categories = new Dictionary<string, ProgramCategory>
            {
                { "Other Programs", rootCategory }
            };

            foreach (var file in programFiles)
            {
                string targetPath = ShortcutResolver.Resolve(file);
                if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                {
                    var relativePath = Path.GetRelativePath(StartMenuPath, file);
                    var directory = Path.GetDirectoryName(relativePath);

                    if (string.IsNullOrEmpty(directory) || directory == ".")
                    {
                        rootCategory.Items.Add(file);
                    }
                    else
                    {
                        if (!categories.ContainsKey(directory))
                        {
                            categories[directory] = new ProgramCategory(directory);
                        }
                        categories[directory].Items.Add(file);
                    }
                }
            }

            DisplayCategories(categories);
        }

        private void LoadPinnedItems()
        {
            GridsPanel.Children.Clear();

            var pinnedFiles = Directory.EnumerateFiles(PinnedStartMenuPath, "*.lnk", SearchOption.TopDirectoryOnly);
            foreach (var file in pinnedFiles)
            {
                var tile = CreatePinnedProgramTile(file);
                if (tile != null)
                {
                    AddTileToGrid(tile);
                }
            }
        }

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

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); 
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); 

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
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
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

        private Tile CreatePinnedProgramTile(string file)
        {
            string targetPath = ShortcutResolver.Resolve(file);
            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                return null;

            var icon = IconExtractor.Extract(targetPath, 0);
            var bitmapImage = icon != null ? IconExtractor.ToBitmapSource(icon) : null;

            var tile = new Tile
            {
                TileName = Path.GetFileNameWithoutExtension(file),
                TileImage = bitmapImage
            };

            tile.MouseRightButtonUp += (s, e) => ShowTileContextMenu(tile, file);
            tile.MouseLeftButtonUp += (s, e) => LaunchProgram(file);

            return tile;
        }

        private void ShowTileContextMenu(Tile tile, string filePath)
        {
            var contextMenu = new ContextMenu();

            var unpinMenuItem = new MenuItem { Header = "Unpin from Start" };
            unpinMenuItem.Click += (s, e) => UnpinFromStartMenu(filePath);
            contextMenu.Items.Add(unpinMenuItem);

            contextMenu.IsOpen = true;
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

        private void UnpinFromStartMenu(string filePath)
        {
            try
            {
                var pinnedFilePath = Path.Combine(PinnedStartMenuPath, Path.GetFileName(filePath));
                if (File.Exists(pinnedFilePath))
                {
                    File.Delete(pinnedFilePath);
                }
                LoadPinnedItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unpin from Start Menu: {ex.Message}");
            }
        }



        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
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
}

