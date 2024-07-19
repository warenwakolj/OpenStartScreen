using System;
using System.Runtime.InteropServices;
using IWshRuntimeLibrary;

public class ShortcutResolver
{
    public static string Resolve(string shortcutPath)
    {
        if (System.IO.File.Exists(shortcutPath))
        {
            WshShell shell = new WshShell();
            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            return link.TargetPath;
        }
        return null;
    }
}
