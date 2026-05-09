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
    public class InventoryViewModel : BaseViewModel
    {
        private readonly IInventoryService _inventoryService;
        private bool _isUpdating;

        public ObservableCollection<InventoryItem> Items { get; } = new ObservableCollection<InventoryItem>();

        private InventoryItem _selectedItem;
        public InventoryItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnPropertyChanged();
                if (!_isUpdating) LoadEditData();
            }
        }

        private string _editPartName, _editPartNumber, _editDescription;
        private int _editQuantity, _editReorderLevel;
        private decimal _editUnitPrice;
        private string _formMessage;

        public string EditPartName { get => _editPartName; set { _editPartName = value; OnPropertyChanged(); } }
        public string EditPartNumber { get => _editPartNumber; set { _editPartNumber = value; OnPropertyChanged(); } }
        public string EditDescription { get => _editDescription; set { _editDescription = value; OnPropertyChanged(); } }
        public int EditQuantity { get => _editQuantity; set { _editQuantity = value; OnPropertyChanged(); } }
        public decimal EditUnitPrice { get => _editUnitPrice; set { _editUnitPrice = value; OnPropertyChanged(); } }
        public int EditReorderLevel { get => _editReorderLevel; set { _editReorderLevel = value; OnPropertyChanged(); } }
        public string FormMessage { get => _formMessage; set { _formMessage = value; OnPropertyChanged(); } }

        public bool IsEditing => SelectedItem != null;

        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand NewItemCommand { get; }
        public ICommand SaveItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand RefreshCommand { get; }

        public InventoryViewModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;

            SearchCommand = new RelayCommand(async _ => await SearchItems());
            ClearSearchCommand = new RelayCommand(async _ => { _searchText = string.Empty; await LoadAllItems(); });
            NewItemCommand = new RelayCommand(_ => ClearEdit());
            SaveItemCommand = new RelayCommand(async _ => await SaveItem());
            DeleteItemCommand = new RelayCommand(async _ => await DeleteItem(), _ => SelectedItem != null);
            CancelEditCommand = new RelayCommand(_ => ClearEdit());
            RefreshCommand = new RelayCommand(async _ => await LoadAllItems());

            Task.Run(async () => await LoadAllItems());
        }

        private string _searchText;
        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }

        private async Task LoadAllItems()
        {
            var items = await _inventoryService.GetAllItemsAsync();
            Items.Clear();
            foreach (var i in items) Items.Add(i);
        }

        private async Task SearchItems()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) { await LoadAllItems(); return; }
            var results = await _inventoryService.SearchItemsAsync(SearchText);
            Items.Clear();
            foreach (var r in results) Items.Add(r);
        }

        public void ClearEdit()
        {
            _isUpdating = true;
            EditPartName = EditPartNumber = EditDescription = string.Empty;
            EditQuantity = 0; EditUnitPrice = 0; EditReorderLevel = 5;
            FormMessage = string.Empty;
            SelectedItem = null;
            _isUpdating = false;
            OnPropertyChanged(nameof(IsEditing));
        }

        public void LoadEditData()
        {
            if (SelectedItem == null) { ClearEdit(); return; }
            EditPartName = SelectedItem.PartName;
            EditPartNumber = SelectedItem.PartNumber;
            EditDescription = SelectedItem.Description;
            EditQuantity = SelectedItem.QuantityInStock;
            EditUnitPrice = SelectedItem.UnitPrice;
            EditReorderLevel = SelectedItem.ReorderLevel;
            OnPropertyChanged(nameof(IsEditing));
        }

        private async Task SaveItem()
        {
            FormMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(EditPartName)) { FormMessage = "Part name is required."; return; }
            try
            {
                if (SelectedItem == null)
                {
                    var newItem = new InventoryItem
                    {
                        PartName = EditPartName,
                        PartNumber = EditPartNumber,
                        Description = EditDescription,
                        QuantityInStock = EditQuantity,
                        UnitPrice = EditUnitPrice,
                        ReorderLevel = EditReorderLevel
                    };
                    await _inventoryService.CreateItemAsync(newItem);
                    FormMessage = "Item created.";
                }
                else
                {
                    var updated = new InventoryItem
                    {
                        InventoryItemId = SelectedItem.InventoryItemId,
                        PartName = EditPartName,
                        PartNumber = EditPartNumber,
                        Description = EditDescription,
                        QuantityInStock = EditQuantity,
                        UnitPrice = EditUnitPrice,
                        ReorderLevel = EditReorderLevel
                    };
                    await _inventoryService.UpdateItemAsync(updated);
                    FormMessage = "Item updated.";
                }
                await LoadAllItems();
                ClearEdit();
            }
            catch (Exception ex) { FormMessage = "Error: " + ex.Message; }
        }

        private async Task DeleteItem()
        {
            if (SelectedItem == null) return;
            await _inventoryService.DeleteItemAsync(SelectedItem.InventoryItemId);
            await LoadAllItems();
            ClearEdit();
        }
    }
}