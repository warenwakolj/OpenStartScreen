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
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Media3D;


namespace OpenStartScreen
{
    public partial class MainWindow : Window
    {
        private bool isZoomedOut = false;
        private ScaleTransform scaleTransform;
        private double targetVerticalOffset;
        private DateTime animationStartTime;
        private TimeSpan animationDuration;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

        public MainWindow()
        {
            InitializeComponent();
            LoadStartMenuItems();
            LoadPinnedItems();
            this.WindowState = WindowState.Maximized;
            GridsPanel.AllowDrop = true;
            DataContext = new UserCard();

            scaleTransform = new ScaleTransform(1.0, 1.0);
            var translateTransform = new TranslateTransform();
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTransform);

            GridsPanel.LayoutTransform = transformGroup;
            this.KeyDown += MainWindow_KeyDown;

            GridsPanel.Drop += GridsPanel_Drop;
            GridsPanel.DragOver += GridsPanel_DragOver;
        }



        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z)
            {
                ToggleZoom();
            }
        }

        private void ToggleZoom()
        {
            double newScale = isZoomedOut ? 1.0 : 0.3;
            var transformGroup = (TransformGroup)GridsPanel.LayoutTransform;
            var scaleTransform = (ScaleTransform)transformGroup.Children[0];
            var translateTransform = (TranslateTransform)transformGroup.Children[1];

            DoubleAnimation scaleXAnimation = new DoubleAnimation
            {
                To = newScale,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation scaleYAnimation = new DoubleAnimation
            {
                To = newScale,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);

            double translateX = isZoomedOut ? 0 : (GridsPanel.ActualWidth * (1 - newScale)) / 2;
            double translateY = isZoomedOut ? 0 : (GridsPanel.ActualHeight * (1 - newScale)) / 2;

            DoubleAnimation translateXAnimation = new DoubleAnimation
            {
                To = translateX,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation translateYAnimation = new DoubleAnimation
            {
                To = translateY,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            translateTransform.BeginAnimation(TranslateTransform.XProperty, translateXAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);

            isZoomedOut = !isZoomedOut;
        }


        private const int SPI_GETDESKWALLPAPER = 0x0073;
        private const int MAX_PATH = 260;

        private string GetWallpaperPath()
        {
            var wallpaperPath = new StringBuilder(MAX_PATH);
            SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaperPath, 0);
            return wallpaperPath.ToString();
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
            _allTiles.Clear();

            var desktopTileInfo = CreateDesktopTileInfo();
            if (desktopTileInfo != null)
            {
                desktopTileInfo.Tile = CreateTile(desktopTileInfo);
                _allTiles.Add(desktopTileInfo);
            }

            var pinnedFiles = Directory.EnumerateFiles(PinnedStartMenuPath, "*.lnk", SearchOption.TopDirectoryOnly);
            foreach (var file in pinnedFiles)
            {
                var tileInfo = CreatePinnedProgramTileInfo(file);
                if (tileInfo != null)
                {
                    tileInfo.Tile = CreateTile(tileInfo);
                    _allTiles.Add(tileInfo);
                }
            }

            ReorganizeTiles();
        }



        private Tile CreateTile(TileInfo tileInfo)
        {
            var tile = new Tile
            {
                TileName = tileInfo.Name,
                TileImage = tileInfo.Image,
                TileBackground = tileInfo.Background,
                TileSize = tileInfo.Size
            };

            tile.MouseRightButtonUp += (s, e) => ShowTileContextMenu(tile, tileInfo.FilePath);
            tile.MouseLeftButtonUp += (s, e) => LaunchProgram(tileInfo.FilePath);

            // Make the tile draggable
            tile.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragDrop.DoDragDrop(tile, tile, DragDropEffects.Move);
                }
            };

            return tile;
        }

        private TileInfo CreateDesktopTileInfo()
        {
            return new TileInfo
            {
                Name = "Desktop",
                Image = null,
                Background = new BitmapImage(new Uri(GetWallpaperPath())),
                FilePath = "explorer.exe",
                Size = TileSize.Default
            };
        }

        private TileInfo CreatePinnedProgramTileInfo(string file)
        {
            string targetPath = ShortcutResolver.Resolve(file);
            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                return null;

            var icon = IconExtractor.Extract(targetPath, 0);
            var bitmapImage = icon != null ? IconExtractor.ToBitmapSource(icon) : null;

            return new TileInfo
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Image = bitmapImage,
                Background = null,
                FilePath = file,
                Size = TileSize.Default
            };
        }

        private void GridsPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Tile)))
            {
                Tile droppedTile = e.Data.GetData(typeof(Tile)) as Tile;
                if (droppedTile != null)
                {
                    Point dropPosition = e.GetPosition(GridsPanel);
                    HitTestResult hitTestResult = VisualTreeHelper.HitTest(GridsPanel, dropPosition);
                    if (hitTestResult != null)
                    {
                        Tile targetTile = FindParent<Tile>(hitTestResult.VisualHit);
                        if (targetTile != null && !ReferenceEquals(droppedTile, targetTile))
                        {
                            MoveTile(droppedTile, targetTile);
                        }
                    }
                }
            }
        }

        private void MoveTile(Tile sourceTile, Tile targetTile)
        {
            var sourceTileInfo = _allTiles.FirstOrDefault(t => t.Name == sourceTile.TileName);
            var targetTileInfo = _allTiles.FirstOrDefault(t => t.Name == targetTile.TileName);

            if (sourceTileInfo != null && targetTileInfo != null)
            {
                int sourceIndex = _allTiles.IndexOf(sourceTileInfo);
                int targetIndex = _allTiles.IndexOf(targetTileInfo);

                _allTiles.RemoveAt(sourceIndex);
                _allTiles.Insert(targetIndex, sourceTileInfo);

                ReorganizeTiles();
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

        private Tile CreateDesktopTile()
        {
            var desktopTile = new Tile
            {
                TileName = "Desktop",
                TileImage = null,
                TileBackground = new BitmapImage(new Uri(GetWallpaperPath()))
            };

            desktopTile.MouseLeftButtonUp += (s, e) => LaunchDesktop();
            return desktopTile;
        }

        private void LaunchDesktop()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "shell:::{3080F90D-D7AD-11D9-BD98-0000947B0257}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch desktop: {ex.Message}");
            }
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
                TileImage = bitmapImage,
                TileBackground = null
            };

            tile.MouseRightButtonUp += (s, e) => ShowTileContextMenu(tile, file);
            tile.MouseLeftButtonUp += (s, e) => LaunchProgram(file);

            return tile;
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


        private void ShowTileContextMenu(Tile tile, string filePath)
        {
            var contextMenu = new ContextMenu();

            var unpinMenuItem = new MenuItem { Header = "Unpin from Start" };
            unpinMenuItem.Click += (s, e) => UnpinFromStartMenu(filePath);
            contextMenu.Items.Add(unpinMenuItem);

            var sizeMenu = new MenuItem { Header = "Resize" };
            sizeMenu.Items.Add(CreateSizeMenuItem("Small", TileSize.Default, tile, filePath));
            sizeMenu.Items.Add(CreateSizeMenuItem("Wide", TileSize.Wide, tile, filePath));
            sizeMenu.Items.Add(CreateSizeMenuItem("Large", TileSize.Large, tile, filePath));
            contextMenu.Items.Add(sizeMenu);

            contextMenu.IsOpen = true;
        }

        private MenuItem CreateSizeMenuItem(string header, TileSize size, Tile tile, string filePath)
        {
            var menuItem = new MenuItem { Header = header };
            menuItem.Click += (s, e) =>
            {
                var tileInfo = _allTiles.FirstOrDefault(t => t.FilePath == filePath);
                if (tileInfo != null)
                {
                    tileInfo.Size = size;
                    tile.TileSize = size;
                    ReorganizeTiles();
                }
            };
            return menuItem;
        }

        private void ReorganizeTiles()
        {
            GridsPanel.Children.Clear();

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var currentGrid = CreateNewGrid();
            var occupiedCells = new List<List<bool>> { new List<bool> { false, false, false, false } };

            foreach (var tileInfo in _allTiles)
            {
                var tile = CreateTile(tileInfo);

                int columnSpan = 1;
                int rowSpan = 1;

                switch (tile.TileSize)
                {
                    case TileSize.Default:
                        tile.Width = 124;
                        tile.Height = 124;
                        break;
                    case TileSize.Wide:
                        tile.Width = 248;
                        tile.Height = 124;
                        columnSpan = 2;
                        break;
                    case TileSize.Large:
                        tile.Width = 248;
                        tile.Height = 248;
                        columnSpan = 2;
                        rowSpan = 2;
                        break;
                }

                int row = 0;
                int col = -1;

                while (col == -1)
                {
                    col = FindNextAvailableColumn(occupiedCells, row, columnSpan, rowSpan);
                    if (col == -1)
                    {
                        row++;
                        if (row >= occupiedCells.Count)
                        {
                            occupiedCells.Add(new List<bool> { false, false, false, false });
                            currentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(124) });
                        }
                    }
                }

                if (row >= 4)
                {
                    stackPanel.Children.Add(currentGrid);
                    currentGrid = CreateNewGrid();
                    occupiedCells = new List<List<bool>> { new List<bool> { false, false, false, false } };
                    row = 0;
                    col = 0;
                }

                Grid.SetRow(tile, row);
                Grid.SetColumn(tile, col);
                Grid.SetColumnSpan(tile, columnSpan);
                Grid.SetRowSpan(tile, rowSpan);

                for (int r = 0; r < rowSpan; r++)
                {
                    while (row + r >= occupiedCells.Count)
                    {
                        occupiedCells.Add(new List<bool> { false, false, false, false });
                        currentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(124) });
                    }
                    for (int c = 0; c < columnSpan; c++)
                    {
                        occupiedCells[row + r][col + c] = true;
                    }
                }

                currentGrid.Children.Add(tile);
            }

            stackPanel.Children.Add(currentGrid);

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = stackPanel,
                MaxHeight = 500
            };

            scrollViewer.HorizontalAlignment = HorizontalAlignment.Left;

            var scrollViewerContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scrollViewerContainer.Children.Add(scrollViewer);

            GridsPanel.Children.Add(scrollViewerContainer);

            GridsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        private Grid CreateNewGrid()
        {
            var grid = new Grid();
            for (int i = 0; i < 4; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(124) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(124) });
            }
            return grid;
        }



        private int FindNextAvailableColumn(List<List<bool>> occupiedCells, int row, int columnSpan, int rowSpan)
        {
            for (int col = 0; col <= 4 - columnSpan; col++)
            {
                bool isAvailable = true;
                for (int r = 0; r < rowSpan; r++)
                {
                    if (row + r >= occupiedCells.Count)
                    {
                        continue;
                    }
                    for (int c = 0; c < columnSpan; c++)
                    {
                        if (occupiedCells[row + r][col + c])
                        {
                            isAvailable = false;
                            break;
                        }
                    }
                    if (!isAvailable) break;
                }
                if (isAvailable) return col;
            }
            return -1;
        }



        private List<TileInfo> _allTiles = new List<TileInfo>();
        private void AddTileToGrid(Tile tile)
        {
            UniformGrid lastGrid = GridsPanel.Children.OfType<UniformGrid>().LastOrDefault();
            if (lastGrid == null || !CanFitTile(lastGrid, tile))
            {
                lastGrid = new UniformGrid { Columns = 2, Rows = 4 };
                lastGrid.Margin = new Thickness(10);
                GridsPanel.Children.Add(lastGrid);
            }

            lastGrid.Children.Add(tile);
        }

        private bool CanFitTile(UniformGrid grid, Tile tile)
        {
            int occupiedCells = grid.Children.Cast<Tile>().Sum(t => GetTileOccupiedCells(t));
            int newTileCells = GetTileOccupiedCells(tile);
            return occupiedCells + newTileCells <= grid.Columns * grid.Rows;
        }


        private int GetTileOccupiedCells(Tile tile)
        {
            switch (tile.TileSize)
            {
                case TileSize.Default:
                    return 1;
                case TileSize.Wide:
                    return 2;
                case TileSize.Large:
                    return 4;
                default:
                    return 1;
            }
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

        public class TileInfo
        {
            public string Name { get; set; }
            public ImageSource Image { get; set; }
            public ImageSource Background { get; set; }
            public string FilePath { get; set; }
            public TileSize Size { get; set; }
            public Tile Tile { get; set; }
        }
    }
}