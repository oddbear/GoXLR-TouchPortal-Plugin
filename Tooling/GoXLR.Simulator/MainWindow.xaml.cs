using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GoXLR.Simulator.ViewModels;
using Microsoft.Extensions.Logging;

namespace GoXLR.Simulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly MainViewModel _viewModel;

        public MainWindow(ILogger<MainWindow> logger, MainViewModel viewModel)
        {
            InitializeComponent();
            _logger = logger;
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ipAddress = IpAddress.Text;
                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    await _viewModel.ConnectAsync(ipAddress);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                MessageBox.Show(exception.Message);
            }

            e.Handled = true;
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.Disconnect();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                MessageBox.Show(exception.Message);
            }

            e.Handled = true;
        }

        private void IpAddress_OnKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    ButtonConnect_Click(sender, e);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                MessageBox.Show(exception.Message);
            }

            e.Handled = true;
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBoxBase textBoxBase)
            {
                textBoxBase.ScrollToEnd();
            }
        }
    }
}
