using Acr.UserDialogs;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.Credentials
{
    public class CredentialViewModel : ABaseViewModel
    {
        private readonly CredentialRecord _credential;
        private readonly IAgentProvider _agentContextProvider;
        private readonly ICredentialService _credentialService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMessageService _messageService;
        private readonly IConnectionService _connectionService;
        private readonly IPoolConfigurator _poolConfigurator;

        public CredentialViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IAgentProvider agentContextProvider,
            ICredentialService credentialService,
            IEventAggregator eventAggregator,
            IMessageService messageService,
            IPoolConfigurator poolConfigurator,
            IConnectionService connectionService,
        CredentialRecord credential
        ) : base(
            "Credential Details",
            userDialogs,
            navigationService
        )
        {
            _credential = credential;
            _agentContextProvider = agentContextProvider;
            _credentialService = credentialService;
            _eventAggregator = eventAggregator;
            _messageService = messageService;
            _connectionService = connectionService;
            _isNew = IsCredentialNew(_credential);
            _poolConfigurator = poolConfigurator;

            if (credential.State == CredentialState.Offered)
            {
                IsPending = true;
                AreButtonsVisible = true;
            }

            if (credential.State == CredentialState.Issued)
            {
                IsIssued = true;
                AreButtonsVisible = false;
                if (credential.CredentialAttributesValues != null)
                {
                    Attributes = credential.CredentialAttributesValues
                        .Select(p =>
                            new CredentialAttribute()
                            {
                                Name = p.Name,
                                Value = p.Value?.ToString(),
                                Type = p.Value != null && (p.Value.ToString().StartsWith("data:image/jpeg;base64") || p.Value.ToString().StartsWith("data:image/png;base64")) ? "Image" : "Text",
                                Image = p.Value != null && (p.Value.ToString().StartsWith("data:image/jpeg;base64") || p.Value.ToString().StartsWith("data:image/png;base64")) ? ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(p.Value.ToString().Replace("data:image/jpeg;base64,", "").Replace("data:image/png;base64,", "")))) : null
                            })
                        .ToList();
                }
            }
        }

        private async Task AcceptCredentialOffer(CredentialRecord credentialRecord)
        {
            if (credentialRecord.State != CredentialState.Offered)
            {
                await DialogService.AlertAsync(string.Format("res-CredentialStateShouldBe", CredentialState.Offered));
                await NavigationService.PopModalAsync();
                return;
            }

            await _poolConfigurator.ConfigurePoolsAsync();

            var context = await _agentContextProvider.GetContextAsync();

            var (msg, rec) = await _credentialService.CreateRequestAsync(context, credentialRecord.Id);

            var connectionRecord = await _connectionService.GetAsync(context, credentialRecord.ConnectionId);

            if (connectionRecord == null)
            {
                //await _messageService.SendAsync(context.Wallet, msg, conectionRecord.TheirVk ?? rec.GetTag("InvitationKey") ?? throw new InvalidOperationException("Cannot locate recipient Key"), conectionRecord.Endpoint.Uri,
                //    conectionRecord.Endpoint?.Verkey == null ? null : conectionRecord.Endpoint.Verkey, conectionRecord.MyVk);
                await DialogService.AlertAsync("Connection-less credentials not supported.");
                return;
            }
            else
            {
                await _messageService.SendAsync(context.Wallet, msg, connectionRecord);
            }

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.CredentialUpdated });

            await NavigationService.PopModalAsync();
        }

        private async Task RejectCredentialOffer()
        {
            if (_credential.State != CredentialState.Offered)
            {
                await DialogService.AlertAsync(string.Format("res-CredentialStateShouldBe", CredentialState.Offered));
                await NavigationService.PopModalAsync();
                return;
            }

            var context = await _agentContextProvider.GetContextAsync();

            await _credentialService.RejectOfferAsync(context, _credential.Id);

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.CredentialUpdated });

            await NavigationService.PopModalAsync();
        }

        private bool IsCredentialNew(CredentialRecord credential)
        {
            return credential.State == CredentialState.Offered;
        }

        #region Bindable Command

        public ICommand NavigateBackCommand => new Command(async () =>
        {
            await NavigationService.PopModalAsync();
        });

        public ICommand AcceptCredentialOfferCommand => new Command(async () =>
        {
            //if (await isAuthenticatedAsync(ApplicationEventType.PassCodeAuthorisedCredentialAccept))
            //{
            await AcceptCredentialOffer(_credential);
            //}
        });

        public ICommand RejectCredentialOfferCommand => new Command(async () =>
        {
            //if (await isAuthenticatedAsync(ApplicationEventType.PassCodeAuthorisedCredentialReject))
            //{
            await RejectCredentialOffer();
            //}
        });

        public ICommand DeleteCommand => new Command(async () =>
        {
            PromptResult result = await DialogService.PromptAsync(AppResources.DeleteCredentialQuestion, AppResources.DeleteCredentialTitle, AppResources.OkLabel, AppResources.CancelLabel);

            if (result.Ok)
            {
                var context = await _agentContextProvider.GetContextAsync();

                await _connectionService.DeleteAsync(context, _credential.Id);

                _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.CredentialUpdated });

                await NavigationService.NavigateBackAsync();
            }
        });

        #endregion Bindable Command

        #region Bindable Properties

        private DateTime? _issuedAt;

        public DateTime? IssuedAt
        {
            get => _issuedAt;
            set => this.RaiseAndSetIfChanged(ref _issuedAt, value);
        }

        private string _credentialName;

        public string CredentialName
        {
            get => _credentialName;
            set => this.RaiseAndSetIfChanged(ref _credentialName, value);
        }

        private string _credentialType;

        public string CredentialType
        {
            get => _credentialType;
            set => this.RaiseAndSetIfChanged(ref _credentialType, value);
        }

        private bool _areButtonsVisible;

        public bool AreButtonsVisible
        {
            get => _areButtonsVisible;
            set => this.RaiseAndSetIfChanged(ref _areButtonsVisible, value);
        }

        private bool _IsIssued;

        public bool IsIssued
        {
            get => _IsIssued;
            set => this.RaiseAndSetIfChanged(ref _IsIssued, value);
        }

        private bool _IsPending;

        public bool IsPending
        {
            get => _IsPending;
            set => this.RaiseAndSetIfChanged(ref _IsPending, value);
        }

        private string _credentialImageUrl;

        public string CredentialImageUrl
        {
            get => _credentialImageUrl;
            set => this.RaiseAndSetIfChanged(ref _credentialImageUrl, value);
        }

        private string _credentialSubtitle;

        public string CredentialSubtitle
        {
            get => _credentialSubtitle;
            set => this.RaiseAndSetIfChanged(ref _credentialSubtitle, value);
        }

        private bool _isNew;

        public bool IsNew
        {
            get => _isNew;
            set => this.RaiseAndSetIfChanged(ref _isNew, value);
        }

        private string _qRImageUrl;

        public string QRImageUrl
        {
            get => _qRImageUrl;
            set => this.RaiseAndSetIfChanged(ref _qRImageUrl, value);
        }

        private IEnumerable<CredentialAttribute> _attributes;

        public IEnumerable<CredentialAttribute> Attributes
        {
            get => _attributes;
            set => this.RaiseAndSetIfChanged(ref _attributes, value);
        }

        private string _attributeCount;

        public string AttributeCount
        {
            get => _attributes == null ? "" : _attributes.ToList().Count.ToString();
            set => this.RaiseAndSetIfChanged(ref _attributeCount, value);
        }

        public IEnumerable<CredentialViewModel> Me
        {
            get => new List<CredentialViewModel> { this }.AsEnumerable();
        }

        #endregion Bindable Properties
    }
}