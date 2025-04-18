using Microsoft.Data.SqlClient;
using SET09102.Services.Administration;
using SET09102.Administrator.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SET09102.Administrator.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly string _connectionString;
        private readonly AuditService _auditService;
        private string _username;
        private string _password;
        private string _lockoutMessage;

        public string Username 
        { 
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string Password 
        { 
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string LockoutMessage 
        { 
            get => _lockoutMessage;
            set
            {
                _lockoutMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLockedOut));
            }
        }

        public bool IsLockedOut => !string.IsNullOrEmpty(LockoutMessage);

        public LoginPage(AuthService authService, string connectionString, AuditService auditService)
        {
            InitializeComponent();
            _authService = authService;
            _connectionString = connectionString;
            _auditService = auditService;
            BindingContext = this;
        }

        public ICommand LoginCommand => new Command(async () =>
        {
            if (await _authService.LoginAsync(Username, Password))
                await Navigation.PushAsync(new UserManagementPage(new UserService(_connectionString, _auditService)));
            else
                LockoutMessage = "Login failed. Try again or check lockout.";
        });

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
        }
    }
}