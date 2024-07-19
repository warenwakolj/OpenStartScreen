using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

public class IconExtractor
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    public static Icon Extract(string filePath, int iconIndex)
    {
        IntPtr hIcon = ExtractIcon(IntPtr.Zero, filePath, iconIndex);
        if (hIcon != IntPtr.Zero)
        {
            return Icon.FromHandle(hIcon);
        }
        return null;
    }

    public static BitmapSource ToBitmapSource(Icon icon)
    {
        using (var bmp = icon.ToBitmap())
        {
            var hBitmap = bmp.GetHbitmap();

            try
            {
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                return bitmapSource;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);
}
