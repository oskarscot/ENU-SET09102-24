using SET09102.Services.Administration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SET09102.Administrator.Pages
{
    public partial class LoginPage : ContentPage, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        public string Username { get; set; }
        public string Password { get; set; }
        public string LockoutMessage { get; set; }
        public bool IsLockedOut => !string.IsNullOrEmpty(LockoutMessage);

        public LoginPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        public ICommand LoginCommand => new Command(async () =>
        {
            if (await _authService.LoginAsync(Username, Password))
                await Navigation.PushAsync(new UserManagementPage(new UserService(new SqlConnection("YourConnectionString"))));
            else
                LockoutMessage = "Login failed. Try again or check lockout.";
            OnPropertyChanged(nameof(LockoutMessage));
            OnPropertyChanged(nameof(IsLockedOut));
        });

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}