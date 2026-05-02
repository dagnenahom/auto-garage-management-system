using SmartGarageSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SmartGarageSystem.Views
{   

    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
            // No need to set DataContext; it's done via the DataTemplate
            
            EditPasswordBox.PasswordChanged += (sender, e) =>
            {
                if (DataContext is UserManagementViewModel vm)
                    vm.EditPassword = EditPasswordBox.Password;
            };

        }
    }
}

