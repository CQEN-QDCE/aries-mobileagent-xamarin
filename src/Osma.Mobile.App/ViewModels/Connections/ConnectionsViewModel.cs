using Acr.UserDialogs;
using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Storage;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Extensions;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.Account;
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
        private IDisposable _subscription;

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
                            .Where(_ => _.Type == ApplicationEventType.ConnectionUpdated)
                            .Subscribe(async _ => await RefreshConnections());

            _subscription = _eventAggregator.GetEventByType<ServiceMessageProcessingEvent>()
            .Where(x => x.MessageType == MessageTypes.ConnectionRequest)
            .Subscribe(async x =>
            {
                int bla = 1;
            });
            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshConnections()
        {
            //RefreshingConnections = true;

            IAgentContext context = await _agentContextProvider.GetContextAsync();

            IList<ConnectionRecord> connectionRecords = await _connectionService.ListAsync(context);

            connectionRecords = connectionRecords.OrderBy(r => r.CreatedAtUtc).ToList();

            foreach (var record in connectionRecords)
            {
                AddOrUpdateConnection(record);
            }

            IList<ConnectionViewModel> connectionsToRemove = new List<ConnectionViewModel>();

            foreach (ConnectionViewModel connection in Connections)
            {
                var found = false;
                foreach (var record in connectionRecords)
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
            if (string.IsNullOrWhiteSpace(con.ConnectionName)) con.ConnectionName = "Agent Médiateur";
            con.ConnectionSubtitle = ConnectionStateTranslator.Translate(connectionRecord.State);
            DateTime datetime = DateTime.Now;
            if (connectionRecord.CreatedAtUtc.HasValue)
            {
                datetime = connectionRecord.CreatedAtUtc.Value.ToLocalTime();
                con.DateTime = datetime;
            }
            Connections.Insert(0, con);
        }

        private async Task ConfigureSettings()
        {
            await NavigationService.NavigateToAsync<AccountViewModel>();
        }

        private async Task SelectConnection(ConnectionViewModel connection)
        {
            await NavigationService.NavigateToAsync(connection);
        }

        #region Bindable Command

        public ICommand RefreshCommand => new Command(async () => await RefreshConnections());

        public ICommand SelectConnectionCommand => new Command<ConnectionViewModel>(async (connection) =>
        {
            if (connection != null) await SelectConnection(connection);
        });

        public ICommand ConfigureSettingsCommand => new Command(async () => await ConfigureSettings());

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