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


        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}