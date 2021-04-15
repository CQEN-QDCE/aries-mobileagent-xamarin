using Acr.UserDialogs;
using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Models.Events;
using Osma.Mobile.App.Assemblers;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Extensions;
using Osma.Mobile.App.Services;
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

namespace Osma.Mobile.App.ViewModels.Credentials
{
    public class CredentialsViewModel : ABaseViewModel
    {
        private readonly ICredentialService _credentialService;
        private readonly ICredentialAssembler _credentialAssembler;
        private readonly IConnectionService _connectionService;
        private readonly IAgentProvider _agentContextProvider;
        private readonly ILifetimeScope _scope;
        private readonly IEventAggregator _eventAggregator;
        private IDisposable _subscription;

        public CredentialsViewModel(IUserDialogs userDialogs, INavigationService navigationService, ICredentialService credentialService, ICredentialAssembler credentialAssembler, IConnectionService connectionService, IAgentProvider agentContextProvider, IEventAggregator eventAggregator, ILifetimeScope scope) : base (
                AppResources.CredentialsPageTitle,
                userDialogs,
                navigationService)
        {
            _credentialService = credentialService;
            _credentialAssembler = credentialAssembler;
            _connectionService = connectionService;
            _agentContextProvider = agentContextProvider;
            _eventAggregator = eventAggregator;
            _scope = scope;

            this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .InvokeCommand(RefreshCommand);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshCredentials();

            _eventAggregator.GetEventByType<ApplicationEvent>()
               .Where(_ => _.Type == ApplicationEventType.CredentialUpdated)
               .Subscribe(async _ => await RefreshCredentials());

            _subscription = _eventAggregator.GetEventByType<ServiceMessageProcessingEvent>()
            .Where(x => x.MessageType == MessageTypes.IssueCredentialNames.OfferCredential)
            .Subscribe(async x => {
                await DisplayCredentialOffer(x.RecordId);
            });
            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshCredentials()
        {
            RefreshingCredentials = true;

            IAgentContext context = await _agentContextProvider.GetContextAsync();
            
            IList<CredentialRecord> credentialRecords = await _credentialService.ListAsync(context);

            IList<CredentialViewModel> credentials = await _credentialAssembler.AssembleMany(credentialRecords);

            IEnumerable<CredentialViewModel> filteredCredentialVms = FilterCredentials(SearchTerm, credentials);

            IEnumerable<Grouping<string, CredentialViewModel>> groupedVms = GroupCredentials(filteredCredentialVms);

            CredentialsGrouped = groupedVms;

            Credentials.Clear();

            Credentials.InsertRange(filteredCredentialVms);

            HasCredentials = Credentials.Any();

            RefreshingCredentials = false;
        }

        private async Task DisplayCredentialOffer(string recordId)
        {
            IAgentContext context = await _agentContextProvider.GetContextAsync();

            CredentialRecord credentialRecord = await _credentialService.GetAsync(context, recordId);

            CredentialViewModel credential = await _credentialAssembler.Assemble(credentialRecord);

            await NavigationService.NavigateToAsync(credential, null, NavigationType.Modal);
        }

        public async Task SelectCredential(CredentialViewModel credential) => await NavigationService.NavigateToAsync(credential, null, NavigationType.Modal);

        private IEnumerable<CredentialViewModel> FilterCredentials(string term, IEnumerable<CredentialViewModel> credentials)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return credentials;
            }
            // Basic search
            var filtered = credentials.Where(credentialViewModel => credentialViewModel.CredentialName.Contains(term));
            return filtered;
        }

        private IEnumerable<Grouping<string, CredentialViewModel>> GroupCredentials(IEnumerable<CredentialViewModel> credentialViewModels)
        {
            var grouped = credentialViewModels
            .OrderBy(credentialViewModel => credentialViewModel.CredentialName)
            .GroupBy(credentialViewModel =>
            {
                if (string.IsNullOrWhiteSpace(credentialViewModel.CredentialName))
                {
                    return "*";
                }
                return credentialViewModel.CredentialName[0].ToString().ToUpperInvariant();
            }) // TODO check credentialName
            .Select(group =>
            {
                return new Grouping<string, CredentialViewModel>(group.Key, group.ToList());
            }
            );

            return grouped;
        }

        private async Task ConfigureSettings()
        {
            await NavigationService.NavigateToAsync<AccountViewModel>();
        }

        #region Bindable Command

        public ICommand SelectCredentialCommand => new Command<CredentialViewModel>(async (credentials) =>
        {
            if (credentials != null) await SelectCredential(credentials);
        });

        public ICommand RefreshCommand => new Command(async () => await RefreshCredentials());

        public ICommand ConfigureSettingsCommand => new Command(async () => await ConfigureSettings());

        #endregion Bindable Command

        #region Bindable Properties

        private RangeEnabledObservableCollection<CredentialViewModel> _credentials = new RangeEnabledObservableCollection<CredentialViewModel>();

        public RangeEnabledObservableCollection<CredentialViewModel> Credentials
        {
            get => _credentials;
            set => this.RaiseAndSetIfChanged(ref _credentials, value);
        }

        private bool _hasCredentials;

        public bool HasCredentials
        {
            get => _hasCredentials;
            set => this.RaiseAndSetIfChanged(ref _hasCredentials, value);
        }

        private bool _refreshingCredentials;

        public bool RefreshingCredentials
        {
            get => _refreshingCredentials;
            set => this.RaiseAndSetIfChanged(ref _refreshingCredentials, value);
        }

        private string _searchTerm;

        public string SearchTerm
        {
            get => _searchTerm;
            set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
        }

        private IEnumerable<Grouping<string, CredentialViewModel>> _credentialsGrouped;

        public IEnumerable<Grouping<string, CredentialViewModel>> CredentialsGrouped
        {
            get => _credentialsGrouped;
            set => this.RaiseAndSetIfChanged(ref _credentialsGrouped, value);
        }

        #endregion Bindable Properties
    }
}