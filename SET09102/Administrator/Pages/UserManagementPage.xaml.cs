using SET09102.Services.Administration;
using SET09102.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SET09102.Administrator.Pages
{
    public partial class UserManagementPage : ContentPage, INotifyPropertyChanged
    {
        private readonly UserService _userService;
        private ObservableCollection<User> _users;
        private string _newUsername;
        private Role _newRole;
        private List<Role> _roles;

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        public string NewUsername
        {
            get => _newUsername;
            set
            {
                _newUsername = value;
                OnPropertyChanged();
            }
        }

        public Role NewRole
        {
            get => _newRole;
            set
            {
                _newRole = value;
                OnPropertyChanged();
            }
        }

        public List<Role> Roles
        {
            get => _roles;
            set
            {
                _roles = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddUserCommand { get; }

        public UserManagementPage(UserService userService)
        {
            InitializeComponent();
            _userService = userService;
            _users = new ObservableCollection<User>();
            _roles = new List<Role>();
            AddUserCommand = new Command(async () => await AddUserAsync(), () => CanAddUser());
            BindingContext = this;
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            await LoadRolesAsync();
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load users: {ex.Message}", "OK");
            }
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                var roles = await _userService.GetAllRolesAsync();
                Roles = roles;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load roles: {ex.Message}", "OK");
                Roles = new List<Role> { new Role { Id = 1, Name = "Admin" }, new Role { Id = 2, Name = "Scientist" } }; // Fallback to mock
            }
        }

        private async Task AddUserAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewUsername) || NewRole == null)
                {
                    await DisplayAlert("Error", "Please provide a username and select a role.", "OK");
                    return;
                }

                var newUser = new User
                {
                    Username = NewUsername,
                    PasswordHash = "defaultHash", // In a real app, hash a default password or prompt for one
                    RoleId = NewRole.Id
                };

                await _userService.AddUserAsync(newUser);
                await DisplayAlert("Success", "User added successfully.", "OK");
                NewUsername = string.Empty;
                NewRole = null;
                await LoadUsersAsync(); // Refresh the user list
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add user: {ex.Message}", "OK");
            }
        }

        private bool CanAddUser()
        {
            return !string.IsNullOrWhiteSpace(NewUsername) && NewRole != null;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        private new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}