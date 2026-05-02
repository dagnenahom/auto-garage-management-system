using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using SmartGarageSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartGarageSystem.ViewModels
{
    public class UserManagementViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly IUserSession _userSession;
        private bool _isUpdating = false;

        // List of all users
        public ObservableCollection<User> Users { get; } = new ObservableCollection<User>();

        // Selected user for editing
        private User _selectedUser;
        public User SelectedUser
        {
            get => _selectedUser;
            set 
            {
                if(_selectedUser == value) return;
                _selectedUser = value;
                OnPropertyChanged();
                if (!_isUpdating)
                {
                    LoadEditUserData();
                }
            }
        }

        // Fields for add/edit form
        private string _editUsername;
        public string EditUsername
        {
            get => _editUsername;
            set { _editUsername = value; OnPropertyChanged(); }
        }

        private string _editFullName;
        public string EditFullName
        {
            get => _editFullName;
            set { _editFullName = value; OnPropertyChanged(); }
        }

        private string _editPassword;
        public string EditPassword
        {
            get => _editPassword;
            set { _editPassword = value; OnPropertyChanged(); }
        }

        // Active roles with IsSelected for checkboxes
        public ObservableCollection<SelectableRole> AvailableRoles { get; } = new ObservableCollection<SelectableRole>();

        private string _formMessage;
        public string FormMessage
        {
            get => _formMessage;
            set { _formMessage = value; OnPropertyChanged(); }
        }

        public bool IsEditing => SelectedUser != null;
        public ICommand NewUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand RefreshCommand { get; }

        public UserManagementViewModel(IUserService userService, IUserSession userSession)
        {
            _userService = userService;
            _userSession = userSession;

            NewUserCommand = new RelayCommand(_ => ClearEdit());
            SaveUserCommand = new RelayCommand(async _ => await SaveUser());
            DeleteUserCommand = new RelayCommand(async _ => await DeleteUser(), _ => SelectedUser != null && SelectedUser.UserId != _userSession.CurrentUser.UserId);
            CancelEditCommand = new RelayCommand(_ => ClearEdit());
            RefreshCommand = new RelayCommand(async _ => await LoadUsers());

            // Load initial data
            Task.Run(async () =>
            {
                await LoadRoles();
                await LoadUsers();
            });
        }

        private async Task LoadRoles()
        {
            var roles = await _userService.GetAllActiveRolesAsync();
            AvailableRoles.Clear();
            foreach (var role in roles)
                AvailableRoles.Add(new SelectableRole { Role = role, IsSelected = false });
        }

        private async Task LoadUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
                Users.Add(user);
        }

        private void ClearEdit()
        {
            _isUpdating = true;
            EditUsername = string.Empty;
            EditFullName = string.Empty;
            EditPassword = string.Empty;
            FormMessage = string.Empty;
            foreach (var r in AvailableRoles) r.IsSelected = false;
            
            SelectedUser = null;
            _isUpdating = false;

            OnPropertyChanged(nameof(IsEditing));
        }

        private void LoadEditUserData()
        {
            if (SelectedUser == null)
            {
                ClearEdit();
                return;
            }

            EditUsername = SelectedUser.Username;
            EditFullName = SelectedUser.FullName;
            EditPassword = string.Empty; // leave blank to keep old password

            // Mark the user's current roles
            foreach (var av in AvailableRoles)
                av.IsSelected = SelectedUser.Roles.Any(r => r.RoleId == av.Role.RoleId);

            OnPropertyChanged(nameof(IsEditing));
        }

        private async Task SaveUser()
        {
            FormMessage = "Saving...";

            FormMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(EditUsername))
            {
                FormMessage = "Username is required.";
                return;
            }

            // Check duplicate (if editing, skip self)
            bool exists = await _userService.UsernameExistsAsync(EditUsername);
            if (exists && (SelectedUser == null || SelectedUser.Username != EditUsername))
            {
                FormMessage = "Username already exists.";
                return;
            }

            var roleIds = AvailableRoles.Where(r => r.IsSelected).Select(r => r.Role.RoleId).ToList();
            if (roleIds.Count == 0)
            {
                FormMessage = "Please select at least one role.";
                return;
            }

            try
            {
                if (SelectedUser == null)   // Add new
                {
                    var newUser = new User
                    {
                        Username = EditUsername,
                        FullName = EditFullName,
                        PasswordHash = EditPassword   // will be hashed in service
                    };
                    await _userService.CreateUserAsync(newUser, roleIds);
                    FormMessage = "User created.";
                }
                else   // Update
                {
                    var updatedUser = new User
                    {
                        UserId = SelectedUser.UserId,
                        Username = EditUsername,
                        FullName = EditFullName,
                        PasswordHash = string.IsNullOrWhiteSpace(EditPassword) ? null : EditPassword
                    };
                    await _userService.UpdateUserAsync(updatedUser, roleIds);
                    FormMessage = "User updated.";
                }

                await LoadUsers();
                ClearEdit();
            }
            catch (Exception ex)
            {
                FormMessage = "Error: " + ex.Message;
            }
        }

        private async Task DeleteUser()
        {
            if (SelectedUser == null) return;
            if (SelectedUser.UserId == _userSession.CurrentUser.UserId)
            {
                FormMessage = "Cannot delete yourself.";
                return;
            }

            await _userService.DeleteUserAsync(SelectedUser.UserId);
            await LoadUsers();
            ClearEdit();
            FormMessage = "User deleted.";
        }
    }

    // Helper class for checkboxes
    public class SelectableRole : BaseViewModel
    {
        public Role Role { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }
}