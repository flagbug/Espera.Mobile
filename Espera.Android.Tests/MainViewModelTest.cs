using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Microsoft.Reactive.Testing;
using NSubstitute;
using ReactiveUI;
using ReactiveUI.Testing;
using Xunit;

namespace Espera.Android.Tests
{
    public class MainViewModelTest
    {
        public class TheConnectCommand
        {
            [Fact]
            public void ChecksMinimumServerVersion()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(Observable.Return(true));
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.Success,
                        new ConnectionInfo
                        {
                            AccessPermission = NetworkAccessPermission.Admin,
                            ServerVersion = new Version(0, 1, 0)
                        }).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Task.FromResult("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel();
                vm.Activator.Activate();

                var thrown = vm.ConnectionFailed.CreateCollection();

                vm.ConnectCommand.Execute(null);

                Assert.Equal(1, thrown.Count);
            }

            [Fact]
            public void SmokeTest()
            {
                var isConnected = new BehaviorSubject<bool>(false);
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(isConnected);
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), null)
                    .Returns(Tuple.Create(ResponseStatus.Success,
                        new ConnectionInfo
                        {
                            AccessPermission = NetworkAccessPermission.Admin,
                            ServerVersion = new Version("99.99.99")
                        }).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Task.FromResult("192.168.1.1"));
                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel();
                vm.Activator.Activate();

                Assert.True(vm.ConnectCommand.CanExecute(null));

                vm.ConnectCommand.Execute(null);
                isConnected.OnNext(true);

                Assert.False(vm.ConnectCommand.CanExecute(null));

                messenger.Received(1).ConnectAsync("192.168.1.1", UserSettings.Instance.Port, Arg.Any<Guid>(), null);
            }

            [Fact]
            public void TimeoutTriggersConnectionFailed()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), null)
                    .Returns(Observable.Never<Tuple<ResponseStatus, ConnectionInfo>>().ToTask());
                messenger.IsConnected.Returns(Observable.Return(false));
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Task.FromResult("192.168.1.1"));

                NetworkMessenger.Override(messenger);

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
            public void WrongPasswordTriggersConnectionFailed()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(Observable.Return(false));
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), null)
                    .Returns(Tuple.Create(ResponseStatus.WrongPassword,
                        new ConnectionInfo { AccessPermission = NetworkAccessPermission.Admin, ServerVersion = new Version(99, 99) }).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Task.FromResult("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                UserSettings.Instance.EnableAdministratorMode = true;
                UserSettings.Instance.AdministratorPassword = "Bla";

                var vm = new MainViewModel();
                vm.Activator.Activate();

                var coll = vm.ConnectionFailed.CreateCollection();

                vm.ConnectCommand.Execute(null);

                Assert.Equal(1, coll.Count);
            }
        }

        public class TheDisconnectCommand
        {
            [Fact]
            public void SmokeTest()
            {
                var isConnected = new BehaviorSubject<bool>(true);
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(isConnected);

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel();
                vm.Activator.Activate();

                Assert.True(vm.DisconnectCommand.CanExecute(true));

                vm.DisconnectCommand.Execute(null);
                isConnected.OnNext(false);

                Assert.False(vm.DisconnectCommand.CanExecute(null));

                messenger.Received(1).Disconnect();
            }
        }

        public class TheIsConnectedProperty
        {
            [Fact]
            public void IsFalseWhileConnectCommandExecutesWithPassword()
            {
                var isConnected = new BehaviorSubject<bool>(false);
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(x =>
                    {
                        isConnected.OnNext(true);
                        return Tuple.Create(ResponseStatus.Success,
                            new ConnectionInfo
                            {
                                AccessPermission = NetworkAccessPermission.Admin,
                                ServerVersion = new Version(99, 99)
                            })
                        .ToTaskResult();
                    });
                messenger.IsConnected.Returns(isConnected);
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Task.FromResult("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                UserSettings.Instance.EnableAdministratorMode = true;
                UserSettings.Instance.AdministratorPassword = "Bla";

                var vm = new MainViewModel();
                vm.Activator.Activate();

                isConnected.FirstAsync(x => x).Subscribe(_ => Assert.False(vm.IsConnected));

                vm.ConnectCommand.Execute(null);
            }
        }
    }
}