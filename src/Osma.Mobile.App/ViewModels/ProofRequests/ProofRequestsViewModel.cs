using Acr.UserDialogs;
using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Extensions;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.Account;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class ProofRequestsViewModel : ABaseViewModel
    {
        private readonly IAgentProvider _agentContextProvider;
        private readonly IConnectionService _connectionService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMessageService _messageService;
        private readonly IProofService _proofService;
        private readonly ILifetimeScope _scope;
        public ProofRequestsViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IProofService proofService,
            IAgentProvider agentContextProvider,
            IMessageService messageService,
            IEventAggregator eventAggregator,
            IConnectionService connectionService,
            ILifetimeScope scope
            ) : base(
                nameof(ProofRequestsViewModel),
                userDialogs,
                navigationService
           )
        {
            _proofService = proofService;
            _agentContextProvider = agentContextProvider;
            _messageService = messageService;
            _scope = scope;
            _connectionService = connectionService;
            _eventAggregator = eventAggregator;

            this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .InvokeCommand(RefreshCommand);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshProofs();

            _eventAggregator.GetEventByType<ApplicationEvent>()
                           .Where(_ => _.Type == ApplicationEventType.ProofRequestUpdated)
                           .Subscribe(async _ => await RefreshProofs());

            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshProofs()
        {
            RefreshingProofRequests = true;

            var context = await _agentContextProvider.GetContextAsync();
            var proofRecords = await _proofService.ListAsync(context);

            var proofsVms = proofRecords
                .Select(p => _scope.Resolve<ProofRequestViewModel>(new NamedParameter("proof", p)))
                .ToList();

            var filteredProofVms = FilterProofRequests(SearchTerm, proofsVms);
            var groupedVms = GroupProofRequests(filteredProofVms);

            ProofRequestsGrouped = groupedVms;
            //ProofRequestsCount = "we have number of records:{0}" + proofRecords.Count;
            ProofRequestsCount = String.Format("Nombre de preuve", proofRecords.Count); //AppResources.ProofRequestCount
            HasProofRequests = ProofRequests.Any();

            ProofRequests.Clear();
            ProofRequests.InsertRange(filteredProofVms);

            RefreshingProofRequests = false;
        }

        public async Task SelectProofRequest(ProofRequestViewModel proof) => await NavigationService.NavigateToAsync(proof, null, NavigationType.Modal);

        private IEnumerable<ProofRequestViewModel> FilterProofRequests(string term, IEnumerable<ProofRequestViewModel> proofs)
        {
            if (string.IsNullOrWhiteSpace(term)) return proofs;
            return proofs.Where(proofRequestViewModel => proofRequestViewModel.ProofName.Contains(term));
        }

        private IEnumerable<Grouping<string, ProofRequestViewModel>> GroupProofRequests(IEnumerable<ProofRequestViewModel> proofRequestsViewModels)
        {
            return proofRequestsViewModels
                .OrderBy(proofRequestsViewModel => proofRequestsViewModel.ProofName)
                .GroupBy(proofRequestsViewModel =>
                {
                    if (string.IsNullOrWhiteSpace(proofRequestsViewModel.ProofName)) return "*";
                    return proofRequestsViewModel.ProofName[0].ToString().ToUpperInvariant();
                }) // TODO check proofRequestName
                .Select(group =>
                {
                    return new Grouping<string, ProofRequestViewModel>(group.Key, group.ToList());
                });
        }

        #region Bindable Command

        public ICommand CheckAccountCommand => new Command(async () => await NavigationService.NavigateToAsync<AccountViewModel>());

        public ICommand CreateInvitationCommand => new Command(async () => await NavigationService.NavigateToAsync<CreateInvitationViewModel>());

        public ICommand RefreshCommand => new Command(async () => await RefreshProofs());

        public ICommand SelectProofRequestCommand => new Command<ProofRequestViewModel>(async (proofs) =>
                                {
            if (proofs != null) await SelectProofRequest(proofs);
        });
        //public ICommand CloudAgentsCommand => new Command(async () => await NavigationService.NavigateToAsync<CloudAgentsViewModel>());
        #endregion Bindable Command

        #region Bindable Properties

        private string _accountInformationLabel = "AccountInformationLabel";
        private string _cloudAgentTitle = "CloudAgentLabel";
        private string _createInvitationLabel = "CreateInvitationLabel";
        private bool _hasProofRequests;
        private RangeEnabledObservableCollection<ProofRequestViewModel> _proofRequests = new RangeEnabledObservableCollection<ProofRequestViewModel>();

        private string _proofRequestsCount;

        private IEnumerable<Grouping<string, ProofRequestViewModel>> _proofRequestsGrouped;

        private string _proofRequestTitle = AppResources.ProofRequestTitle;

        private bool _refreshingProofRequests;

        private string _searchProofRequests = "SearchProofRequests";

        private string _searchTerm;

        public string AccountInformationLabel
        {
            get => _accountInformationLabel;
            set => this.RaiseAndSetIfChanged(ref _accountInformationLabel, value);
        }

        public string CloudAgentTitle
        {
            get => _cloudAgentTitle;
            set => this.RaiseAndSetIfChanged(ref _cloudAgentTitle, value);
        }

        public string CreateInvitationLabel
        {
            get => _createInvitationLabel;
            set => this.RaiseAndSetIfChanged(ref _createInvitationLabel, value);
        }

        public bool HasProofRequests
        {
            get => _hasProofRequests;
            set => this.RaiseAndSetIfChanged(ref _hasProofRequests, value);
        }

        public RangeEnabledObservableCollection<ProofRequestViewModel> ProofRequests
        {
            get => _proofRequests;
            set => this.RaiseAndSetIfChanged(ref _proofRequests, value);
        }
        public string ProofRequestsCount
        {
            get => _proofRequestsCount;
            set => this.RaiseAndSetIfChanged(ref _proofRequestsCount, value);
        }

        public IEnumerable<Grouping<string, ProofRequestViewModel>> ProofRequestsGrouped
        {
            get => _proofRequestsGrouped;
            set => this.RaiseAndSetIfChanged(ref _proofRequestsGrouped, value);
        }

        //AppResources.AccountInformationLabel
        //AppResources.CreateInvitationLabel
        //AppResources.CloudAgentLabel
        public string ProofRequestTitle
        {
            get => _proofRequestTitle;
            set => this.RaiseAndSetIfChanged(ref _proofRequestTitle, value);
        }

        public bool RefreshingProofRequests
        {
            get => _refreshingProofRequests;
            set => this.RaiseAndSetIfChanged(ref _refreshingProofRequests, value);
        }
        public string SearchProofRequests
        {
            get => _searchProofRequests;
            set => this.RaiseAndSetIfChanged(ref _searchProofRequests, value);
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
        }
         //AppResources.SearchProofRequests
        #endregion Bindable Properties
    }
}