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
    public class JobCardViewModel : BaseViewModel
    {
        private readonly IJobCardService _jobService;
        private bool _isUpdating;

        public ObservableCollection<JobCard> JobCards { get; } = new();
        private JobCard _selectedJobCard;
        public JobCard SelectedJobCard
        {
            get => _selectedJobCard;
            set {
                    if( _selectedJobCard == value) return;
                _selectedJobCard = value;
                    OnPropertyChanged(); 
                if(!_isUpdating)
                    LoadJobCardDetail(); 
            }
        }

        // --- Header fields ---
        private string _editDescription;
        public string EditDescription { get => _editDescription; set { _editDescription = value; OnPropertyChanged(); } }

        private Vehicle _selectedVehicle;
        public Vehicle SelectedVehicle
        {
            get => _selectedVehicle;
            set { _selectedVehicle = value; OnPropertyChanged(); }
        }

        private User _selectedMechanic;
        public User SelectedMechanic
        {
            get => _selectedMechanic;
            set { _selectedMechanic = value; OnPropertyChanged(); }
        }

        private string _selectedStatus;
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); }
        }

        private string _formMessage;
        public string FormMessage { get => _formMessage; set { _formMessage = value; OnPropertyChanged(); } }

        // Dropdown lists
        public ObservableCollection<Vehicle> VehicleList { get; } = new();
        public ObservableCollection<User> MechanicList { get; } = new();
        public ObservableCollection<InventoryItem> InventoryList { get; } = new();
        public List<string> StatusList { get; } = new() { "Open", "InProgress", "Completed", "Closed" };

        // JobCard Items
        public ObservableCollection<JobCardItem> CurrentItems { get; } = new();

        private InventoryItem _selectedInventoryItem;
        public InventoryItem SelectedInventoryItem
        {
            get => _selectedInventoryItem;
            set { _selectedInventoryItem = value; OnPropertyChanged(); }
        }

        private int _addQuantity = 1;
        public int AddQuantity { get => _addQuantity; set { _addQuantity = value; OnPropertyChanged(); } }

        // Commands
        public ICommand NewJobCardCommand { get; }
        public ICommand SaveJobCardCommand { get; }
        public ICommand DeleteJobCardCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LoadDropdownsCommand { get; }  // new

        public JobCardViewModel(IJobCardService jobService)
        {
            _jobService = jobService;

            NewJobCardCommand = new RelayCommand(_ => ClearEdit());
            SaveJobCardCommand = new RelayCommand(async _ => await SaveJobCard());
            DeleteJobCardCommand = new RelayCommand(async _ => await DeleteJobCard());
            AddItemCommand = new RelayCommand(async _ => await AddItem());
            RemoveItemCommand = new RelayCommand(async param =>
            {
                if (param is JobCardItem item)
                    await RemoveItem(item);
            });
            RefreshCommand = new RelayCommand(async _ => await LoadAllJobCards());
            LoadDropdownsCommand = new RelayCommand(async _ => await LoadDropdowns());

            Task.Run(async () =>
            {
                await LoadAllJobCards();
                await LoadDropdowns();
            });
        }

        private async Task LoadAllJobCards()
        {
            try
            {
                var cards = await _jobService.GetAllJobCardsAsync();
                JobCards.Clear();
                foreach (var c in cards) JobCards.Add(c);
            }
            catch (Exception ex)
            {
                FormMessage = "Error loading job cards: " + ex.Message;
            }
        }

        private async Task LoadDropdowns()
        {
            FormMessage = "";
            try
            {
                // Vehicles
                var vehicles = await _jobService.GetVehiclesAsync();
                VehicleList.Clear();
                foreach (var v in vehicles)
                    VehicleList.Add(v);

                // Mechanics
                var mechanics = await _jobService.GetMechanicsAsync();
                MechanicList.Clear();
                foreach (var m in mechanics)
                    MechanicList.Add(m);

                // Inventory items (only with stock)
                var invItems = await _jobService.GetInventoryItemsAsync();
                InventoryList.Clear();
                foreach (var i in invItems)
                    InventoryList.Add(i);

                FormMessage = $"Loaded {VehicleList.Count} vehicles, {MechanicList.Count} mechanics, {InventoryList.Count} parts.";
            }
            catch (Exception ex)
            {
                FormMessage = "Error loading dropdowns: " + ex.Message;
            }
        }

        private async Task LoadJobCardDetail()
        {
            if (SelectedJobCard == null)
            {
                ClearEdit();
                return;
            }
            try
            {
                var fullCard = await _jobService.GetJobCardByIdAsync(SelectedJobCard.JobCardId);
                if (fullCard == null) return;

                EditDescription = fullCard.Description;
                SelectedVehicle = VehicleList.FirstOrDefault(v => v.VehicleId == fullCard.VehicleId);
                SelectedMechanic = MechanicList.FirstOrDefault(u => u.UserId == fullCard.AssignedUserId);
                SelectedStatus = fullCard.Status;

                CurrentItems.Clear();
                foreach (var item in fullCard.Items)
                    CurrentItems.Add(item);
            }
            catch (Exception ex)
            {
                FormMessage = "Error loading job details: " + ex.Message;
            }
        }

        private void ClearEdit()
        {
            _isUpdating = true;
            SelectedJobCard = null;
            EditDescription = string.Empty;
            SelectedVehicle = null;
            SelectedMechanic = null;
            SelectedStatus = "Open";
            CurrentItems.Clear();
            SelectedInventoryItem = null;
            AddQuantity = 1;
            FormMessage = string.Empty;
            _isUpdating = false;
        }



        private async Task SaveJobCard()
        {
            FormMessage = string.Empty;
            if (SelectedVehicle == null || SelectedMechanic == null)
            { FormMessage = "Vehicle and mechanic are required."; return; }

            try
            {
                if (SelectedJobCard == null)
                {
                    var newCard = new JobCard
                    {
                        VehicleId = SelectedVehicle.VehicleId,
                        AssignedUserId = SelectedMechanic.UserId,
                        Status = SelectedStatus,
                        Description = EditDescription
                    };
                    var newId = await _jobService.CreateJobCardAsync(newCard);
                    await LoadAllJobCards();
                    SelectedJobCard = JobCards.FirstOrDefault(c => c.JobCardId == newId);
                    ClearEdit();
                    FormMessage = "Job card created.";
                }
                else
                {
                    var updated = new JobCard
                    {
                        JobCardId = SelectedJobCard.JobCardId,
                        VehicleId = SelectedVehicle.VehicleId,
                        AssignedUserId = SelectedMechanic.UserId,
                        Status = SelectedStatus,
                        Description = EditDescription,
                        DateCompleted = (SelectedStatus == "Completed" || SelectedStatus == "Closed") ? DateTime.Now : SelectedJobCard.DateCompleted
                    };
                    await _jobService.UpdateJobCardAsync(updated);
                    FormMessage = "Job card updated.";
                    await LoadAllJobCards();
                }
            }
            catch (Exception ex) { FormMessage = "Error: " + ex.Message; }
        }

        private async Task DeleteJobCard()
        {
            if (SelectedJobCard == null) return;
            try
            {
                await _jobService.DeleteJobCardAsync(SelectedJobCard.JobCardId);
                await LoadAllJobCards();
                ClearEdit();
            }
            catch (Exception ex) { FormMessage = "Error deleting: " + ex.Message; }
        }

        private async Task AddItem()
        {
            if (SelectedJobCard == null || SelectedInventoryItem == null || AddQuantity < 1)
            { FormMessage = "Select a part and quantity."; return; }
            try
            {
                var item = new JobCardItem
                {
                    InventoryItemId = SelectedInventoryItem.InventoryItemId,
                    Quantity = AddQuantity,
                    UnitPrice = SelectedInventoryItem.UnitPrice
                };
                await _jobService.AddItemToJobCardAsync(SelectedJobCard.JobCardId, item);
                await LoadJobCardDetail();
                ClearEdit();
                FormMessage = "Item added.";
            }
            catch (Exception ex) { FormMessage = "Error: " + ex.Message; }
        }

        private async Task RemoveItem(JobCardItem item)
        {
            try
            {
                await _jobService.RemoveItemFromJobCardAsync(item.JobCardItemId);
                await LoadJobCardDetail();
                FormMessage = "Item removed.";
            }
            catch (Exception ex) { FormMessage = "Error: " + ex.Message; }
        }
    }
}
