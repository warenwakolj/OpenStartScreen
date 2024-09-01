using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public static readonly DependencyProperty TileSizeProperty =
            DependencyProperty.Register("TileSize", typeof(TileSize), typeof(Tile), new PropertyMetadata(TileSize.Default));

        public Tile()
        {
            InitializeComponent();
            UpdateGradientBrush();

            this.MouseMove += Tile_MouseMove;
        }

        private void Tile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
            }
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

        public TileSize TileSize
        {
            get => (TileSize)GetValue(TileSizeProperty);
            set => SetValue(TileSizeProperty, value);
        }

        public double ImageSize => TileSize == TileSize.Large ? 72 : 44;

        private static void OnTileSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Tile tile)
            {
                tile.UpdateSize();
            }
        }

        private void UpdateSize()
        {
            UpdateGradientBrush();
            switch (TileSize)
            {
                case TileSize.Default:
                    Width = 124;
                    Height = 124;
                    break;
                case TileSize.Wide:
                    Width = 248;
                    Height = 124;
                    break;
                case TileSize.Large:
                    Width = 248;
                    Height = 248;
                    break;
            }
        }
    }



    public enum TileSize
    {
        Default,
        Wide,
        Large
    }
}