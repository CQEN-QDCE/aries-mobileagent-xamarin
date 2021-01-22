using Acr.UserDialogs;
using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Decorators.Service;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Extensions;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.Connections
{
    public class ConnectionsViewModel : ABaseViewModel
    {
        private readonly IConnectionService _connectionService;
        private readonly IAgentProvider _agentContextProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly IProofService _proofService;
        private readonly IWalletRecordService _recordService;
        private readonly ILifetimeScope _scope;

        public ConnectionsViewModel(IUserDialogs userDialogs,
                                    INavigationService navigationService,
                                    IConnectionService connectionService,
                                    IAgentProvider agentContextProvider,
                                    IEventAggregator eventAggregator,
                                    IProofService proofService,
                                    IWalletRecordService recordService,
                                    ILifetimeScope scope) :
                                    base(AppResources.ConnectionsPageTitle, userDialogs, navigationService)
        {
            _connectionService = connectionService;
            _agentContextProvider = agentContextProvider;
            _eventAggregator = eventAggregator;
            _proofService = proofService;
            _recordService = recordService;
            _scope = scope;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshConnections();

            _eventAggregator.GetEventByType<ApplicationEvent>()
                            .Where(_ => _.Type == ApplicationEventType.ConnectionsUpdated)
                            .Subscribe(async _ => await RefreshConnections());

            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshConnections()
        {
            //RefreshingConnections = true;

            var context = await _agentContextProvider.GetContextAsync();
            var records = await _connectionService.ListAsync(context);
            records = records.OrderBy(r => r.CreatedAtUtc).ToList();
            foreach (var record in records)
            {
                AddOrUpdateConnection(record);
            }
            var connectionsToRemove = new List<ConnectionViewModel>();
            foreach (ConnectionViewModel connection in Connections)
            {
                var found = false;
                foreach (var record in records)
                {
                    if (record.Id == connection.Id)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    connectionsToRemove.Add(connection);
                }
            }

            foreach (ConnectionViewModel connection in connectionsToRemove)
            {
                Connections.Remove(connection);
            }
            HasConnections = Connections.Any();

            //RefreshingConnections = false;
        }

        private void AddOrUpdateConnection(ConnectionRecord connectionRecord)
        {
            foreach (ConnectionViewModel connection in Connections)
            {
                if (connectionRecord.Id == connection.Id)
                {
                    connection.ConnectionSubtitle = ConnectionStateTranslator.Translate(connectionRecord.State);
                    return;
                }
            }
            var con = _scope.Resolve<ConnectionViewModel>(new NamedParameter("record", connectionRecord));
            con.ConnectionSubtitle = ConnectionStateTranslator.Translate(connectionRecord.State);
            DateTime datetime = DateTime.Now;
            if (connectionRecord.CreatedAtUtc.HasValue)
            {
                datetime = connectionRecord.CreatedAtUtc.Value.ToLocalTime();
                con.DateTime = datetime;
            }
            Connections.Insert(0, con);
        }

        public async Task ScanInvite()
        {
            //var expectedFormat = ZXing.BarcodeFormat.QR_CODE;
            
            //var opts = new ZXing.Mobile.MobileBarcodeScanningOptions { PossibleFormats = new List<ZXing.BarcodeFormat> { expectedFormat } };

            var context = await _agentContextProvider.GetContextAsync();

            //var scanner = new ZXing.Mobile.MobileBarcodeScanner();

            //var result = await scanner.Scan(opts);

            //if (result == null) return;

            //AgentMessage message = await MessageDecoder.ParseMessageAsync(result.Text);

            AgentMessage message = await MessageDecoder.ParseMessageAsync("https://vc-authn-controller-vc-auth.apps.exp.lab.pocquebec.org/url/a6659ab9-4878-450e-8731-fb2910dd672c");

            switch (message)
            {
                case ConnectionInvitationMessage invitation:
                    break;

                case RequestPresentationMessage presentation:
                    RequestPresentationMessage proofRequest = (RequestPresentationMessage)presentation;
                    var service = message.GetDecorator<ServiceDecorator>(DecoratorNames.ServiceDecorator);
                    ProofRecord proofRecord = await _proofService.ProcessRequestAsync(context, proofRequest, null);
                    proofRecord.SetTag("RecipientKey", service.RecipientKeys.ToList()[0]);
                    proofRecord.SetTag("ServiceEndpoint", service.ServiceEndpoint);
                    await _recordService.UpdateAsync(context.Wallet, proofRecord);
                    _eventAggregator.Publish(new ApplicationEvent { Type = ApplicationEventType.ProofRequestUpdated });
                    break;

                default:
                    DialogService.Alert("Invalid invitation!");
                    return;
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                if (message is ConnectionInvitationMessage)
                {
                    await NavigationService.NavigateToAsync<AcceptInviteViewModel>(message as ConnectionInvitationMessage, NavigationType.Modal);
                }
            });
        }

        public async Task SelectConnection(ConnectionViewModel connection) => await NavigationService.NavigateToAsync(connection);

        //        public async Task SelectConnection(ConnectionViewModel connection) => await NavigationService.NavigateToAsync(connection, NavigationType.Modal);

        #region Bindable Command

        public ICommand RefreshCommand => new Command(async () => await RefreshConnections());

        public ICommand ScanInviteCommand => new Command(async () => await ScanInvite());

        public ICommand CreateInvitationCommand => new Command(async () => await NavigationService.NavigateToAsync<CreateInvitationViewModel>());

        public ICommand SelectConnectionCommand => new Command<ConnectionViewModel>(async (connection) =>
        {
            if (connection != null)
                await SelectConnection(connection);
        });

        #endregion Bindable Command

        #region Bindable Properties

        private RangeEnabledObservableCollection<ConnectionViewModel> _connections = new RangeEnabledObservableCollection<ConnectionViewModel>();

        //  private IList<ConnectionViewModel> _connections = new List<ConnectionViewModel>();
        public RangeEnabledObservableCollection<ConnectionViewModel> Connections
        {
            get => _connections;
            set => this.RaiseAndSetIfChanged(ref _connections, value);
        }

        //public IList<ConnectionViewModel> Connections
        //{
        //    get => _connections;
        //    set => this.RaiseAndSetIfChanged(ref _connections, value);
        //}

        private bool _hasConnections;

        public bool HasConnections
        {
            get => _hasConnections;
            set => this.RaiseAndSetIfChanged(ref _hasConnections, value);
        }

        private bool _refreshingConnections;

        public bool RefreshingConnections
        {
            get => _refreshingConnections;
            set => this.RaiseAndSetIfChanged(ref _refreshingConnections, value);
        }

        #endregion Bindable Properties
    }
}