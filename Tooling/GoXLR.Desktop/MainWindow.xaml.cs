using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GoXLR.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace GoXLR.Desktop
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

        private void ButtonGetProfiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.GetProfiles();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                MessageBox.Show(exception.Message);
            }
        }

        private void ButtonSetProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.SetProfile();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                MessageBox.Show(exception.Message);
            }
        }

        private void ButtonSetRouting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.SetRouting();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                MessageBox.Show(exception.Message);
            }
        }

        private void ComboBoxClients_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                _viewModel.GetProfiles();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                MessageBox.Show(exception.Message);
            }
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
