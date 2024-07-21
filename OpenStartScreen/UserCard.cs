using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

public class UserCard : INotifyPropertyChanged
{
    private string _username;
    private BitmapImage _userProfileImage;

    public string Username
    {
        get { return _username; }
        set
        {
            _username = value;
            OnPropertyChanged(nameof(Username));
        }
    }

    public BitmapImage UserProfileImage
    {
        get { return _userProfileImage; }
        set
        {
            _userProfileImage = value;
            OnPropertyChanged(nameof(UserProfileImage));
        }
    }

    public ICommand OpenContextMenuCommand { get; }
    public ICommand ChangeAccountPictureCommand { get; }
    public ICommand LockCommand { get; }
    public ICommand SignOutCommand { get; }

    public UserCard()
    {
        Username = GetUsername();
        UserProfileImage = GetUserProfileImage();

        OpenContextMenuCommand = new RelayCommand(OpenContextMenu);
        ChangeAccountPictureCommand = new RelayCommand(ChangeAccountPicture);
        LockCommand = new RelayCommand(Lock);
        SignOutCommand = new RelayCommand(SignOut);
    }

    private string GetUsername()
    {
        return WindowsIdentity.GetCurrent().Name.Split('\\')[1];
    }

    private BitmapImage GetUserProfileImage()
    {
        string userImagePath = GetUserProfilePicturePath();
        if (File.Exists(userImagePath))
        {
            return LoadImage(userImagePath);
        }
        else
        {
            string defaultImagePath = @"C:\ProgramData\Microsoft\User Account Pictures\user-192.png";
            if (File.Exists(defaultImagePath))
            {
                return LoadImage(defaultImagePath);
            }
        }
        return null;
    }

    private BitmapImage LoadImage(string imagePath)
    {
        BitmapImage bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imagePath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        return bitmap;
    }

    private string GetUserProfilePicturePath()
    {
        string profilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string imagePath = Path.Combine(profilePath, "AppData", "Roaming", "Microsoft", "Windows", "AccountPictures");
        if (Directory.Exists(imagePath))
        {
            var files = Directory.GetFiles(imagePath, "*.jpg");
            if (files.Length > 0)
            {
                return files[0]; 
            }
        }
        return null;
    }

    private void OpenContextMenu(object parameter)
    {
        if (parameter is Button button)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    private void Lock(object parameter)
    {
        LockWorkStation();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    private const uint EWX_LOGOFF = 0x00000000;

    private void SignOut(object parameter)
    {
        ExitWindowsEx(EWX_LOGOFF, 0);
    }

    private void ChangeAccountPicture(object parameter)
    {
        Process.Start(new ProcessStartInfo("ms-settings:yourinfo") { UseShellExecute = true });
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
