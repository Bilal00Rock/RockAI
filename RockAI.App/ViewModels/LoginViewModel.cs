using RockAI.Application.Common.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RockAI.App.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserSession _userSession;

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

    public event EventHandler? LoginSucceeded;
    public LoginViewModel(IAuthenticationService authenticationService, IUserSession userSession)
    {
        _authenticationService = authenticationService;
        _userSession = userSession;

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
            if (result.IsError)
            {
                ErrorMessage = result.Errors.First().Description;
                return;
            }

            await _userSession.SetAsync(result.Value);
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
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