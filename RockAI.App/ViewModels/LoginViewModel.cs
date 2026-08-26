using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace RockAI.App.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{

    public LoginViewModel()
    {
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set { SetProperty(ref _email, value); }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set { SetProperty(ref _password, value); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((Command)LoginCommand).ChangeCanExecute();
            }
        }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { SetProperty(ref _errorMessage, value); }
    }

    public ICommand LoginCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private async Task ExecuteLoginAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
           
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
