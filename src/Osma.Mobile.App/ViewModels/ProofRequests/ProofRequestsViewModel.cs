using Acr.UserDialogs;
using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Osma.Mobile.App.Assemblers;
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
        private readonly IWalletRecordService _recordService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IProofService _proofService;
        private readonly IProofAssembler _proofAssembler;
        private readonly ICredentialService _credentialService;
        private readonly ILifetimeScope _scope;
        private readonly IRequestPresentationFiller _requestPresentationFiller;

        public ProofRequestsViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IProofService proofService,
            ICredentialService credentialService,
            IAgentProvider agentContextProvider,
            IWalletRecordService recordService,
            IEventAggregator eventAggregator,
            IProofAssembler proofAssembler,
            IRequestPresentationFiller requestPresentationFiller,
            ILifetimeScope scope
            ) : base(
                AppResources.ProofsPageTitle,
                userDialogs,
                navigationService
           )
        {
            _proofService = proofService;
            _agentContextProvider = agentContextProvider;
            _scope = scope;
            _eventAggregator = eventAggregator;
            _recordService = recordService;
            _credentialService = credentialService;
            _proofAssembler = proofAssembler;
            _requestPresentationFiller = requestPresentationFiller;

            this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .InvokeCommand(RefreshCommand);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshProofs();

            //_eventAggregator.GetEventByType<ApplicationEvent>()
            //               .Where(_ => _.Type == ApplicationEventType.ProofRequestUpdated)
            //               .Subscribe(async _ => { await RefreshProofs(); await DetectNewProofRequest(); });

            await base.InitializeAsync(navigationData);
        }

        //public async Task DetectNewProofRequest()
        //{
        //    var context = await _agentContextProvider.GetContextAsync();

        //    foreach (var proofRequest in ProofRequests)
        //    {
        //        if (proofRequest.IsNew)
        //        {
        //            var record = await _recordService.GetAsync<ProofRecord>(context.Wallet, proofRequest.Id);
        //            record.SetTag("IsNew", "false");
        //            await _recordService.UpdateAsync(context.Wallet, record);
        //            await SelectProofRequest(proofRequest);
        //            break;
        //        }
        //    }
        //}

        public async Task RefreshProofs()
        {
            RefreshingProofRequests = true;

            IAgentContext context = await _agentContextProvider.GetContextAsync();

            IList<ProofRecord> proofRecords = await _proofService.ListAsync(context);

            IList<ProofRequestViewModel> proofs = await _proofAssembler.AssembleMany(proofRecords);

            IEnumerable<ProofRequestViewModel> filteredProofVms = FilterProofRequests(SearchTerm, proofs);

            IEnumerable<Grouping<string, ProofRequestViewModel>> groupedVms = GroupProofRequests(filteredProofVms);

            ProofRequestsGrouped = groupedVms;

            ProofRequestsCount = String.Format("Nombre de preuve", proofRecords.Count); //AppResources.ProofRequestCount

            HasProofRequests = ProofRequests.Any();

            ProofRequests.Clear();

            ProofRequests.InsertRange(filteredProofVms);

            RefreshingProofRequests = false;
        }

        public async Task SelectProofRequest(ProofRequestViewModel proofRequest)
        {
            await _requestPresentationFiller.Fill(proofRequest);
            await NavigationService.NavigateToAsync(proofRequest, null, NavigationType.Modal);
        }

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

        private async Task ConfigureSettings()
        {
            await NavigationService.NavigateToAsync<AccountViewModel>();
        }

        #region Bindable Command

        public ICommand CheckAccountCommand => new Command(async () => await NavigationService.NavigateToAsync<AccountViewModel>());

        public ICommand CreateInvitationCommand => new Command(async () => await NavigationService.NavigateToAsync<CreateInvitationViewModel>());

        public ICommand RefreshCommand => new Command(async () => await RefreshProofs());

        public ICommand SelectProofRequestCommand => new Command<ProofRequestViewModel>(async (proofs) =>
                                {
                                    if (proofs != null) await SelectProofRequest(proofs);
                                });

        public ICommand ConfigureSettingsCommand => new Command(async () => await ConfigureSettings());

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

        public string SearchTerm
        {
            get => _searchTerm;
            set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
        }

        //AppResources.SearchProofRequests

        #endregion Bindable Properties
    }
}