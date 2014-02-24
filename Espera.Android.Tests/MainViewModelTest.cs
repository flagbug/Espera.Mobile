using Espera.Mobile.Core;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.ViewModels;
using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using ReactiveUI.Testing;
using System;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace Espera.Android.Tests
{
    public class MainViewModelTest
    {
        [Fact]
        public void ConnectCommandSmokeTest()
        {
            var isConnected = new BehaviorSubject<bool>(false);
            var messenger = CreateDefaultNetworkMessenger();
            messenger.SetupGet(x => x.IsConnected).Returns(isConnected);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel();

            Assert.True(vm.ConnectCommand.CanExecute(null));

            vm.ConnectCommand.Execute(null);
            isConnected.OnNext(true);

            Assert.False(vm.ConnectCommand.CanExecute(null));

            messenger.Verify(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<Guid>(), null), Times.Once);
        }

        [Fact]
        public void ConnectCommandTimeoutTriggersConnectionFailed()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<Guid>(), null))
                .Returns(Task.Delay(1000).ContinueWith(x => (ConnectionInfo)null));
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(false));

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel();

            var coll = vm.ConnectionFailed.CreateCollection();

            (new TestScheduler()).With(scheduler =>
            {
                vm.ConnectCommand.Execute(null);
                scheduler.AdvanceByMs(10000);
            });

            Assert.Equal(1, coll.Count);
        }

        [Fact]
        public void ConnectCommandWithWrongPasswordTriggersConnectionFailed()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(false));
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<Guid>(), null))
                .Returns(new ConnectionInfo(AccessPermission.Admin, new Version(99, 99),
                    new ResponseInfo(401, "Wrong password")).ToTaskResult);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            UserSettings.Instance.EnableAdministratorMode = true;
            UserSettings.Instance.AdministratorPassword = "Bla";

            var vm = new MainViewModel();

            var coll = vm.ConnectionFailed.CreateCollection();

            vm.ConnectCommand.Execute(null);

            Assert.Equal(1, coll.Count);
        }

        [Fact]
        public void DisconnectCommandSmokeTest()
        {
            var isConnected = new BehaviorSubject<bool>(true);
            var messenger = CreateDefaultNetworkMessenger();
            messenger.SetupGet(x => x.IsConnected).Returns(isConnected);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel();

            Assert.True(vm.DisconnectCommand.CanExecute(true));

            vm.DisconnectCommand.Execute(null);
            isConnected.OnNext(false);

            Assert.False(vm.DisconnectCommand.CanExecute(null));

            messenger.Verify(x => x.Disconnect(), Times.Once);
        }

        [Fact]
        public void IsConnectedIsFalseWhileConnectCommandExecutesWithPassword()
        {
            var isConnected = new BehaviorSubject<bool>(false);
            var messenger = CreateDefaultNetworkMessenger();
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>()))
                .Callback(() => isConnected.OnNext(true))
                .Returns(new ConnectionInfo(AccessPermission.Admin, new Version(99, 99),
                    new ResponseInfo(200, "Ok")).ToTaskResult);
            messenger.SetupGet(x => x.IsConnected).Returns(isConnected);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            UserSettings.Instance.EnableAdministratorMode = true;
            UserSettings.Instance.AdministratorPassword = "Bla";

            var vm = new MainViewModel();

            isConnected.FirstAsync(x => x).Subscribe(_ => Assert.False(vm.IsConnected));

            vm.ConnectCommand.Execute(null);
        }

        [Fact]
        public void MinimumServerVersionMustBeMet()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(true));
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(new ConnectionInfo(AccessPermission.Admin, new Version(0, 1, 0),
                    new ResponseInfo(200, "Ok")).ToTaskResult);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel();

            var thrown = vm.ConnectionFailed.CreateCollection();

            vm.ConnectCommand.Execute(null);

            Assert.Equal(1, thrown.Count);
        }

        private static Mock<INetworkMessenger> CreateDefaultNetworkMessenger()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(true));

            return messenger;
        }
    }
}