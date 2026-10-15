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
                // nothing to do for fake
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
    }
}
