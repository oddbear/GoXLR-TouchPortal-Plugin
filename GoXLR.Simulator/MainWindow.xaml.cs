using System;
using System.Windows;
using GoXLR.Simulator.ViewModels;

namespace GoXLR.Simulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ipAddress = IpAddress.Text;
                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    _viewModel.Connect(ipAddress);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.Disconnect();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
    }
}
