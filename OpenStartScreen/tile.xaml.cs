using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenStartScreen
{
    public partial class Tile : UserControl
    {
        public static readonly DependencyProperty TileNameProperty =
            DependencyProperty.Register("TileName", typeof(string), typeof(Tile), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TileImageProperty =
            DependencyProperty.Register("TileImage", typeof(ImageSource), typeof(Tile), new PropertyMetadata(null));

        public static readonly DependencyProperty TileBackgroundProperty =
            DependencyProperty.Register("TileBackground", typeof(ImageSource), typeof(Tile), new PropertyMetadata(null));

        public static readonly DependencyProperty TileWidthProperty =
            DependencyProperty.Register("TileWidth", typeof(double), typeof(Tile), new PropertyMetadata(126.0));

        public static readonly DependencyProperty TileHeightProperty =
            DependencyProperty.Register("TileHeight", typeof(double), typeof(Tile), new PropertyMetadata(126.0));

        public Tile()
        {
            InitializeComponent();
            this.MouseMove += Tile_MouseMove;
            this.MouseLeftButtonDown += Tile_MouseLeftButtonDown;
            this.MouseLeftButtonUp += Tile_MouseLeftButtonUp;

            UpdateGradientBrush();
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
        }

        private void UpdateGradientBrush()
        {
            SolidColorBrush accentBrush = SystemParameters.WindowGlassBrush as SolidColorBrush;
            if (accentBrush != null)
            {
                Color baseColor = accentBrush.Color;
                Color darkerColor = DarkenColor(baseColor, 0.1);

                var gradientBrush = (LinearGradientBrush)FindResource("TileGradientBrush");
                if (gradientBrush != null)
                {
                    gradientBrush.GradientStops[1].Color = baseColor;
                    gradientBrush.GradientStops[0].Color = darkerColor;
                }
            }
        }

        private Color DarkenColor(Color color, double factor)
        {
            byte r = (byte)(color.R * (1 - factor));
            byte g = (byte)(color.G * (1 - factor));
            byte b = (byte)(color.B * (1 - factor));
            return Color.FromRgb(r, g, b);
        }

        public string TileName
        {
            get => (string)GetValue(TileNameProperty);
            set => SetValue(TileNameProperty, value);
        }

        public ImageSource TileImage
        {
            get => (ImageSource)GetValue(TileImageProperty);
            set => SetValue(TileImageProperty, value);
        }

        public ImageSource TileBackground
        {
            get => (ImageSource)GetValue(TileBackgroundProperty);
            set => SetValue(TileBackgroundProperty, value);
        }

        public double TileWidth
        {
            get => (double)GetValue(TileWidthProperty);
            set => SetValue(TileWidthProperty, value);
        }

        public double TileHeight
        {
            get => (double)GetValue(TileHeightProperty);
            set => SetValue(TileHeightProperty, value);
        }

        private bool isDragging = false;
        private Point startPoint;

        private void Tile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
            isDragging = true;
        }

        private void Tile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
            }
        }

        private void Tile_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(null);

                if (Math.Abs(currentPosition.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPosition.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    isDragging = false;

                    DataObject data = new DataObject(typeof(Tile), this);
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                }
            }
        }

        private void SystemParameters_StaticPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemParameters.WindowGlassBrush))
            {
                UpdateGradientBrush();
            }
        }

        public void ResizeTile(double width, double height)
        {
            TileWidth = width;
            TileHeight = height;
            this.Width = width;
            this.Height = height;
        }
    }
}
