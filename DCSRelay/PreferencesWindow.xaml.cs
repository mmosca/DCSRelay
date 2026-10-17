using System;
using System.Windows;
using DCSRelay.Services;
using DCSRelay.ViewModels;

namespace DCSRelay
{
    public partial class PreferencesWindow : Window
    {
        private readonly PreferencesViewModel _viewModel;

        public PreferencesWindow(PreferencesViewModel viewModel)
        {
            InitializeComponent();
            Icon = SvgIconLoader.LoadFromAssets("AppIcon.Simplified.svg") ?? Icon;

            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.CloseRequested += OnCloseRequested;
            _viewModel.ValidationErrorRequested += OnValidationErrorRequested;
            Closed += OnWindowClosed;
        }

        private void OnCloseRequested(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void OnValidationErrorRequested(string message)
        {
            System.Windows.MessageBox.Show(this, message, "Invalid address", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
            _viewModel.ValidationErrorRequested -= OnValidationErrorRequested;
            Closed -= OnWindowClosed;
        }
    }
}
