using Acr.UserDialogs;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Indy.AnonCredsApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services.Interfaces;
using Plugin.Fingerprint;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class ProofRequestViewModel : ABaseViewModel
    {
        private readonly IAgentProvider _agentContextProvider;
        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMessageService _messageService;
        private readonly INavigationService _navigationService;
        private readonly ProofRecord _proof;

        private readonly IDictionary<string, bool> _proofAttributes = new Dictionary<string, bool>();
        private readonly IDictionary<string, bool> _proofAttributesRevealed = new Dictionary<string, bool>();
        private readonly IProofService _proofService;
        private readonly IProvisioningService _provisioningService;
        private readonly JObject _requestedAttributes;
        private readonly IList<string> _requestedAttributesKeys;
        private readonly Dictionary<string, RequestedAttribute> _requestedAttributesMap = new Dictionary<string, RequestedAttribute>();
        private readonly Dictionary<string, bool> _requestedAttributesRevealedMap = new Dictionary<string, bool>();
        private readonly JObject _requestedPredicates;
        private readonly IList<string> _requestedPredicatesKeys;
        private readonly Dictionary<string, RequestedAttribute> _requestedPredicatesMap = new Dictionary<string, RequestedAttribute>();
        private readonly IUserDialogs _userDialogs;
        private IDictionary<string, bool> _previousProofAttribute = new Dictionary<string, bool>();

        public ProofRequestViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IProofService proofService,
            IAgentProvider agentContextProvider,
            IMessageService messageService,
            ICredentialService credentialService,
            IConnectionService connectionService,
            IEventAggregator eventAggregator,
            IProvisioningService provisioningService,
            ProofRecord proof
        ) : base(
            nameof(ProofRequestViewModel),
            userDialogs,
            navigationService
        )
        {
            _proof = proof;
            _proofService = proofService;
            _agentContextProvider = agentContextProvider;
            _messageService = messageService;
            _credentialService = credentialService;
            _connectionService = connectionService;
            _eventAggregator = eventAggregator;
            _userDialogs = userDialogs;
            _navigationService = navigationService;
            _provisioningService = provisioningService;
            GetConnectionAlias();

            var requestJson = (JObject)JsonConvert.DeserializeObject(_proof.RequestJson);

            ProofName = requestJson["name"]?.ToString();
            ProofVersion = "Version - " + requestJson["version"];
            // ProofState = _proof.State.ToString();
            string proofState = localiserProofState(_proof.State);
            ProofState = proofState;

            if (_proof.State == Hyperledger.Aries.Features.PresentProof.ProofState.Requested)
            {
                AreButtonsVisible = true;
            }
            else
            {
                AreButtonsVisible = false;
            }

            _requestedAttributes = (JObject)requestJson["requested_attributes"];
            _requestedPredicates = (JObject)requestJson["requested_predicates"];

            _requestedAttributesKeys = _requestedAttributes?.Properties().Select(p => p.Name).ToList();
            _requestedPredicatesKeys = _requestedPredicates?.Properties().Select(p => p.Name).ToList();

            if (_requestedAttributesKeys != null)
            {
                Attributes = _requestedAttributesKeys
                    .Select(k =>
                        new ProofAttribute
                        {
                            Name = _requestedAttributes[k]["name"]?.ToString(),
                            Type = "Text",
                            IsRevealed = true,
                            IsNotPredicate = true
                        })
                    .ToList();

                _requestedPredicatesKeys.ForEach(pk =>
                {
                    var pa = new ProofAttribute
                    {
                        Name = _requestedPredicates[pk]["name"]?.ToString(),
                        Type = "Text",
                        IsRevealed = false,
                        IsNotPredicate = false
                    };
                    Attributes.Add(pa);
                });

                _requestedAttributesKeys.ForEach(k => _requestedAttributesRevealedMap.Add(k, true));
            }

            MessagingCenter.Subscribe<PassCodeViewModel>(this, ApplicationEventType.PassCodeAuthorisedProofAccept.ToString(), async (p) =>
            {
                await AcceptProofRequest();
            });

            MessagingCenter.Subscribe<PassCodeViewModel>(this, ApplicationEventType.PassCodeAuthorisedProofReject.ToString(), async (p) =>
            {
                await RejectProofRequest();
            });
        }

        public override async Task InitializeAsync(object navigationData)
        {
            RefreshProofRequest();

            _ = _eventAggregator.GetEventByType<ApplicationEvent>()
                            .Where(_ => _.Type == ApplicationEventType.ProofRequestAtrributeUpdated)
                            .Subscribe(_ => RefreshProofRequest());

            await base.InitializeAsync(navigationData);
        }

        public void RefreshProofRequest()
        {
            RefreshingProofRequest = true;

            IsFrameVisible = false;

            RefreshingProofRequest = false;
        }

        private async Task AcceptProofRequest()
        {
            string proofState = localiserProofState(_proof.State);
            if (_proof.State != Hyperledger.Aries.Features.PresentProof.ProofState.Requested)
            {
                /*await DialogService
                    .AlertAsync("Proof state should be " +
                                Hyperledger.Aries.Features.PresentProof.ProofState.Requested); */
                await DialogService
                   .AlertAsync(string.Format("DialogProofStateShouldBe", proofState)); //AppResources.DialogProofStateShouldBe
                return;
            }
            if (_requestedAttributesMap.Keys.Count + _requestedPredicatesMap.Keys.Count !=
                _requestedAttributesKeys.Count + _requestedPredicatesKeys.Count)
            {
                await DialogService.AlertAsync("ProofAttributesMissing"); //AppResources.ProofAttributesMissing
                return;
            }

            _requestedAttributesRevealedMap.ForEach(attr =>
                _requestedAttributesMap[attr.Key].Revealed = attr.Value);

            var requestedCredentials = new RequestedCredentials
            {
                RequestedAttributes = _requestedAttributesMap,
                RequestedPredicates = _requestedPredicatesMap
            };

            var context = await _agentContextProvider.GetContextAsync();

            //var provisioningRecord = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);

            //var credentialObjects = new List<CredentialInfo>();
            //foreach (var credId in requestedCredentials.GetCredentialIdentifiers())
            //{
            //    credentialObjects.Add(
            //        JsonConvert.DeserializeObject<CredentialInfo>(
            //            await AnonCreds.ProverGetCredentialAsync(context.Wallet, credId)));
            //}

            //var schemas = await BuildSchemasAsync(await agentContext.Pool,
            //    credentialObjects
            //        .Select(x => x.SchemaId)
            //        .Distinct());

            //var definitions = await BuildCredentialDefinitionsAsync(await agentContext.Pool,
            //    credentialObjects
            //        .Select(x => x.CredentialDefinitionId)
            //        .Distinct());

            //var revocationStates = await BuildRevocationStatesAsync(await agentContext.Pool,
            //    credentialObjects,
            //    requestedCredentials);

            //var proofJson = await AnonCreds.ProverCreateProofAsync(agentContext.Wallet, proofRequest.ToJson(),
            //    requestedCredentials.ToJson(), provisioningRecord.MasterSecretId, schemas, definitions,
            //    revocationStates);

            //      var proofJson = await _proofService.CreatePresentationAsync(
            //context,
            //_proof.RequestJson.ToObject<ProofRequest>(),
            //requestedCredentials);
            //      /* This is required */
            //      if (proofJson.Contains("\"rev_reg_id\":null"))
            //      {
            //          String[] separator = { "\"rev_reg_id\":null" };
            //          String[] proofJsonList = proofJson.Split(separator, StringSplitOptions.None);
            //          proofJson = proofJsonList[0] + "\"rev_reg_id\":null,\"timestamp\":null}]}";
            //      }


            var (msg, rec) = await _proofService.CreatePresentationAsync(context, _proof.Id, requestedCredentials);
            // TODO: Trouver comment faire fonctionner cet appel.
            //await _messageService.SendAsync(context.Wallet, msg, rec);
            throw new Exception("Trouver comment faire fonctionner cet appel");

            _eventAggregator.Publish(new ApplicationEvent { Type = ApplicationEventType.ProofRequestUpdated });

            await NavigationService.PopModalAsync();
        }

        private void BuildRequestedAttributesPredicatesMap(CredentialRecord proofCredential)
        {
            IsFrameVisible = false;
            _proofAttributes[_previousProofAttribute.Keys.Single()] = false;

            var requestedAttribute = new RequestedAttribute
            {
                CredentialId = proofCredential.CredentialId,
                Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds()
            };
            var attributeName = _requestedAttributesKeys
                .SingleOrDefault(k =>
                    _requestedAttributes[k]["name"]?.ToString() == _previousProofAttribute.Keys.Single());

            var isPredicate = false;
            if (attributeName == null)
            {
                isPredicate = true;
                attributeName = _requestedPredicatesKeys
                    .SingleOrDefault(k =>
                        _requestedPredicates[k]["name"]?.ToString() == _previousProofAttribute.Keys.Single());
            }

            Attributes
                .ForEach(a =>
                {
                    if (!isPredicate)
                    {
                        if (attributeName != null &&
                            a.Name == _requestedAttributes[attributeName]["name"]?.ToString())
                            a.Value = proofCredential.SchemaId;
                    }
                    else
                    {
                        if (attributeName != null &&
                            a.Name == _requestedPredicates[attributeName]["name"]?.ToString())
                            a.Value = proofCredential.SchemaId;
                    }
                });

            _eventAggregator.Publish(new ApplicationEvent
            {
                Type = ApplicationEventType.ProofRequestAtrributeUpdated
            });

            if (!isPredicate)
            {
                if (_requestedAttributesMap.ContainsKey(attributeName))
                {
                    if (_requestedAttributesMap[attributeName]?.CredentialId !=
                        requestedAttribute.CredentialId)
                        _requestedAttributesMap[attributeName] = requestedAttribute;
                    return;
                }

                _requestedAttributesMap.Add(attributeName, requestedAttribute);
            }
            else
            {
                if (attributeName != null && _requestedPredicatesMap.ContainsKey(attributeName))
                {
                    if (_requestedPredicatesMap[attributeName]?.CredentialId !=
                        requestedAttribute.CredentialId)
                        _requestedPredicatesMap[attributeName] = requestedAttribute;
                    return;
                }

                if (attributeName != null) _requestedPredicatesMap.Add(attributeName, requestedAttribute);
            }
        }

        private async Task BuildRevealedAttributeMap(ProofAttribute proofAttribute)
        {
            string proofState = localiserProofState(_proof.State);
            if (_proof.State != Hyperledger.Aries.Features.PresentProof.ProofState.Requested)
            {
                /*await DialogService
                    .AlertAsync("Proof state should be " +
                                Hyperledger.Aries.Features.PresentProof.ProofState.Requested); */
                await DialogService
                    .AlertAsync(string.Format("DialogProofStateShouldBe", proofState)); //AppResources.DialogProofStateShouldBe
                return;
            }

            if (!_proofAttributesRevealed.ContainsKey(proofAttribute.Name))
            {
                _proofAttributesRevealed.Add(proofAttribute.Name, false);

                IsRevealed = false;
            }
            else
            {
                _proofAttributesRevealed[proofAttribute.Name] = !_proofAttributesRevealed[proofAttribute.Name];

                IsRevealed = _proofAttributesRevealed[proofAttribute.Name];
            }

            var attributeName = _requestedAttributesKeys
                .SingleOrDefault(k => _requestedAttributes[k]["name"]?.ToString() == proofAttribute.Name);

            Attributes
                .ForEach(a =>
                {
                    if (attributeName != null &&
                        a.Name == _requestedAttributes[attributeName]["name"]?.ToString())
                        a.IsRevealed = IsRevealed;
                });

            _eventAggregator.Publish(new ApplicationEvent
            {
                Type = ApplicationEventType.ProofRequestAtrributeUpdated
            });

            if (attributeName != null) _requestedAttributesRevealedMap[attributeName] = IsRevealed;
        }

        private async Task FilterCredentialRecords(string name)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var credentialsRecords = await _credentialService.ListAsync(context);

            IList<JObject> restrictions = null;

            var attributeName = _requestedAttributesKeys
                .SingleOrDefault(k => _requestedAttributes[k]["name"]?.ToString() == name);

            if (attributeName == null)
            {
                attributeName = _requestedPredicatesKeys
                    .SingleOrDefault(k => _requestedPredicates[k]["name"]?.ToString() == name);
                if (attributeName != null)
                    restrictions = _requestedPredicates[attributeName]["restrictions"]?
                        .ToObject<List<JObject>>();
            }
            else
            {
                restrictions = _requestedAttributes[attributeName]["restrictions"]?
                    .ToObject<List<JObject>>();
            }

            if (restrictions != null)
            {
                IList<string> credentialDefinitionIds = restrictions
                    .Select(r => r["cred_def_id"]?.ToString())
                    .Where(r => r != null)
                    .ToList();

                if (credentialDefinitionIds.Count > 0)
                {
                    ProofCredentials = credentialsRecords
                        .Where(cr => cr.State == CredentialState.Issued &&
                                     credentialDefinitionIds.Contains(cr.CredentialDefinitionId))
                        .ToList();
                }
                else
                {
                    ProofCredentials = credentialsRecords;
                }
            }
        }

        private async void GetConnectionAlias()
        {
            if (_proof.ConnectionId == null) return;
            var agentContext = await _agentContextProvider.GetContextAsync();
            var connection = await _connectionService.GetAsync(agentContext, _proof.ConnectionId);
            Alias = connection.Alias.Name;
        }
        private async Task<bool> isAuthenticatedAsync(ApplicationEventType eventType)
        {
            var result = await CrossFingerprint.Current.IsAvailableAsync(true);
            bool authenticated = true;
            if (result)
            {
                //var auth = await CrossFingerprint.Current.AuthenticateAsync(AppResources.PleaseAuthenticateToProceed); //AppResources.PleaseAuthenticateToProceed
                //if (!auth.Authenticated)
                //{
                //    authenticated = false;
                //}
                /*}
                else
                {
                    authenticated = false;
                    var vm = new PassCodeViewModel(_userDialogs, _navigationService, _agentContextProvider, _provisioningService);
                    vm.Event = eventType;
                    await NavigationService.NavigateToPopupAsync<PassCodeViewModel>(true, vm); */
            }
            return authenticated;
        }

        private async Task LoadProofCredentials(ProofAttribute proofAttribute)
        {
            string proofState = localiserProofState(_proof.State);
            if (_proof.State != Hyperledger.Aries.Features.PresentProof.ProofState.Requested)
            {
                /*await DialogService
                    .AlertAsync("Proof state should be " +
                                Hyperledger.Aries.Features.PresentProof.ProofState.Requested); */
                await DialogService
                    .AlertAsync(string.Format("DialogProofStateShouldBe", proofState)); //AppResources.DialogProofStateShouldBe
                return;
            }
            if (_previousProofAttribute.Any() && !_previousProofAttribute.ContainsKey(proofAttribute.Name))
                _proofAttributes[_previousProofAttribute.Keys.Single()] = false;

            if (!_proofAttributes.ContainsKey(proofAttribute.Name))
            {
                _proofAttributes.Add(proofAttribute.Name, true);
                _previousProofAttribute = new Dictionary<string, bool> { { proofAttribute.Name, true } };

                IsFrameVisible = true;
            }
            else
            {
                _proofAttributes[proofAttribute.Name] = !_proofAttributes[proofAttribute.Name];
                _previousProofAttribute = new Dictionary<string, bool>
                {
                    { proofAttribute.Name, _proofAttributes[proofAttribute.Name] }
                };

                IsFrameVisible = _proofAttributes[proofAttribute.Name];
            }

            if (!IsFrameVisible) return;

            await FilterCredentialRecords(proofAttribute.Name);
        }
        private string localiserProofState(ProofState proofState)
        {
            string strOut = "";
            switch (proofState)
            {
                case Hyperledger.Aries.Features.PresentProof.ProofState.Requested:
                    strOut = "ProofStateRequestedLabel"; // AppResources.ProofStateRequestedLabel;
                    break;

                case Hyperledger.Aries.Features.PresentProof.ProofState.Accepted:
                    strOut = "ProofStateAcceptedLabel"; // AppResources.ProofStateAcceptedLabel;
                    break;

                case Hyperledger.Aries.Features.PresentProof.ProofState.Rejected:
                    strOut = "ProofStateRejectedLabel"; // AppResources.ProofStateRejectedLabel;
                    break;

                default:
                    break;
            }

            return strOut;
        }

        private async Task RejectProofRequest()
        {
            await NavigationService.PopModalAsync();
        }
        #region Bindable Command

        public ICommand AcceptProofRequestCommand => new Command(async () =>
        {
            if (await isAuthenticatedAsync(ApplicationEventType.PassCodeAuthorisedProofAccept))
            {
                await AcceptProofRequest();
            }
        });

        public ICommand NavigateBackCommand => new Command(async () =>
                    await NavigationService.PopModalAsync());
        public ICommand RefreshCommand => new Command(_ => RefreshProofRequest());

        public ICommand RejectProofRequestCommand => new Command(async () =>
                {
            if (await isAuthenticatedAsync(ApplicationEventType.PassCodeAuthorisedProofReject))
            {
                await RejectProofRequest();
            }
        });

        public ICommand SelectProofAttributeCommand => new Command<ProofAttribute>(async (proofAttribute) =>
        {
            if (proofAttribute != null) await LoadProofCredentials(proofAttribute);
        });

        public ICommand SelectProofCredentialCommand => new Command<CredentialRecord>(proofCredential =>
        {
            if (proofCredential != null) BuildRequestedAttributesPredicatesMap(proofCredential);
        });
        public ICommand ToggledCommand => new Command<ProofAttribute>(async (proofAttribute) =>
        {
            if (proofAttribute != null) await BuildRevealedAttributeMap(proofAttribute);
        });

        #endregion Bindable Command

        #region Bindable Properties

        private string _acceptProofRequestLabel = "AcceptProofRequestLabel";
        private string _alias;
        private bool _areButtonsVisible;
        private IList<ProofAttribute> _attributes;
        private string _digitalProofRequestLabel = "DigitalProofRequestLabel";
        private string _errorLabel = "ErrorLabel";
        private bool _isFrameVisible;
        private bool _isRevealed;
        private IList<CredentialRecord> _proofCredentials;
        private string _proofName;

        private string _proofState;

        private string _proofVersion;

        private bool _refreshingProofRequest;

        private string _rejectProofRequestLabel = "RejectProofRequestLabel";

        private string _revealDataLabel = "RevealDataLabel";

        public string AcceptProofRequestLabel
        {
            get => _acceptProofRequestLabel;
            set => this.RaiseAndSetIfChanged(ref _acceptProofRequestLabel, value);
        }

        public string Alias
        {
            get => _alias;
            set => this.RaiseAndSetIfChanged(ref _alias, value);
        }

        public bool AreButtonsVisible
        {
            get => _areButtonsVisible;
            set => this.RaiseAndSetIfChanged(ref _areButtonsVisible, value);
        }

        public IList<ProofAttribute> Attributes
        {
            get => _attributes;
            set => this.RaiseAndSetIfChanged(ref _attributes, value);
        }

        public string DigitalProofRequestLabel
        {
            get => _digitalProofRequestLabel;
            set => this.RaiseAndSetIfChanged(ref _digitalProofRequestLabel, value);
        }

        public string ErrorLabel
        {
            get => _errorLabel;
            set => this.RaiseAndSetIfChanged(ref _errorLabel, value);
        }

        public bool IsFrameVisible
        {
            get => _isFrameVisible;
            set => this.RaiseAndSetIfChanged(ref _isFrameVisible, value);
        }

        public bool IsRevealed
        {
            get => _isRevealed;
            set => this.RaiseAndSetIfChanged(ref _isRevealed, value);
        }

        public IList<CredentialRecord> ProofCredentials
        {
            get => _proofCredentials;
            set => this.RaiseAndSetIfChanged(ref _proofCredentials, value);
        }

        public string ProofName
        {
            get => _proofName;
            set => this.RaiseAndSetIfChanged(ref _proofName, value);
        }
        public string ProofState
        {
            get => _proofState;
            set => this.RaiseAndSetIfChanged(ref _proofState, value);
        }
        public string ProofVersion
        {
            get => _proofVersion;
            set => this.RaiseAndSetIfChanged(ref _proofVersion, value);
        }
        public bool RefreshingProofRequest
        {
            get => _refreshingProofRequest;
            set => this.RaiseAndSetIfChanged(ref _refreshingProofRequest, value);
        }
         //AppResources.DigitalProofRequestLabel
         // AppResources.AcceptProofRequestLabel
         // AppResources.RejectProofRequestLabel

        public string RejectProofRequestLabel
        {
            get => _rejectProofRequestLabel;
            set => this.RaiseAndSetIfChanged(ref _rejectProofRequestLabel, value);
        }

         //AppResources.RevealDataLabel

        public string RevealDataLabel
        {
            get => _revealDataLabel;
            set => this.RaiseAndSetIfChanged(ref _revealDataLabel, value);
        }

         //AppResources.ErrorLabel
        #endregion Bindable Properties
    }
}