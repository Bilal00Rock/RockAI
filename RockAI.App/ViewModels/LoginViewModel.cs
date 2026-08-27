using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RockAI.Application.Common.Interfaces;

namespace RockAI.App.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthenticationService _authenticationService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value)
                return;

            _email = value;
            OnPropertyChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password == value)
                return;

            _password = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();

            ((Command)LoginCommand).ChangeCanExecute();
        }
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;

        LoginCommand = new Command(
            async () => await LoginAsync(),
            () => !IsBusy);
    }

    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var result = await _authenticationService.LoginAsync(
                Email,
                Password);

            result.Switch(
                user =>
                {
                    Debug.WriteLine(
                        $"Logged in: {user.FirstName} {user.LastName}");
                },
                errors =>
                {
                    ErrorMessage = errors.FirstOrDefault().Description
                                    ?? "Login failed.";
                });
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

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}