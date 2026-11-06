using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SalesManagement_SysDev
{
    public partial class Employee : UserControl
    {
        private readonly EmployeeService _employeeService = new();
        private List<EmployeeDisplayModel> _cachedList = new();
        private List<MSalesOffice> _offices = new();
        private List<MPosition> _positions = new();
        private int _selectedEmployeeIdForHide = 0;

        public Employee()
        {
            InitializeComponent();
            AttachEmployeeCardEvents();
        }

        /// <summary>
        /// 既存の Employee.xaml を編集せず、配置済みの5枚のカードにクリック処理を設定する。
        /// </summary>
        private void AttachEmployeeCardEvents()
        {
            var operationGrid = this.Content is Grid root
                ? root.Children.OfType<Grid>()
                    .FirstOrDefault(grid => Grid.GetRow(grid) == 1)
                : null;

            if (operationGrid == null)
                return;

            foreach (var card in operationGrid.Children.OfType<Border>())
            {
                int row = Grid.GetRow(card);
                int column = Grid.GetColumn(card);

                card.Cursor = Cursors.Hand;

                if (row == 0 && column == 0)
                    card.MouseLeftButtonUp += (_, _) => OpenEmployeeOperation(EmployeeOperationMode.Register);
                else if (row == 0 && column == 1)
                    card.MouseLeftButtonUp += (_, _) => OpenEmployeeOperation(EmployeeOperationMode.Update);
                else if (row == 0 && column == 2)
                    card.MouseLeftButtonUp += (_, _) => OpenEmployeeOperation(EmployeeOperationMode.List);
                else if (row == 1 && column == 0)
                    card.MouseLeftButtonUp += (_, _) => OpenEmployeeOperation(EmployeeOperationMode.Search);
                else if (row == 1 && column == 1)
                    card.MouseLeftButtonUp += (_, _) => OpenEmployeeOperation(EmployeeOperationMode.Hide);
            }
        }

        private void OpenEmployeeOperation(EmployeeOperationMode mode)
        {
            var window = new EmployeeOperationWindow(mode)
            {
                Owner = Window.GetWindow(this)
            };

            window.ShowDialog();
        }
    }
}
