using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

public class IconExtractor
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_SYSICONINDEX = 0x000004000;
    private const int SHIL_EXTRALARGE = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }
    private const uint SHGFI_ICON = 0x000000100;

    [DllImport("shell32.dll", EntryPoint = "#727")]
    private static extern int SHGetImageList(int iImageList, ref Guid riid, ref IImageList ppv);
    private static Guid IID_IImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");

    [ComImport]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IImageList
    {
        int Dummy(); // This method is just a placeholder.
        int GetIcon(int i, int flags, out IntPtr picon);
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    extern static int SHDefExtractIcon(string pszIconFile, int iIndex, int uFlags, out IntPtr phiconLarge, IntPtr phiconSmall, int nIconSize);
    public static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(IntPtr hIcon);
    }

    public static BitmapSource ToBitmapSource(string filePath, int size, bool big = false)
    {
        IntPtr hIcon;
        if (SHDefExtractIcon(filePath, 0, 0, out hIcon, IntPtr.Zero, big ? 64 : 48) == 0)
        {
            try
            {
                Icon extractedIcon = Icon.FromHandle(hIcon);
                return Imaging.CreateBitmapSourceFromHIcon(
                    extractedIcon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                // Dispose the icon to prevent resource leaks
                NativeMethods.DestroyIcon(hIcon);
            }
        }
        return null; // Failure
    }

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);
}
