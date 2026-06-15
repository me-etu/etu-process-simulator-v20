using System.Windows;
using PlcSimAdvancedFramework.ViewModels;

namespace PlcSimAdvancedFramework.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
