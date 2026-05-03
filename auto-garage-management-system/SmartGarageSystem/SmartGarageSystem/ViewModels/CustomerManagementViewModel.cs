using SmartGarageSystem.Models;
using SmartGarageSystem.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartGarageSystem.ViewModels
{
    public class CustomerManagementViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;
        private bool _isUpdating;   // guard against recursion

        // --- List & Selection ---
        public ObservableCollection<Customer> Customers { get; } = new ObservableCollection<Customer>();

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer == value) return;
                _selectedCustomer = value;
                OnPropertyChanged();
                if (!_isUpdating)
                    LoadEditCustomerData();
            }
        }

        // --- Edit Form Fields ---
        private string _editFullName;
        public string EditFullName
        {
            get => _editFullName;
            set { _editFullName = value; OnPropertyChanged(); }
        }

        private string _editPhone;
        public string EditPhone
        {
            get => _editPhone;
            set { _editPhone = value; OnPropertyChanged(); }
        }

        private string _editEmail;
        public string EditEmail
        {
            get => _editEmail;
            set { _editEmail = value; OnPropertyChanged(); }
        }

        private string _editAddress;
        public string EditAddress
        {
            get => _editAddress;
            set { _editAddress = value; OnPropertyChanged(); }
        }

        private string _formMessage;
        public string FormMessage
        {
            get => _formMessage;
            set { _formMessage = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        public bool IsEditing => SelectedCustomer != null;

        // --- Commands ---
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand NewCustomerCommand { get; }
        public ICommand SaveCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand RefreshCommand { get; }

        public CustomerManagementViewModel(ICustomerService customerService)
        {
            _customerService = customerService;

            // Search
            SearchCommand = new RelayCommand(async _ => await SearchCustomers());
            ClearSearchCommand = new RelayCommand(async _ =>
            {
                SearchText = string.Empty;
                await LoadAllCustomers();
            });

            // CRUD
            NewCustomerCommand = new RelayCommand(_ => ClearEdit());
            SaveCustomerCommand = new RelayCommand(async _ => await SaveCustomer());
            DeleteCustomerCommand = new RelayCommand(async _ => await DeleteCustomer(), _ => SelectedCustomer != null);
            CancelEditCommand = new RelayCommand(_ => ClearEdit());
            RefreshCommand = new RelayCommand(async _ =>
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    await LoadAllCustomers();
                else
                    await SearchCustomers();
            });

            // Initial load
            Task.Run(async () => await LoadAllCustomers());
        }

        private async Task LoadAllCustomers()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            Customers.Clear();
            foreach (var c in customers)
                Customers.Add(c);
        }

        private async Task SearchCustomers()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadAllCustomers();
                return;
            }
            var results = await _customerService.SearchCustomersAsync(SearchText);
            Customers.Clear();
            foreach (var c in results)
                Customers.Add(c);
        }

        public void ClearEdit()
        {
            _isUpdating = true;

            EditFullName = string.Empty;
            EditPhone = string.Empty;
            EditEmail = string.Empty;
            EditAddress = string.Empty;
            FormMessage = string.Empty;

            SelectedCustomer = null;   // safe because of _isUpdating

            _isUpdating = false;
            OnPropertyChanged(nameof(IsEditing));
        }

        public void LoadEditCustomerData()
        {
            if (SelectedCustomer == null)
            {
                ClearEdit();
                return;
            }
            EditFullName = SelectedCustomer.FullName;
            EditPhone = SelectedCustomer.Phone;
            EditEmail = SelectedCustomer.Email;
            EditAddress = SelectedCustomer.Address;
            OnPropertyChanged(nameof(IsEditing));
        }

        private async Task SaveCustomer()
        {
            FormMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(EditFullName))
            {
                FormMessage = "Name is required.";
                return;
            }

            try
            {
                if (SelectedCustomer == null)  // new
                {
                    var newCust = new Customer
                    {
                        FullName = EditFullName,
                        Phone = EditPhone,
                        Email = EditEmail,
                        Address = EditAddress
                    };
                    await _customerService.CreateCustomerAsync(newCust);
                    FormMessage = "Customer created.";
                }
                else  // update
                {
                    var updated = new Customer
                    {
                        CustomerId = SelectedCustomer.CustomerId,
                        FullName = EditFullName,
                        Phone = EditPhone,
                        Email = EditEmail,
                        Address = EditAddress
                    };
                    await _customerService.UpdateCustomerAsync(updated);
                    FormMessage = "Customer updated.";
                }

                await LoadAllCustomers();
                ClearEdit();
            }
            catch (Exception ex)
            {
                FormMessage = "Error: " + ex.Message;
            }
        }

        private async Task DeleteCustomer()
        {
            if (SelectedCustomer == null) return;
            try
            {
                await _customerService.DeleteCustomerAsync(SelectedCustomer.CustomerId);
                await LoadAllCustomers();
                ClearEdit();
                FormMessage = "Customer deleted.";
            }
            catch (Exception ex)
            {
                FormMessage = "Error: " + ex.Message;
            }
        }
    }

}
