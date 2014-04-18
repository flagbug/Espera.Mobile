using System;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using ReactiveUI.Testing;
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
            vm.Activator.Activate();

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
                .Returns(Task.Delay(1000).ContinueWith(x => Tuple.Create(ResponseStatus.Fatal, (ConnectionInfo)null)));
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(false));

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel();
            vm.Activator.Activate();

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
                .Returns(Tuple.Create(ResponseStatus.WrongPassword,
                    new ConnectionInfo { AccessPermission = NetworkAccessPermission.Admin, ServerVersion = new Version(99, 99) }).ToTaskResult);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            UserSettings.Instance.EnableAdministratorMode = true;
            UserSettings.Instance.AdministratorPassword = "Bla";

            var vm = new MainViewModel();
            vm.Activator.Activate();

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
            vm.Activator.Activate();

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
                .Returns(Tuple.Create(ResponseStatus.Success,
                    new ConnectionInfo { AccessPermission = NetworkAccessPermission.Admin, ServerVersion = new Version(99, 99) }).ToTaskResult);
            messenger.SetupGet(x => x.IsConnected).Returns(isConnected);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            UserSettings.Instance.EnableAdministratorMode = true;
            UserSettings.Instance.AdministratorPassword = "Bla";

            var vm = new MainViewModel();
            vm.Activator.Activate();

            isConnected.FirstAsync(x => x).Subscribe(_ => Assert.False(vm.IsConnected));

            vm.ConnectCommand.Execute(null);
        }

        [Fact]
        public void MinimumServerVersionMustBeMet()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(true));
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(Tuple.Create(ResponseStatus.Success,
                    new ConnectionInfo { AccessPermission = NetworkAccessPermission.Admin, ServerVersion = new Version(0, 1, 0) }).ToTaskResult);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel();
            vm.Activator.Activate();

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