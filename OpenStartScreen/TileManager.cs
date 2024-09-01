using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenStartScreen;

public partial class MainWindow
{
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