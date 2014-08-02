using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Espera.Mobile.Core;
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
            public void AppDoeNotDieWhenDeactivatingViewModelBeforeCommandThrows()
            {
                UserSettings.Instance.ServerAddress = "192.168.1.1";

                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Observable.Never<Tuple<ResponseStatus, ConnectionInfo>>().ToTask());
                messenger.IsConnected.Returns(Observable.Return(false));

                NetworkMessenger.Override(messenger);

                new TestScheduler().With(sched =>
                {
                    var vm = new MainViewModel(() => "192.168.1.2");
                    vm.Activator.Activate();

                    vm.ConnectCommand.Execute(null);

                    vm.Activator.Deactivate();

                    sched.AdvanceByMs(MainViewModel.ConnectCommandTimeout.TotalMilliseconds + 10);
                });
            }

            [Fact]
            public async Task ChecksMinimumServerVersion()
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
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(() => "192.168.1.2");
                vm.Activator.Activate();

                var thrown = vm.ConnectionFailed.CreateCollection();

                await AssertEx.ThrowsAsync<ServerVersionException>(async () => await vm.ConnectCommand.ExecuteAsync());

                Assert.Equal(1, thrown.Count);
            }

            [Fact]
            public async Task ConnectsWithCustomIpAddressIfSet()
            {
                UserSettings.Instance.ServerAddress = "192.168.1.3";

                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.Success,
                        new ConnectionInfo
                        {
                            AccessPermission = NetworkAccessPermission.Admin,
                            ServerVersion = new Version("99.99.99")
                        }).ToTaskResult());

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(() => "192.168.1.2");
                vm.Activator.Activate();

                await vm.ConnectCommand.ExecuteAsync();

                messenger.Received(1).ConnectAsync(UserSettings.Instance.ServerAddress, NetworkConstants.DefaultPort,
                    UserSettings.Instance.UniqueIdentifier, null);
            }

            [Fact]
            public async Task DoesntTryToDiscoverServerWithCustomIpAddressIfSet()
            {
                UserSettings.Instance.ServerAddress = "192.168.1.3";

                bool discoverServerSubscribed = false;

                var messenger = Substitute.For<INetworkMessenger>();
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>())
                    .Returns(Observable.Defer(() => Observable.Start(() => discoverServerSubscribed = true).Select(_ => string.Empty)));
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.Success,
                        new ConnectionInfo
                        {
                            AccessPermission = NetworkAccessPermission.Admin,
                            ServerVersion = new Version("99.99.99")
                        }).ToTaskResult());

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(() => "192.168.1.2");
                vm.Activator.Activate();

                await vm.ConnectCommand.ExecuteAsync();

                Assert.False(discoverServerSubscribed);
            }

            [Fact]
            public async Task SmokeTest()
            {
                var isConnected = new BehaviorSubject<bool>(false);
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(isConnected);
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.Success,
                        new ConnectionInfo
                        {
                            AccessPermission = NetworkAccessPermission.Admin,
                            ServerVersion = new Version("99.99.99")
                        }).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));
                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(() => "192.168.1.2");
                vm.Activator.Activate();

                Assert.True(vm.ConnectCommand.CanExecute(null));

                await vm.ConnectCommand.ExecuteAsync();
                isConnected.OnNext(true);

                Assert.False(vm.ConnectCommand.CanExecute(null));

                messenger.Received(1).ConnectAsync("192.168.1.1", UserSettings.Instance.Port, Arg.Any<Guid>(), null);
            }

            [Fact]
            public void TimeoutTriggersConnectionFailed()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Observable.Never<Tuple<ResponseStatus, ConnectionInfo>>().ToTask());
                messenger.IsConnected.Returns(Observable.Return(false));
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(() => "192.168.1.2");
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
            public async Task WrongPasswordTriggersConnectionFailed()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(Observable.Return(false));
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.WrongPassword,
                        new ConnectionInfo { AccessPermission = NetworkAccessPermission.Admin, ServerVersion = new Version(99, 99) }).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                UserSettings.Instance.EnableAdministratorMode = true;
                UserSettings.Instance.AdministratorPassword = "Bla";

                var vm = new MainViewModel(() => "192.168.1.2");
                vm.Activator.Activate();

                var coll = vm.ConnectionFailed.CreateCollection();

                await AssertEx.ThrowsAsync<WrongPasswordException>(async () => await vm.ConnectCommand.ExecuteAsync());

                Assert.Equal(1, coll.Count);
            }
        }

        public class TheDisconnectCommand
        {
            [Fact]
            public async Task SmokeTest()
            {
                var isConnected = new BehaviorSubject<bool>(true);
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(isConnected);

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(() => "192.168.1.2");
                vm.Activator.Activate();

                Assert.True(vm.DisconnectCommand.CanExecute(true));

                await vm.DisconnectCommand.ExecuteAsync();
                isConnected.OnNext(false);

                Assert.False(vm.DisconnectCommand.CanExecute(null));

                messenger.Received(1).Disconnect();
            }
        }

        public class TheIsConnectedProperty
        {
            [Fact]
            public async Task IsFalseWhileConnectCommandExecutesWithPassword()
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
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                UserSettings.Instance.EnableAdministratorMode = true;
                UserSettings.Instance.AdministratorPassword = "Bla";

                var vm = new MainViewModel(() => "192.168.1.2");
                vm.Activator.Activate();

                var coll = isConnected.CreateCollection();

                await vm.ConnectCommand.ExecuteAsync();

                Assert.Equal(new[] { false, true }, coll);
            }
        }
    }
}