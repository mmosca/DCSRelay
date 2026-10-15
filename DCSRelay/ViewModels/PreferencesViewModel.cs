using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DCSRelay.Services;

namespace DCSRelay.ViewModels
{
    public class PreferencesViewModel : INotifyPropertyChanged
    {
        private readonly IAppConfigurationService _configService;

        private bool _startMinimized;
        private string _dcsBiosCommandAddress = string.Empty;

        private bool _originalStartMinimized;
        private string _originalDcsBiosCommandAddress = string.Empty;

        public PreferencesViewModel(IAppConfigurationService configService)
        {
            _configService = configService;
            // load initial values from service
            _startMinimized = _configService.StartMinimized;
            _dcsBiosCommandAddress = _configService.DcsBiosCommandAddress ?? string.Empty;

            _originalStartMinimized = _startMinimized;
            _originalDcsBiosCommandAddress = _dcsBiosCommandAddress;
        }

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

        public bool IsDirty => _startMinimized != _originalStartMinimized || _dcsBiosCommandAddress != _originalDcsBiosCommandAddress;

        public async Task SaveAsync()
        {
            _configService.StartMinimized = _startMinimized;
            _configService.DcsBiosCommandAddress = _dcsBiosCommandAddress;
            await _configService.SaveAsync();

            // update originals
            _originalStartMinimized = _startMinimized;
            _originalDcsBiosCommandAddress = _dcsBiosCommandAddress;
            OnPropertyChanged(nameof(IsDirty));
        }

        public void Cancel()
        {
            // revert to originals
            StartMinimized = _originalStartMinimized;
            DcsBiosCommandAddress = _originalDcsBiosCommandAddress;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
