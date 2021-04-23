using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Routing;
using Hyperledger.Aries.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Osma.Mobile.App.Assemblers;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels;
using Osma.Mobile.App.ViewModels.Account;
using Osma.Mobile.App.ViewModels.Connections;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using Osma.Mobile.App.ViewModels.Credentials;
using Osma.Mobile.App.ViewModels.ProofRequests;
using Osma.Mobile.App.Views;
using Osma.Mobile.App.Views.Account;
using Osma.Mobile.App.Views.Connections;
using Osma.Mobile.App.Views.CreateInvitation;
using Osma.Mobile.App.Views.Credentials;
using Osma.Mobile.App.Views.ProofRequests;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
[assembly: ExportFont("fa-regular-400.ttf", Alias = "FA-R")]
[assembly: ExportFont("fa-brands-400.ttf", Alias = "FA-B")]
[assembly: ExportFont("fa-solid-900.ttf", Alias = "FA-S")]

namespace Osma.Mobile.App
{
    public partial class App : Application
    {
        public new static App Current => Application.Current as App;
        public static IContainer Container { get; set; }

        private static Assembly _platformSpecific;

        public static int ScreenHeight { get; set; }
        public static int ScreenWidth { get; set; }

        // Timer to check new messages in the configured mediator agent every 10sec
        private readonly Timer timer;

        private static IHost Host { get; set; }

        public App()
        {
            InitializeComponent();

            timer = new Timer
            {
                Enabled = false,
                AutoReset = true,
                Interval = TimeSpan.FromSeconds(10).TotalMilliseconds
            };
            timer.Elapsed += Timer_Elapsed;
            CultureInfo.CurrentUICulture = new CultureInfo("fr", false); /* 1 */
        }

        public App(IHost host) : this() => Host = host;

        public static IHostBuilder BuildHost(Assembly platformSpecific = null)
        {
            _platformSpecific = platformSpecific;
            return XamarinHost.CreateDefaultBuilder<App>()
                .ConfigureServices((_, services) =>
                {
                    services.AddAriesFramework(builder => builder.RegisterEdgeAgent(
                        options: options =>
                        {
                            string basePath = string.Empty;

                            basePath = FileSystem.AppDataDirectory;

                            options.EndpointUri = "http://ma-sqin-mediator-agent.apps.exp.lab.pocquebec.org";

                            options.WalletConfiguration.StorageConfiguration =
                                new WalletConfiguration.WalletStorageConfiguration
                                {
                                    Path = Path.Combine(
                                        path1: basePath,
                                        path2: ".indy_client",
                                        path3: "wallets")
                                };
                            options.WalletConfiguration.Id = "MobileWallet";
                            options.WalletCredentials.Key = "SecretWalletKey";
                            options.RevocationRegistryDirectory = Path.Combine(
                                path1: basePath,
                                path2: ".indy_client",
                                path3: "tails");

                            // Available network configurations (see PoolConfigurator.cs):
                            //   sovrin-live
                            //   sovrin-staging
                            //   sovrin-builder
                            //   bcovrin-test
                            options.PoolName = "vonx-pocquebec";
                            options.ProtocolVersion = 2;
                            //                string test = Path.Combine(
                            //path1: basePath,
                            //path2: ".indy_client",
                            //path3: "pool\\vonx-pocquebec\\vonx-pocquebec.txn");
                            //                test = test.Replace("\\", "/");
                            //                options.GenesisFilename = test;

                            int bla = 1;
                        },
                        delayProvisioning: true));

                    services.AddSingleton<IPoolConfigurator, PoolConfigurator>();
                    services.AddSingleton<IWalletRecordService, SqinWalletRecordService>();
                    services.AddSingleton<IConnectionAssembler, ConnectionAssembler>();
                    services.AddSingleton<ICredentialAssembler, CredentialAssembler>();
                    services.AddSingleton<IProofAssembler, ProofAssembler>();
                    services.AddSingleton<IProofCredentialSelector, ProofCredentialSelector>();
                    services.AddSingleton<IRequestPresentationFiller, RequestPresentationFiller>();

                    var containerBuilder = new ContainerBuilder();
                    containerBuilder.RegisterAssemblyModules(typeof(CoreModule).Assembly);
                    if (platformSpecific != null)
                    {
                        containerBuilder.RegisterAssemblyModules(platformSpecific);
                    }

                    containerBuilder.Populate(services);
                    Container = containerBuilder.Build();
                });
        }

        protected override async void OnStart()
        {
            //Preferences.Clear();
            await Host.StartAsync();

            // View models and pages mappings
            var _navigationService = Container.Resolve<INavigationService>();
            _navigationService.AddPageViewModelBinding<MainViewModel, MainPage>();
            _navigationService.AddPageViewModelBinding<ConnectionsViewModel, ConnectionsPage>();
            _navigationService.AddPageViewModelBinding<ConnectionViewModel, ConnectionPage>();
            _navigationService.AddPageViewModelBinding<RegisterViewModel, RegisterPage>();
            _navigationService.AddPageViewModelBinding<AcceptInviteViewModel, AcceptInvitePage>();
            _navigationService.AddPageViewModelBinding<CredentialsViewModel, CredentialsPage>();
            _navigationService.AddPageViewModelBinding<CredentialViewModel, CredentialPage>();
            _navigationService.AddPageViewModelBinding<AccountViewModel, AccountPage>();
            _navigationService.AddPageViewModelBinding<CreateInvitationViewModel, CreateInvitationPage>();
            _navigationService.AddPageViewModelBinding<ProofRequestsViewModel, ProofRequestsPage>();
            _navigationService.AddPageViewModelBinding<ProofRequestViewModel, ProofRequestPage>();
            _navigationService.AddPageViewModelBinding<SelectAttributeValueViewModel, SelectAttributeValuePage>();
            //_navigationService.AddPopupViewModelBinding<SelectAttributeValueViewModel, SelectAttributeValuePage>();

            if (Preferences.Get(AppConstant.LocalWalletProvisioned, false))
            {
                await _navigationService.NavigateToAsync<MainViewModel>();
            }
            else
            {
                await _navigationService.NavigateToAsync<RegisterViewModel>();
            }
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Check for new messages with the mediator agent if successfully provisioned
            if (Preferences.Get(AppConstant.LocalWalletProvisioned, false))
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var context = await Container.Resolve<IAgentProvider>().GetContextAsync();
                        var result = await Container.Resolve<IEdgeClientService>().FetchInboxAsync(context);
                        if (result.processedCount > 0)
                        {
                            Container.Resolve<IEventAggregator>().Publish(new ApplicationEvent() { Type = ApplicationEventType.ConnectionUpdated });
                            //Container.Resolve<IEventAggregator>().Publish(new ApplicationEvent() { Type = ApplicationEventType.CredentialUpdated });
                            Container.Resolve<IEventAggregator>().Publish(new ApplicationEvent() { Type = ApplicationEventType.ProofRequestUpdated });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                });
            }
        }

        protected override void OnSleep() =>
            // Stop timer when application goes to background
            timer.Enabled = false;

        protected override void OnResume() =>
            // Resume timer when application comes in foreground
            timer.Enabled = true;
    }
}