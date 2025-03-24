using SET09102.Services.Administration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace SET09102.Administrator.Pages
{
    public partial class LoginPage : ContentPage, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private string? _username;
        private string? _password;
        private string? _lockoutMessage;

        public string? Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string? Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string? LockoutMessage
        {
            get => _lockoutMessage;
            set { _lockoutMessage = value; OnPropertyChanged(); }
        }

        public bool IsLockedOut => !string.IsNullOrEmpty(LockoutMessage);

        public ICommand LoginCommand { get; }

        public LoginPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            BindingContext = this;

            LoginCommand = new Command(async () =>
            {
                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    LockoutMessage = "Please enter both username and password.";
                    return;
                }

                bool isAuthenticated = await _authService.LoginAsync(Username, Password);
                if (isAuthenticated)
                {
                    LockoutMessage = string.Empty;
                    // Navigate to UserManagementPage
                    await Navigation.PushAsync(new UserManagementPage(new UserService(new SqlConnection("Server=localhost;Database=EnvMonitoring;Trusted_Connection=True;"))));
                }
                else
                {
                    LockoutMessage = "Login failed. Check credentials or wait if locked out.";
                }
            });
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        private new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(LockoutMessage))
                OnPropertyChanged(nameof(IsLockedOut));
        }
    }
}