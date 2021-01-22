using Acr.UserDialogs;
using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json;
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
        private readonly IWalletRecordService _recordService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMessageService _messageService;
        private readonly IProofService _proofService;
        private readonly ICredentialService _credentialService;

        private readonly ILifetimeScope _scope;
        public ProofRequestsViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IProofService proofService,
            ICredentialService credentialService,
            IAgentProvider agentContextProvider,
            IMessageService messageService,
            IWalletRecordService recordService,
            IEventAggregator eventAggregator,
            IConnectionService connectionService,
            ILifetimeScope scope
            ) : base(
                AppResources.ProofsPageTitle,
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
            _recordService = recordService;
            _credentialService = credentialService;

            this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .InvokeCommand(RefreshCommand);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshProofs();

            _eventAggregator.GetEventByType<ApplicationEvent>()
                           .Where(_ => _.Type == ApplicationEventType.ProofRequestUpdated)
                           .Subscribe(async _ => { await RefreshProofs(); await DetectNewProofRequest(); });

            await base.InitializeAsync(navigationData);
        }

        public async Task DetectNewProofRequest()
        {
            var context = await _agentContextProvider.GetContextAsync();
           
            foreach (var proofRequest in ProofRequests)
            {
                if (proofRequest.IsNew)
                {
                    var record = await _recordService.GetAsync<ProofRecord>(context.Wallet, proofRequest.Id);
                    record.SetTag("IsNew", "false");
                    await _recordService.UpdateAsync(context.Wallet, record);
                    await SelectProofRequest(proofRequest);
                    break;
                }
            }
        }

        public async Task RefreshProofs()
        {
            RefreshingProofRequests = true;

            var context = await _agentContextProvider.GetContextAsync();

            var proofRecords = await _proofService.ListAsync(context);

            // TODO: Tranférer le code de mappage suivant dans un mapper.
            var proofsVms = new List<ProofRequestViewModel>();

            foreach (var proofRecord in proofRecords)
            {
                var proof = _scope.Resolve<ProofRequestViewModel>(new NamedParameter("proof", proofRecord));

                proof.Id = proofRecord.Id;
                
                proof.IsNew = proofRecord.State == ProofState.Requested && string.IsNullOrEmpty(proofRecord.GetTag("IsNew"));
                
                if (proofRecord.CreatedAtUtc.HasValue)
                {
                    proof.Alias = proofRecord.CreatedAtUtc.Value.ToLocalTime().ToString();
                }
                
                if (proofRecord.ProofJson != null)
                {
                    var partialProof = JsonConvert.DeserializeObject<PartialProof>(proofRecord.ProofJson);
                    var proofRequest = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);
                    proof.Attributes.Clear();
                    foreach (var revealedAttributeKey in partialProof.RequestedProof.RevealedAttributes.Keys)
                    {
                        var proofAttribute = new ProofAttribute();
                        proofAttribute.Name = proofRequest.RequestedAttributes[revealedAttributeKey].Name; // TODO: Que faire pour gérer l'attribut Names?
                        proofAttribute.IsNotPredicate = true;
                        proofAttribute.IsRevealed = true;
                        proofAttribute.Type = "Text";
                        proofAttribute.Value = partialProof.RequestedProof.RevealedAttributes[revealedAttributeKey].Raw;
                        proof.Attributes.Add(proofAttribute);
                    }
                }

                proof.ProofState = ProofStateTranslator.Translate(proofRecord.State);

                proofsVms.Add(proof);
            }

            //var proofsVms = proofRecords
            //    .Select(p => _scope.Resolve<ProofRequestViewModel>(new NamedParameter("proof", p)))
            //    .ToList();

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

        public async Task SelectProofRequest(ProofRequestViewModel proof)
        {
            await PreFillProofRequestValues(proof);
            await NavigationService.NavigateToAsync(proof, null, NavigationType.Modal);
        }

        private async Task PreFillProofRequestValues(ProofRequestViewModel proof)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var proofRecord = await _recordService.GetAsync<ProofRecord>(context.Wallet, proof.Id);

            var holderProofObject = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);
            //var credentials = await _proofService.ListCredentialsForProofRequestAsync(context, holderProofObject, "username");

            if (proofRecord.State == ProofState.Requested)
            {
                var proofRequest = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);
                foreach(var requestedAttribute in proofRequest.RequestedAttributes.Values)
                {
                    if (requestedAttribute.Restrictions.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(requestedAttribute.Restrictions[0].CredentialDefinitionId))
                        {
                            foreach(var credentialRecord in await _credentialService.ListIssuedCredentialsAsync(context))
                            {
                                if (credentialRecord.CredentialDefinitionId == requestedAttribute.Restrictions[0].CredentialDefinitionId)
                                {
                                    foreach(var pa in proof.Attributes)
                                    {
                                        if (pa.Name == requestedAttribute.Name)
                                        {
                                            foreach(var cav in credentialRecord.CredentialAttributesValues)
                                            {
                                                if (cav.Name == requestedAttribute.Name)
                                                {
                                                    pa.Value = cav.Value.ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            int bla = 1;
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