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

        public Tile()
        {
            InitializeComponent();
            this.MouseMove += Tile_MouseMove;
            this.MouseLeftButtonDown += Tile_MouseLeftButtonDown;
            this.MouseLeftButtonUp += Tile_MouseLeftButtonUp;
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
    }
}