using System.Threading.Tasks;
using DCSRelay.Services;
using DCSRelay.ViewModels;
using Xunit;

namespace DCSRelay.Tests.ViewModels
{
    public class PreferencesViewModelTests
    {
        private class FakeConfigService : IAppConfigurationService
        {
            public bool StartMinimized { get; set; }
            public string DcsBiosCommandAddress { get; set; } = "127.0.0.1:7778";
            public bool SaveCalled { get; private set; }

            public Task LoadAsync()
            {
                return Task.CompletedTask;
            }

            public Task SaveAsync()
            {
                SaveCalled = true;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public void Constructor_LoadsValuesFromService()
        {
            var svc = new FakeConfigService { StartMinimized = true, DcsBiosCommandAddress = "10.0.0.5:1234" };
            var vm = new PreferencesViewModel(svc);

            Assert.True(vm.StartMinimized);
            Assert.Equal("10.0.0.5:1234", vm.DcsBiosCommandAddress);
            Assert.False(vm.IsDirty);
        }

        [Fact]
        public async Task SaveAsync_PersistsToService_And_ClearsDirty()
        {
            var svc = new FakeConfigService { StartMinimized = false, DcsBiosCommandAddress = "127.0.0.1:7778" };
            var vm = new PreferencesViewModel(svc);

            vm.StartMinimized = true;
            vm.DcsBiosCommandAddress = "192.168.1.10:9000";

            Assert.True(vm.IsDirty);

            await vm.SaveAsync();

            Assert.True(svc.SaveCalled);
            Assert.True(svc.StartMinimized);
            Assert.Equal("192.168.1.10:9000", svc.DcsBiosCommandAddress);
            Assert.False(vm.IsDirty);
        }

        [Fact]
        public async Task SaveAsync_UsesDefaultAddress_WhenBlank()
        {
            var svc = new FakeConfigService { StartMinimized = false, DcsBiosCommandAddress = "127.0.0.1:7778" };
            var vm = new PreferencesViewModel(svc)
            {
                DcsBiosCommandAddress = "   "
            };

            await vm.SaveAsync();

            Assert.Equal("127.0.0.1:7778", vm.DcsBiosCommandAddress);
            Assert.Equal("127.0.0.1:7778", svc.DcsBiosCommandAddress);
        }

        [Fact]
        public async Task SaveCommand_WithValidAddress_SavesAndRequestsClose()
        {
            var svc = new FakeConfigService();
            var vm = new PreferencesViewModel(svc)
            {
                DcsBiosCommandAddress = "10.1.2.3:5555"
            };

            bool? closeResult = null;
            vm.CloseRequested += result => closeResult = result;

            vm.SaveCommand.Execute(null);
            await WaitForConditionAsync(() => svc.SaveCalled);

            Assert.True(svc.SaveCalled);
            Assert.True(closeResult);
            Assert.Equal("10.1.2.3:5555", svc.DcsBiosCommandAddress);
        }

        [Fact]
        public async Task SaveCommand_WithInvalidAddress_RaisesValidationError_AndDoesNotSave()
        {
            var svc = new FakeConfigService();
            var vm = new PreferencesViewModel(svc)
            {
                DcsBiosCommandAddress = "not-an-address"
            };

            string? errorMessage = null;
            vm.ValidationErrorRequested += message => errorMessage = message;

            vm.SaveCommand.Execute(null);
            await Task.Delay(50);

            Assert.False(svc.SaveCalled);
            Assert.NotNull(errorMessage);
        }

        [Fact]
        public void CancelCommand_RevertsValues_AndRequestsCloseFalse()
        {
            var svc = new FakeConfigService { StartMinimized = false, DcsBiosCommandAddress = "127.0.0.1:7778" };
            var vm = new PreferencesViewModel(svc)
            {
                StartMinimized = true,
                DcsBiosCommandAddress = "10.2.3.4:5555"
            };

            bool? closeResult = null;
            vm.CloseRequested += result => closeResult = result;

            vm.CancelCommand.Execute(null);

            Assert.False(vm.StartMinimized);
            Assert.Equal("127.0.0.1:7778", vm.DcsBiosCommandAddress);
            Assert.False(vm.IsDirty);
            Assert.False(closeResult);
        }

        [Fact]
        public void Cancel_RevertsChanges()
        {
            var svc = new FakeConfigService { StartMinimized = false, DcsBiosCommandAddress = "127.0.0.1:7778" };
            var vm = new PreferencesViewModel(svc);

            vm.StartMinimized = true;
            vm.DcsBiosCommandAddress = "10.2.3.4:5555";
            Assert.True(vm.IsDirty);

            vm.Cancel();

            Assert.False(vm.StartMinimized);
            Assert.Equal("127.0.0.1:7778", vm.DcsBiosCommandAddress);
            Assert.False(vm.IsDirty);
        }

        private static async Task WaitForConditionAsync(System.Func<bool> condition, int timeoutMs = 1000)
        {
            var elapsed = 0;
            while (!condition() && elapsed < timeoutMs)
            {
                await Task.Delay(20);
                elapsed += 20;
            }
        }
    }
}
