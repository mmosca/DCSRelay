using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DCSRelay.Services;

namespace DCSRelay.ViewModels
{
    public class PreferencesViewModel : INotifyPropertyChanged
    {
        private const string DefaultAddress = "127.0.0.1:7778";

        private readonly IAppConfigurationService _configService;
        private readonly IWindowsStartupRegistrationService _startupRegistrationService;

        private bool _startMinimized;
        private bool _startWithWindows;
        private string _dcsBiosCommandAddress = string.Empty;
        private bool _isSaving;

        private bool _originalStartMinimized;
        private bool _originalStartWithWindows;
        private string _originalDcsBiosCommandAddress = string.Empty;

        public PreferencesViewModel(IAppConfigurationService configService, IWindowsStartupRegistrationService startupRegistrationService)
        {
            _configService = configService;
            _startupRegistrationService = startupRegistrationService;
            _startMinimized = _configService.StartMinimized;
            _startWithWindows = _configService.StartWithWindows;
            _dcsBiosCommandAddress = _configService.DcsBiosCommandAddress ?? string.Empty;

            _originalStartMinimized = _startMinimized;
            _originalStartWithWindows = _startWithWindows;
            _originalDcsBiosCommandAddress = _dcsBiosCommandAddress;

            SaveCommand = new RelayCommand(async _ => await ExecuteSaveCommandAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => ExecuteCancelCommand());
        }

        public RelayCommand SaveCommand { get; }

        public RelayCommand CancelCommand { get; }

        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                if (_startMinimized != value)
                {
                    _startMinimized = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDirty));
                }
            }
        }

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (_startWithWindows != value)
                {
                    _startWithWindows = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDirty));
                }
            }
        }

        public string DcsBiosCommandAddress
        {
            get => _dcsBiosCommandAddress;
            set
            {
                if (_dcsBiosCommandAddress != value)
                {
                    _dcsBiosCommandAddress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDirty));
                }
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                if (_isSaving != value)
                {
                    _isSaving = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDirty =>
            _startMinimized != _originalStartMinimized ||
            _startWithWindows != _originalStartWithWindows ||
            _dcsBiosCommandAddress != _originalDcsBiosCommandAddress;

        public event Action<bool>? CloseRequested;

        public event Action<string>? ValidationErrorRequested;

        public async Task SaveAsync()
        {
            var value = NormalizeAddress(_dcsBiosCommandAddress);

            _configService.StartMinimized = _startMinimized;
            _configService.StartWithWindows = _startWithWindows;
            _configService.DcsBiosCommandAddress = value;
            await _configService.SaveAsync();
            _startupRegistrationService.SetEnabled(_startWithWindows);

            _dcsBiosCommandAddress = value;
            OnPropertyChanged(nameof(DcsBiosCommandAddress));

            _originalStartMinimized = _startMinimized;
            _originalStartWithWindows = _startWithWindows;
            _originalDcsBiosCommandAddress = _dcsBiosCommandAddress;
            OnPropertyChanged(nameof(IsDirty));
        }

        public void Cancel()
        {
            StartMinimized = _originalStartMinimized;
            StartWithWindows = _originalStartWithWindows;
            DcsBiosCommandAddress = _originalDcsBiosCommandAddress;
        }

        private async Task ExecuteSaveCommandAsync()
        {
            if (IsSaving)
            {
                return;
            }

            var value = NormalizeAddress(_dcsBiosCommandAddress);
            if (!IsValidAddress(value))
            {
                ValidationErrorRequested?.Invoke("Please enter a valid IP or IP:port (e.g. 127.0.0.1:7778)");
                return;
            }

            IsSaving = true;
            SaveCommand.RaiseCanExecuteChanged();

            try
            {
                await SaveAsync();
                CloseRequested?.Invoke(true);
            }
            finally
            {
                IsSaving = false;
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        private void ExecuteCancelCommand()
        {
            Cancel();
            CloseRequested?.Invoke(false);
        }

        private static string NormalizeAddress(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? DefaultAddress : normalized;
        }

        private static bool IsValidAddress(string value)
        {
            var parts = value.Split(':');
            if (parts.Length == 1)
            {
                return IPAddress.TryParse(parts[0], out _);
            }

            if (parts.Length == 2 && int.TryParse(parts[1], out var port))
            {
                return IPAddress.TryParse(parts[0], out _) && port > 0 && port <= 65535;
            }

            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
