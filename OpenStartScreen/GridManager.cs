using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace OpenStartScreen;

public partial class MainWindow
{
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
}