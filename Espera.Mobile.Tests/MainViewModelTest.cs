using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Network;
﻿using Espera.Mobile.Core.Settings;
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
                var settings = new UserSettings { ServerAddress = "192.168.1.1" };

                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Observable.Never<Tuple<ResponseStatus, ConnectionInfo>>().ToTask());
                messenger.IsConnected.Returns(false);

                NetworkMessenger.Override(messenger);

                new TestScheduler().With(sched =>
                {
                    var vm = new MainViewModel(settings, () => "192.168.1.2");
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
                messenger.IsConnected.Returns(true);
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.Success,
                        new ConnectionInfo
                        {
                            AccessPermission = NetworkAccessPermission.Admin,
                            ServerVersion = new Version(0, 1, 0)
                        }).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(new UserSettings(), () => "192.168.1.2");
                vm.Activator.Activate();

                var thrown = vm.ConnectionFailed.CreateCollection();

                await AssertEx.ThrowsAsync<ServerVersionException>(async () => await vm.ConnectCommand.ExecuteAsync());

                Assert.Equal(1, thrown.Count);
            }

            [Fact]
            public async Task ConnectsWithCustomIpAddressIfSet()
            {
                var settings = new UserSettings { ServerAddress = "192.168.1.3" };

                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.Success,
                        new ConnectionInfo
                        {
                            AccessPermission = NetworkAccessPermission.Admin,
                            ServerVersion = new Version("99.99.99")
                        }).ToTaskResult());

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(settings, () => "192.168.1.2");
                vm.Activator.Activate();

                await vm.ConnectCommand.ExecuteAsync();

                messenger.Received(1).ConnectAsync(settings.ServerAddress, NetworkConstants.DefaultPort,
                    settings.UniqueIdentifier, null);
            }

            [Fact]
            public async Task DoesntTryToDiscoverServerWithCustomIpAddressIfSet()
            {
                var settings = new UserSettings { ServerAddress = "192.168.1.3" };

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

                var vm = new MainViewModel(settings, () => "192.168.1.2");
                vm.Activator.Activate();

                await vm.ConnectCommand.ExecuteAsync();

                Assert.False(discoverServerSubscribed);
            }

            [Fact]
            public async Task SmokeTest()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(false);
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.Success,
                        new ConnectionInfo
                        {
                            AccessPermission = NetworkAccessPermission.Admin,
                            ServerVersion = new Version("99.99.99")
                        }).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));
                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(new UserSettings(), () => "192.168.1.2");
                vm.Activator.Activate();

                Assert.True(vm.ConnectCommand.CanExecute(null));

                await vm.ConnectCommand.ExecuteAsync();
                messenger.IsConnected.Returns(true);
                messenger.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(messenger, new PropertyChangedEventArgs("IsConnected"));

                Assert.False(vm.ConnectCommand.CanExecute(null));

                messenger.Received(1).ConnectAsync("192.168.1.1", NetworkConstants.DefaultPort, Arg.Any<Guid>(), null);
            }

            [Fact]
            public void TimeoutTriggersConnectionFailed()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Observable.Never<Tuple<ResponseStatus, ConnectionInfo>>().ToTask());
                messenger.IsConnected.Returns(false);
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(new UserSettings(), () => "192.168.1.2");
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
                messenger.IsConnected.Returns(false);
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Tuple.Create(ResponseStatus.WrongPassword,
                        new ConnectionInfo { AccessPermission = NetworkAccessPermission.Admin, ServerVersion = new Version(99, 99) }).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var settings = new UserSettings { AdministratorPassword = "Bla" };

                var vm = new MainViewModel(settings, () => "192.168.1.2");
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
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(true);

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(new UserSettings(), () => "192.168.1.2");
                vm.Activator.Activate();

                Assert.True(vm.DisconnectCommand.CanExecute(true));

                await vm.DisconnectCommand.ExecuteAsync();
                messenger.IsConnected.Returns(false);
                messenger.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(messenger, new PropertyChangedEventArgs("IsConnected"));

                Assert.False(vm.DisconnectCommand.CanExecute(null));

                messenger.Received(1).Disconnect();
            }
        }

        public class TheIsConnectedProperty
        {
            [Fact]
            public async Task IsFalseWhileConnectCommandExecutesWithPassword()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(x =>
                    {
                        messenger.IsConnected.Returns(true);
                        messenger.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(messenger, new PropertyChangedEventArgs("IsConnected"));
                        return Tuple.Create(ResponseStatus.Success,
                            new ConnectionInfo
                            {
                                AccessPermission = NetworkAccessPermission.Admin,
                                ServerVersion = new Version(99, 99)
                            })
                        .ToTaskResult();
                    });
                messenger.IsConnected.Returns(false);
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var settings = new UserSettings { AdministratorPassword = "Bla" };

                var vm = new MainViewModel(settings, () => "192.168.1.2");
                vm.Activator.Activate();

                var coll = messenger.WhenAnyValue(x => x.IsConnected).CreateCollection();

                await vm.ConnectCommand.ExecuteAsync();

                Assert.Equal(new[] { false, true }, coll);
            }
        }
    }
}