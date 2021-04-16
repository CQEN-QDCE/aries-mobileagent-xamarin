using Acr.UserDialogs;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
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
        public readonly IAgentProvider _agentContextProvider;
        public readonly IConnectionService _connectionService;
        public readonly ICredentialService _credentialService;
        public readonly IEventAggregator _eventAggregator;
        public readonly IMessageService _messageService;
        public readonly INavigationService _navigationService;
        public readonly ProofRecord _proof;
        public readonly ProofRequest _proofRequest;
        public readonly IWalletRecordService _recordService;

        public readonly IDictionary<string, bool> _proofAttributes = new Dictionary<string, bool>();
        public readonly IDictionary<string, bool> _proofAttributesRevealed = new Dictionary<string, bool>();
        public readonly IProofService _proofService;
        public readonly IProvisioningService _provisioningService;
        public readonly JObject _requestedAttributes;
        public readonly IList<string> _requestedAttributesKeys;
        public readonly Dictionary<string, RequestedAttribute> _requestedAttributesMap = new Dictionary<string, RequestedAttribute>();
        public readonly Dictionary<string, bool> _requestedAttributesRevealedMap = new Dictionary<string, bool>();
        public readonly JObject _requestedPredicates;
        public readonly IList<string> _requestedPredicatesKeys;
        public readonly Dictionary<string, RequestedAttribute> _requestedPredicatesMap = new Dictionary<string, RequestedAttribute>();
        public readonly IUserDialogs _userDialogs;
        public IDictionary<string, bool> _previousProofAttribute = new Dictionary<string, bool>();

        public string _attributeNameInEdition;


        public ProofRequestViewModel(IUserDialogs userDialogs,
                                     INavigationService navigationService,
                                     IProofService proofService,
                                     IAgentProvider agentContextProvider,
                                     IMessageService messageService,
                                     ICredentialService credentialService,
                                     IConnectionService connectionService,
                                     IEventAggregator eventAggregator,
                                     IProvisioningService provisioningService,
                                     IWalletRecordService recordService,
                                     ProofRecord proof
        ) : base(
            AppResources.ProofRequestPageTitle,
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
            _recordService = recordService;
            _navigationService = navigationService;
            _provisioningService = provisioningService;
            GetConnectionAlias();

            JObject requestJson = (JObject)JsonConvert.DeserializeObject(_proof.RequestJson);

            _proofRequest = JsonConvert.DeserializeObject<ProofRequest>(_proof.RequestJson);

            ProofName = requestJson["name"]?.ToString();
            
            ProofVersion = "Version - " + requestJson["version"];
            
            string proofState = ProofStateTranslator.Translate(_proof.State);

            ProofState = proofState;

            _requestedAttributesMap = new Dictionary<string, RequestedAttribute>();
 
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
            string proofState = ProofStateTranslator.Translate(_proof.State);

            if (_proof.State != Hyperledger.Aries.Features.PresentProof.ProofState.Requested)
            {
                await DialogService.AlertAsync(string.Format(AppResources.ProofStateShouldBeMessage, ProofStateTranslator.Translate(Hyperledger.Aries.Features.PresentProof.ProofState.Requested)));
                return;
            }

            if (_requestedAttributesMap.Keys.Count + _requestedPredicatesMap.Keys.Count !=
                _requestedAttributesKeys.Count + _requestedPredicatesKeys.Count)
            {
                await DialogService.AlertAsync(AppResources.ProofAttributesMissingMessage);
                return;
            }

            _requestedAttributesRevealedMap.ForEach(attr => _requestedAttributesMap[attr.Key].Revealed = attr.Value);

            var requestedCredentials = new RequestedCredentials()
            {
                RequestedAttributes = _requestedAttributesMap,
                RequestedPredicates = _requestedPredicatesMap
            };

            // TODO: Mettre le Timestamp à null car lorsqu'il est présent, la création de la preuve ne marche pas. Pourquoi?
            foreach (var keyValue in requestedCredentials.RequestedAttributes.Values)
            {
                keyValue.Timestamp = null;
            }

            var context = await _agentContextProvider.GetContextAsync();

            var (msg, rec) = await _proofService.CreatePresentationAsync(context, _proof.Id, requestedCredentials);

            if (string.IsNullOrEmpty(_proof.ConnectionId))
            {
                await _messageService.SendAsync(context.Wallet, msg, _proof.GetTag("RecipientKey"), _proof.GetTag("ServiceEndpoint"));
            }
            else
            {
                ConnectionRecord connectionRecord = await _recordService.GetAsync<ConnectionRecord>(context.Wallet, _proof.ConnectionId);
                await _messageService.SendAsync(context.Wallet, msg, connectionRecord);
            }

            _eventAggregator.Publish(new ApplicationEvent { Type = ApplicationEventType.ProofRequestUpdated });

            await NavigationService.PopModalAsync();
        }

        public void BuildRequestedAttributesPredicatesMap(CredentialRecord proofCredential)
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
            string value = string.Empty;
            foreach(var credentialAttributesValue in proofCredential.CredentialAttributesValues)
            {
                if (credentialAttributesValue.Name == _attributeNameInEdition)
                {
                    value = credentialAttributesValue.Value.ToString();
                }
            }
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
                            a.Value = value;
                    }
                    else
                    {
                        if (attributeName != null &&
                            a.Name == _requestedPredicates[attributeName]["name"]?.ToString())
                            a.Value = proofCredential.SchemaId;
                    }
                });

            IList<ProofAttribute> newList = new List<ProofAttribute>();
            foreach(var attr in Attributes)
            {
                newList.Add(attr);
            }

            Attributes = newList;

            _eventAggregator.Publish(new ApplicationEvent
            {
                Type = ApplicationEventType.ProofRequestAtrributeUpdated
            });

            string key = null;
            foreach (var requestedAttribute2 in _proofRequest.RequestedAttributes)
            {
                if (requestedAttribute2.Value.Name == attributeName)
                {
                    key = requestedAttribute2.Key;
                }
            }

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
            string proofState = ProofStateTranslator.Translate(_proof.State);

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
            IAgentContext context = await _agentContextProvider.GetContextAsync();

            List<CredentialRecord> credentialRecords = await _credentialService.ListAsync(context);

            credentialRecords = credentialRecords.Where(cr => cr.State == CredentialState.Issued).ToList();

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

                IList<ProofCredential> proofCredentials = new List<ProofCredential>();

                if (credentialDefinitionIds.Count > 0)
                {
                    foreach (CredentialRecord credentialRecord in credentialRecords)
                    {
                        if (credentialRecord.State == CredentialState.Issued && credentialDefinitionIds.Contains(credentialRecord.CredentialDefinitionId))
                        {
                            foreach (var credentialAttributeValue in credentialRecord.CredentialAttributesValues)
                            {
                                if (credentialAttributeValue.Name == name)
                                {
                                    var proofCredential = new ProofCredential
                                    {
                                        AttributeName = name,
                                        AttributeValue = credentialAttributeValue.Value.ToString(),
                                        IssuedAt = credentialRecord.CreatedAtUtc.HasValue ? credentialRecord.CreatedAtUtc : null,
                                        SchemaName = SchemaId.Parse(credentialRecord.SchemaId).Name,
                                        CredentialRecord = credentialRecord
                                    };
                                    proofCredentials.Add(proofCredential);
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach(CredentialRecord credentialRecord in credentialRecords)
                    {
                        foreach(var credentialAttributeValue in credentialRecord.CredentialAttributesValues)
                        {
                            if (credentialAttributeValue.Name == name)
                            {
                                var proofCredential = new ProofCredential
                                {
                                    AttributeName = name,
                                    AttributeValue = credentialAttributeValue.Value.ToString(),
                                    IssuedAt = credentialRecord.CreatedAtUtc.HasValue ? credentialRecord.CreatedAtUtc : null,
                                    SchemaName = SchemaId.Parse(credentialRecord.SchemaId).Name,
                                    CredentialRecord = credentialRecord
                                };
                                proofCredentials.Add(proofCredential);
                            }
                        }
                    }
                }
                SelectedAttributeName = name;
                ProofCredentials = proofCredentials;
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
            string proofState = ProofStateTranslator.Translate(_proof.State);

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
                _attributeNameInEdition = proofAttribute.Name;
                IsFrameVisible = false;
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

            //if (!IsFrameVisible) return;

            await FilterCredentialRecords(proofAttribute.Name);
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

        public ICommand CancelSelectAttributeValueCommand => new Command(async () =>
        {
            IsFrameVisible = false;
        });

        public ICommand DoneSelectAttributeValueCommand => new Command(async () =>
        {
            if (await isAuthenticatedAsync(ApplicationEventType.PassCodeAuthorisedProofAccept))
            {
                await AcceptProofRequest();
            }
        });

        public ICommand NavigateBackCommand => new Command(async () => await NavigationService.PopModalAsync());

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
            if (proofAttribute != null) 
            { 
                await LoadProofCredentials(proofAttribute);
                await NavigationService.NavigateToAsync(new SelectAttributeValueViewModel(_userDialogs, _navigationService, this), null, NavigationType.Modal);
                //await NavigationService.NavigateToPopupAsync(true, new SelectAttributeValueViewModel(_userDialogs, _navigationService));
                
            };
        });

        public ICommand SelectProofCredentialCommand => new Command<ProofCredential>(proofCredential =>
        {
            HasNavigationBar = false;
            if (proofCredential != null) BuildRequestedAttributesPredicatesMap(proofCredential.CredentialRecord);
        });

        public ICommand ToggledCommand => new Command<ProofAttribute>(async (proofAttribute) =>
        {
            if (proofAttribute != null) await BuildRevealedAttributeMap(proofAttribute);
        });

        #endregion Bindable Command

        #region Bindable Properties

        private string _alias;
        private bool _areButtonsVisible;
        private IList<ProofAttribute> _attributes;
        private string _errorLabel = "ErrorLabel";
        private string _selectedAttributeName;
        private bool _isFrameVisible;
        private bool _isRevealed;
        private IList<ProofCredential> _proofCredentials;
        private string _proofName;

        private string _id;

        private bool _hasNavigationBar = true;

        private bool _isNew;

        private string _proofState;

        private string _proofVersion;

        private bool _refreshingProofRequest;

        private string _revealDataLabel = "RevealDataLabel";

        public string Alias
        {
            get => _alias;
            set => this.RaiseAndSetIfChanged(ref _alias, value);
        }

        public bool HasNavigationBar
        {
            get => _hasNavigationBar;
            set => _hasNavigationBar = value;
        }

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public bool IsNew
        {
            get => _isNew;
            set => _isNew = value;
        }

        public bool AreButtonsVisible
        {
            get => _areButtonsVisible;
            set => this.RaiseAndSetIfChanged(ref _areButtonsVisible, value);
        }

        public string SelectedAttributeName
        {
            get => _selectedAttributeName;
            set => this.RaiseAndSetIfChanged(ref _selectedAttributeName, value);
        }

        public IList<ProofAttribute> Attributes
        {
            get => _attributes;
            set => this.RaiseAndSetIfChanged(ref _attributes, value);
        }

        public Dictionary<string, RequestedAttribute> RequestedAttributesMap
        {
            get => _requestedAttributesMap;
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

        public IList<ProofCredential> ProofCredentials
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

        public string RevealDataLabel
        {
            get => _revealDataLabel;
            set => this.RaiseAndSetIfChanged(ref _revealDataLabel, value);
        }

        #endregion Bindable Properties
    }
}