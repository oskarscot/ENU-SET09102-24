using SET09102.Administrator.Services;
using SET09102.Administrator.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SET09102.Administrator.Pages
{
    public partial class UserManagementPage : ContentPage, INotifyPropertyChanged
    {
        private readonly UserService _userService;
        public ObservableCollection<User> Users { get; set; }
        public string NewUsername { get; set; }
        public string NewEmail { get; set; }
        public Role NewRole { get; set; }
        public List<Role> Roles { get; set; }

        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand EditUserCommand { get; }

        public UserManagementPage(UserService userService)
        {
            InitializeComponent();
            _userService = userService;
            Users = new ObservableCollection<User>();
            Roles = new List<Role> 
            { 
                new Role { Id = 1, Name = "Admin", Description = "System Administrator" },
                new Role { Id = 2, Name = "Scientist", Description = "Environmental Scientist" },
                new Role { Id = 3, Name = "Manager", Description = "Operations Manager" }
            };

            AddUserCommand = new Command(async () => await AddUser());
            DeleteUserCommand = new Command<User>(async (user) => await DeleteUser(user));
            EditUserCommand = new Command<User>(async (user) => await EditUser(user));

            LoadUsers();
        }

        private async void LoadUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                Users.Clear();
                foreach (var user in users)
                    Users.Add(user);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load users: " + ex.Message, "OK");
            }
        }

        private async Task AddUser()
        {
            if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewEmail) || NewRole == null)
            {
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            try
            {
                var user = new User
                {
                    Username = NewUsername,
                    Email = NewEmail,
                    Role = NewRole,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _userService.CreateUserAsync(user);
                Users.Add(user);
                ClearForm();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to add user: " + ex.Message, "OK");
            }
        }

        private async Task DeleteUser(User user)
        {
            if (user == null) return;

            var result = await DisplayAlert("Confirm", "Are you sure you want to delete this user?", "Yes", "No");
            if (result)
            {
                try
                {
                    await _userService.DeleteUserAsync(user.Id);
                    Users.Remove(user);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Failed to delete user: " + ex.Message, "OK");
                }
            }
        }

        private async Task EditUser(User user)
        {
            if (user == null) return;

            // TODO: Implement edit user functionality
            await DisplayAlert("Info", "Edit user functionality coming soon", "OK");
        }

        private void ClearForm()
        {
            NewUsername = string.Empty;
            NewEmail = string.Empty;
            NewRole = null;
            OnPropertyChanged(nameof(NewUsername));
            OnPropertyChanged(nameof(NewEmail));
            OnPropertyChanged(nameof(NewRole));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}