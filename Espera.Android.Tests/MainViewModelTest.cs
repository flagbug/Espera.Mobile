using Espera.Android.Network;
using Espera.Android.ViewModels;
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
            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>())).Returns(Task.Delay(0)).Verifiable();
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(false));

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel(Observable.Return(12345));

            vm.ConnectCommand.Execute(null);

            messenger.Verify(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void ConnectCommandTimeoutTriggersConnectionFailed()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>())).Returns(Task.Delay(1000));
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(false));

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel(Observable.Return(12345));

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
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>())).Returns(Task.Delay(0)).Verifiable();
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(false));
            messenger.Setup(x => x.Authorize(It.IsAny<string>())).Returns(new ResponseInfo(401, "Wrong password").ToTaskResult());

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel(Observable.Return(12345))
            {
                EnableAdministratorMode = true,
                Password = "Bla"
            };

            var coll = vm.ConnectionFailed.CreateCollection();

            vm.ConnectCommand.Execute(null);

            Assert.Equal(1, coll.Count);
        }

        [Fact]
        public void IsConnectedIsFalseWhileConnectCommandExecutesWithPassword()
        {
            var isConnected = new BehaviorSubject<bool>(false);
            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()))
                .Callback(() => isConnected.OnNext(true))
                .Returns(Task.Delay(0)).Verifiable();
            messenger.SetupGet(x => x.IsConnected).Returns(isConnected);
            messenger.Setup(x => x.Authorize(It.IsAny<string>())).Returns(new ResponseInfo(200, "Ok").ToTaskResult());

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var vm = new MainViewModel(Observable.Return(12345))
            {
                EnableAdministratorMode = true,
                Password = "Bla"
            };

            isConnected.FirstAsync(x => x).Subscribe(_ => Assert.False(vm.IsConnected));

            vm.ConnectCommand.Execute(null);
        }

        [Fact]
        public void PortChangeCallsDisconnect()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>())).Returns(Task.Delay(0)).Verifiable();
            messenger.Setup(x => x.Disconnect()).Verifiable();

            var isConnected = new BehaviorSubject<bool>(false);
            messenger.SetupGet(x => x.IsConnected).Returns(isConnected);

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var port = new BehaviorSubject<int>(12345);
            var vm = new MainViewModel(port);

            isConnected.OnNext(true);
            port.OnNext(123456);

            messenger.Verify(x => x.Disconnect(), Times.Once);
        }

        [Fact]
        public void PortChangeWhileDisconnectedDoesntCallDisconnect()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>())).Returns(Task.Delay(0)).Verifiable();
            messenger.Setup(x => x.Disconnect()).Verifiable();
            messenger.SetupGet(x => x.IsConnected).Returns(Observable.Return(false));

            NetworkMessenger.Override(messenger.Object, IPAddress.Parse("192.168.1.1"));

            var port = new BehaviorSubject<int>(12345);
            var vm = new MainViewModel(port);

            port.OnNext(123456);

            messenger.Verify(x => x.Disconnect(), Times.Never);
        }
    }
}