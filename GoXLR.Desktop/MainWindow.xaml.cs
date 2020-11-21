using System;
using System.Windows;
using GoXLR.Desktop.ViewModels;

namespace GoXLR.Desktop
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

        private async void ButtonGetProfiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.GetProfiles();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private async void ButtonSetProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.SetProfile();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private async void ButtonSetRouting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.SetRouting();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void ComboBoxClients_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                _viewModel.UpdateProfiles();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
    }
}
