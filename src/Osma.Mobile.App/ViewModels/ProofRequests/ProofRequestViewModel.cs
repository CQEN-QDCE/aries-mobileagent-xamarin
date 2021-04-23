using Acr.UserDialogs;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using ReactiveUI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class ProofRequestViewModel : ABaseViewModel
    {
        private readonly IUserDialogs _userDialogs;
        private readonly INavigationService _navigationService;
        private readonly IAgentProvider _agentContextProvider;
        private readonly IConnectionService _connectionService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMessageService _messageService;
        private readonly ProofRecord _proofRecord;
        private readonly ProofRequest _proofRequest;
        private readonly IWalletRecordService _recordService;
        private readonly IProofService _proofService;
        private readonly IProofCredentialSelector _proofCredentialSelector;

        public ProofRequestViewModel(IUserDialogs userDialogs,
                                     INavigationService navigationService,
                                     IProofService proofService,
                                     IAgentProvider agentContextProvider,
                                     IMessageService messageService,
                                     IConnectionService connectionService,
                                     IEventAggregator eventAggregator,
                                     IWalletRecordService recordService,
                                     IProofCredentialSelector proofCredentialSelector,
                                     ProofRecord proof) : base (AppResources.ProofRequestPageTitle, userDialogs, navigationService)
        {
            _proofRecord = proof;
            _proofService = proofService;
            _agentContextProvider = agentContextProvider;
            _messageService = messageService;
            _connectionService = connectionService;
            _eventAggregator = eventAggregator;
            _userDialogs = userDialogs;
            _recordService = recordService;
            _navigationService = navigationService;
            _proofCredentialSelector = proofCredentialSelector;
            GetConnectionAlias();

            _proofRequest = JsonConvert.DeserializeObject<ProofRequest>(_proofRecord.RequestJson);

            ProofName = _proofRequest.Name;

            Version = _proofRequest.Version;

            State = ProofStateTranslator.Translate(_proofRecord.State);

            AreButtonsVisible = _proofRecord.State == ProofState.Requested;

            Attributes = new List<ProofAttributeViewModel>();
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await base.InitializeAsync(navigationData);
        }

        private async Task AcceptProofRequest()
        {
            if (_proofRecord.State != ProofState.Requested)
            {
                await DialogService.AlertAsync(string.Format(AppResources.ProofStateShouldBeMessage, ProofStateTranslator.Translate(ProofState.Requested)));

                return;
            }

            RequestedCredentials requestedCredentials = new RequestedCredentials()
            {
                RequestedAttributes = new Dictionary<string, RequestedAttribute>(),
                RequestedPredicates = new Dictionary<string, RequestedAttribute>()
            };

            foreach (ProofAttributeViewModel proofAttribute in Attributes)
            {
                if (proofAttribute.IsPredicate)
                {
                    requestedCredentials.RequestedPredicates.Add(proofAttribute.Id, new RequestedAttribute { CredentialId = proofAttribute.CredentialId, Revealed = proofAttribute.IsRevealed });
                }
                else
                {
                    requestedCredentials.RequestedAttributes.Add(proofAttribute.Id, new RequestedAttribute { CredentialId = proofAttribute.CredentialId, Revealed = proofAttribute.IsRevealed });
                }
            }

            // TODO: Mettre le Timestamp à null car lorsqu'il est présent, la création de la preuve ne marche pas. Pourquoi?
            //foreach (var keyValue in requestedCredentials.RequestedAttributes.Values)
            //{
            //    keyValue.Timestamp = null;
            //}

            var context = await _agentContextProvider.GetContextAsync();

            ProofRecord proofRecord = await _recordService.GetAsync<ProofRecord>(context.Wallet, _proofRecord.Id);

            var (msg, rec) = await _proofService.CreatePresentationAsync(context, proofRecord.Id, requestedCredentials);

            if (string.IsNullOrEmpty(proofRecord.ConnectionId))
            {
                await _messageService.SendAsync(context.Wallet, msg, proofRecord.GetTag("RecipientKey"), proofRecord.GetTag("ServiceEndpoint"));
            }
            else
            {
                ConnectionRecord connectionRecord = await _recordService.GetAsync<ConnectionRecord>(context.Wallet, proofRecord.ConnectionId);
                await _messageService.SendAsync(context.Wallet, msg, connectionRecord);
            }

            _eventAggregator.Publish(new ApplicationEvent { Type = ApplicationEventType.ProofRequestUpdated });

            await NavigationService.PopModalAsync();
        }

        private void AssignSelectedAttributeValue(CredentialRecord credentialRecord, ProofAttributeViewModel proofAttribute)
        {
            proofAttribute.CredentialId = credentialRecord.Id;

            foreach (CredentialPreviewAttribute credentialPreviewAttribute in credentialRecord.CredentialAttributesValues)
            {
                if (credentialPreviewAttribute.Name == proofAttribute.Name)
                {
                    proofAttribute.Value = credentialPreviewAttribute.Value.ToString();
                }
            }
        }

        private async void GetConnectionAlias()
        {
            if (_proofRecord.ConnectionId == null) return;
            var agentContext = await _agentContextProvider.GetContextAsync();
            var connection = await _connectionService.GetAsync(agentContext, _proofRecord.ConnectionId);
        }

        private async Task RejectProofRequest()
        {
            await NavigationService.PopModalAsync();
        }

        #region Bindable Command

        public ICommand AcceptCommand => new Command(async () =>
        {
            await AcceptProofRequest();
        });

        public ICommand RefuseCommand => new Command(async () =>
        {
             await RejectProofRequest();
        });

        public ICommand SelectAttributeValueCommand => new Command<ProofAttributeViewModel>(async (proofAttribute) =>
        {
            if (proofAttribute != null)
            {
                SelectAttributeValueViewModel viewModel = new SelectAttributeValueViewModel(_userDialogs, 
                                                                                            _navigationService, 
                                                                                            _proofCredentialSelector, 
                                                                                            _proofRequest, 
                                                                                            proofAttribute.Name,
                                                                                            (o) => AssignSelectedAttributeValue(o.CredentialRecord, proofAttribute));

                await NavigationService.NavigateToAsync(viewModel, null, NavigationType.Modal);
            };
        });

        #endregion Bindable Command

        #region Bindable Properties

        private bool _areButtonsVisible;
        private IList<ProofAttributeViewModel> _attributes;
        private string _errorLabel = "ErrorLabel";
        private bool _isRevealed;
        private string _proofName;

        private string _id;

        private string _proofState;

        private string _version;

        private bool _refreshingProofRequest;

        private string _revealDataLabel = "RevealDataLabel";

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public bool AreButtonsVisible
        {
            get => _areButtonsVisible;
            set => this.RaiseAndSetIfChanged(ref _areButtonsVisible, value);
        }

        public IList<ProofAttributeViewModel> Attributes
        {
            get => _attributes;
            set => this.RaiseAndSetIfChanged(ref _attributes, value);
        }

        public string ErrorLabel
        {
            get => _errorLabel;
            set => this.RaiseAndSetIfChanged(ref _errorLabel, value);
        }

        public bool IsRevealed
        {
            get => _isRevealed;
            set => this.RaiseAndSetIfChanged(ref _isRevealed, value);
        }

        public string ProofName
        {
            get => _proofName;
            set => this.RaiseAndSetIfChanged(ref _proofName, value);
        }

        public string State
        {
            get => _proofState;
            set => this.RaiseAndSetIfChanged(ref _proofState, value);
        }

        public string Version
        {
            get => _version;
            set => this.RaiseAndSetIfChanged(ref _version, value);
        }

        public bool RefreshingProofRequest
        {
            get => _refreshingProofRequest;
            set => this.RaiseAndSetIfChanged(ref _refreshingProofRequest, value);
        }

        public string RevealDataLabel
        {
            get => _revealDataLabel;
            set => this.RaiseAndSetIfChanged(ref _revealDataLabel, value);
        }

        #endregion Bindable Properties
    }
}