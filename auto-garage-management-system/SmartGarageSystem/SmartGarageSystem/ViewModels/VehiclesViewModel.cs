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
    public class VehiclesViewModel : BaseViewModel
    {
        private readonly IVehicleService _vehicleService;
        private bool _isUpdating;

        // --- Vehicle list & selection ---
        public ObservableCollection<Vehicle> Vehicles { get; } = new ObservableCollection<Vehicle>();

        private Vehicle _selectedVehicle;
        public Vehicle SelectedVehicle
        {
            get => _selectedVehicle;
            set
            {
                if (_selectedVehicle == value) return;
                _selectedVehicle = value;
                OnPropertyChanged();
                if (!_isUpdating)
                    LoadEditVehicleData();
            }
        }

        // --- Edit form fields ---
        private string _editLicensePlate;
        public string EditLicensePlate
        {
            get => _editLicensePlate;
            set { _editLicensePlate = value; OnPropertyChanged(); }
        }

        private string _editMake;
        public string EditMake
        {
            get => _editMake;
            set { _editMake = value; OnPropertyChanged(); }
        }

        private string _editModel;
        public string EditModel
        {
            get => _editModel;
            set { _editModel = value; OnPropertyChanged(); }
        }

        private int? _editYear;
        public int? EditYear
        {
            get => _editYear;
            set { _editYear = value; OnPropertyChanged(); }
        }

        private string _editColor;
        public string EditColor
        {
            get => _editColor;
            set { _editColor = value; OnPropertyChanged(); }
        }

        private string _editVIN;
        public string EditVIN
        {
            get => _editVIN;
            set { _editVIN = value; OnPropertyChanged(); }
        }

        
        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set { _selectedCustomer = value; OnPropertyChanged(); }
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

        public bool IsEditing => SelectedVehicle != null;

        // Customers dropdown
        public ObservableCollection<Customer> CustomerList { get; } = new ObservableCollection<Customer>();

        // --- Commands ---
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand NewVehicleCommand { get; }
        public ICommand SaveVehicleCommand { get; }
        public ICommand DeleteVehicleCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand RefreshCommand { get; }

        public VehiclesViewModel(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;

            SearchCommand = new RelayCommand(async _ => await SearchVehicles());
            ClearSearchCommand = new RelayCommand(async _ =>
            {
                SearchText = string.Empty;
                await LoadAllVehicles();
            });

            NewVehicleCommand = new RelayCommand(_ => ClearEdit());
            SaveVehicleCommand = new RelayCommand(async _ => await SaveVehicle());
            DeleteVehicleCommand = new RelayCommand(async _ => await DeleteVehicle(), _ => SelectedVehicle != null);
            CancelEditCommand = new RelayCommand(_ => ClearEdit());
            RefreshCommand = new RelayCommand(async _ =>
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    await LoadAllVehicles();
                else
                    await SearchVehicles();
            });

            // Initial load
            Task.Run(async () =>
            {
                await LoadAllVehicles();
                await LoadCustomers();
            });
        }

        private async Task LoadAllVehicles()
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            Vehicles.Clear();
            foreach (var v in vehicles)
                Vehicles.Add(v);
        }

        private async Task SearchVehicles()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadAllVehicles();
                return;
            }
            var results = await _vehicleService.SearchVehiclesAsync(SearchText);
            Vehicles.Clear();
            foreach (var v in results)
                Vehicles.Add(v);
        }

        private async Task LoadCustomers()
        {
            var customers = await _vehicleService.GetCustomersForDropdownAsync();
            CustomerList.Clear();
            foreach (var c in customers)
                CustomerList.Add(c);
        }

        public void ClearEdit()
        {
            _isUpdating = true;

            EditLicensePlate = string.Empty;
            EditMake = string.Empty;
            EditModel = string.Empty;
            EditYear = null;
            EditColor = string.Empty;
            EditVIN = string.Empty;
            SelectedCustomer = null;
            FormMessage = string.Empty;

            SelectedVehicle = null;   // safe

            _isUpdating = false;
            OnPropertyChanged(nameof(IsEditing));
        }

        public void LoadEditVehicleData()
        {
            if (SelectedVehicle == null)
            {
                ClearEdit();
                return;
            }
            EditLicensePlate = SelectedVehicle.LicensePlate;
            EditMake = SelectedVehicle.Make;
            EditModel = SelectedVehicle.Model;
            EditYear = SelectedVehicle.Year;
            EditColor = SelectedVehicle.Color;
            EditVIN = SelectedVehicle.VIN;

            // Set the customer dropdown
            SelectedCustomer = CustomerList.FirstOrDefault(c => c.CustomerId == SelectedVehicle.CustomerId);
            OnPropertyChanged(nameof(IsEditing));
        }

        private async Task SaveVehicle()
        {
            FormMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(EditLicensePlate) || string.IsNullOrWhiteSpace(EditMake) || string.IsNullOrWhiteSpace(EditModel))
            {
                FormMessage = "License plate, Make and Model are required.";
                return;
            }
            if (SelectedCustomer == null)
            {
                FormMessage = "Please select a customer.";
                return;
            }

            try
            {
                if (SelectedVehicle == null) // new
                {
                    var newVehicle = new Vehicle
                    {
                        CustomerId = SelectedCustomer.CustomerId,
                        LicensePlate = EditLicensePlate,
                        Make = EditMake,
                        Model = EditModel,
                        Year = EditYear,
                        Color = EditColor,
                        VIN = EditVIN
                    };
                    await _vehicleService.CreateVehicleAsync(newVehicle);
                    FormMessage = "Vehicle created.";
                }
                else // update
                {
                    var updated = new Vehicle
                    {
                        VehicleId = SelectedVehicle.VehicleId,
                        CustomerId = SelectedCustomer.CustomerId,
                        LicensePlate = EditLicensePlate,
                        Make = EditMake,
                        Model = EditModel,
                        Year = EditYear,
                        Color = EditColor,
                        VIN = EditVIN
                    };
                    await _vehicleService.UpdateVehicleAsync(updated);
                    FormMessage = "Vehicle updated.";
                }

                await LoadAllVehicles();
                ClearEdit();
            }
            catch (Exception ex)
            {
                FormMessage = "Error: " + ex.Message;
            }
        }

        private async Task DeleteVehicle()
        {
            if (SelectedVehicle == null) return;
            try
            {
                await _vehicleService.DeleteVehicleAsync(SelectedVehicle.VehicleId);
                await LoadAllVehicles();
                ClearEdit();
                FormMessage = "Vehicle deleted.";
            }
            catch (Exception ex)
            {
                FormMessage = "Error: " + ex.Message;
            }
        }
    }

}
