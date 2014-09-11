using System;
using System.ComponentModel;
using System.Configuration;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
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
                    .Returns(Observable.Never<ConnectionResultContainer>().ToTask());
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
                    .Returns(Task.FromResult(new ConnectionResultContainer(ConnectionResult.ServerVersionToLow, serverVersion: new Version("0.1.0"))));
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(new UserSettings(), () => "192.168.1.2");
                vm.Activator.Activate();

                ConnectionResultContainer result = await vm.ConnectCommand.ExecuteAsync();

                Assert.Equal(ConnectionResult.ServerVersionToLow, result.ConnectionResult);
                Assert.Equal(new Version("0.1.0"), result.ServerVersion);
            }

            [Fact]
            public async Task ConnectsWithCustomIpAddressIfSet()
            {
                var settings = new UserSettings { ServerAddress = "192.168.1.3" };

                var messenger = Substitute.For<INetworkMessenger>();
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Task.FromResult(new ConnectionResultContainer(ConnectionResult.Successful, NetworkAccessPermission.Admin, new Version("99.99.99"))));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(settings, () => "192.168.1.2");
                vm.Activator.Activate();

                ConnectionResultContainer result = await vm.ConnectCommand.ExecuteAsync();

                Assert.Equal(ConnectionResult.Successful, result.ConnectionResult);
                Assert.Equal(new Version("99.99.99"), result.ServerVersion); ;

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
                    .Returns(Task.FromResult(new ConnectionResultContainer(ConnectionResult.Successful, NetworkAccessPermission.Admin, new Version("99.99.99"))));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(settings, () => "192.168.1.2");
                vm.Activator.Activate();

                await vm.ConnectCommand.ExecuteAsync();

                Assert.False(discoverServerSubscribed);
            }

            [Fact]
            public async Task IgnoresPasswordIfNotPremium()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Task.FromResult(new ConnectionResultContainer(ConnectionResult.Successful, NetworkAccessPermission.Admin, new Version("99.99.99"))));

                NetworkMessenger.Override(messenger);

                var settings = new UserSettings
                {
                    AdministratorPassword = "Password",
                    IsPremium = false
                };

                // We're not in the trial period
                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTime.MinValue);
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTime.MinValue + AppConstants.TrialTime);

                var vm = new MainViewModel(settings, () => "192.168.1.2", installationDateFetcher, clock);
                vm.Activator.Activate();

                await vm.ConnectCommand.ExecuteAsync();

                messenger.Received().ConnectAsync("192.168.1.1", settings.Port, new Guid(), null);
            }

            [Fact]
            public async Task SmokeTest()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(false);
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(Task.FromResult(new ConnectionResultContainer(ConnectionResult.Successful, NetworkAccessPermission.Admin, new Version("99.99.99"))));
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
                    .Returns(Observable.Never<ConnectionResultContainer>().ToTask());
                messenger.IsConnected.Returns(false);
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var vm = new MainViewModel(new UserSettings(), () => "192.168.1.2");
                vm.Activator.Activate();

                (new TestScheduler()).With(scheduler =>
                {
                    var connectTask = vm.ConnectCommand.ExecuteAsyncTask();
                    scheduler.AdvanceByMs(MainViewModel.ConnectCommandTimeout.TotalMilliseconds + 1);

                    Assert.Equal(ConnectionResult.Timeout, connectTask.Result.ConnectionResult);
                });
            }

            [Fact]
            public async Task WrongPasswordTriggersConnectionFailed()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.IsConnected.Returns(false);
                messenger.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>())
                    .Returns(new ConnectionResultContainer(ConnectionResult.WrongPassword).ToTaskResult());
                messenger.DiscoverServerAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(Observable.Return("192.168.1.1"));

                NetworkMessenger.Override(messenger);

                var settings = new UserSettings
                {
                    AdministratorPassword = "Bla",
                    IsPremium = true
                };

                var vm = new MainViewModel(settings, () => "192.168.1.2");
                vm.Activator.Activate();

                Assert.Equal(ConnectionResult.WrongPassword, (await vm.ConnectCommand.ExecuteAsync()).ConnectionResult);
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
                        return new ConnectionResultContainer(ConnectionResult.Successful, NetworkAccessPermission.Admin, new Version("99.99.99")).ToTaskResult();
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