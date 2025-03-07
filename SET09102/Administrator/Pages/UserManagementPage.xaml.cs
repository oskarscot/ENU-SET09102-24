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
        public ObservableCollection<User> Users { get; set; }
        public string NewUsername { get; set; }
        public Role NewRole { get; set; }
        public List<Role> Roles { get; set; }

        public UserManagementPage(UserService userService)
        {
            InitializeComponent();
            _userService = userService;
            Users = new ObservableCollection<User>();
            Roles = new List<Role> { new Role { Id = 1, Name = "Admin" }, new Role { Id = 2, Name = "Scientist" } }; // Mock
            LoadUsers();
        }

        private async void LoadUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
                Users.Add(user);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}