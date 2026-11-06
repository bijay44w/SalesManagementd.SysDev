using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    public partial class Inventory : UserControl
    {
        private readonly StockService _stockService = new();
        private readonly ProductService _productService = new();
        private List<StockDisplayModel> _cachedStockList = new();

        public Inventory()
        {
            InitializeComponent();
            Loaded += Inventory_Loaded;
        }

        private void Inventory_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMainMasters();
            ExecuteMainSearch();
            UpdateMainActionButtonsState();
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message, "在庫管理",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenInventoryOperation(InventoryOperationMode mode)
        {
            var window = new InventoryOperationWindow(mode)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }

        private void card_InventoryRegister_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            OpenInventoryOperation(InventoryOperationMode.Register);
        }

        private void card_InventoryUpdate_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            OpenInventoryOperation(InventoryOperationMode.Update);
        }

        private void card_InventoryList_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            OpenInventoryOperation(InventoryOperationMode.List);
        }

        private void card_InventorySearch_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            OpenInventoryOperation(InventoryOperationMode.Search);
        }

        private void ShowNewProdError(string msg)
        {
            OpenInventoryOperation(InventoryOperationMode.Hide);
        }
    }
}
