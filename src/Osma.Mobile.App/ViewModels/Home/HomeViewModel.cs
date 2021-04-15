using Acr.UserDialogs;
using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Osma.Mobile.App.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReactiveUI;
using Hyperledger.Aries.Features.PresentProof;
using Xamarin.Forms;
using System.Windows.Input;
using System.Reactive.Linq;
using Osma.Mobile.App.Events;
using System.Linq;
using Osma.Mobile.App.Utilities;
using Newtonsoft.Json;
using Osma.Mobile.App.Assemblers;
using Osma.Mobile.App.ViewModels.Credentials;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.ViewModels.ProofRequests;
using Osma.Mobile.App.ViewModels.Connections;
using Osma.Mobile.App.ViewModels.Account;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System.IO;
using Hyperledger.Aries.Decorators.Service;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Storage;

namespace Osma.Mobile.App.ViewModels.Home
{
    public class HomeViewModel : ABaseViewModel
    {
        private readonly IConnectionService _connectionService;
        private readonly ICredentialService _credentialService;
        private readonly ICredentialAssembler _credentialAssembler;
        private readonly IProofAssembler _proofAssembler;
        private readonly IConnectionAssembler _connectionAssembler;
        private readonly IProofService _proofService;
        private readonly IWalletRecordService _recordService;
        private readonly IRequestPresentationFiller _requestPresentationFiller;
        private readonly IAgentProvider _agentContextProvider;
        private readonly ILifetimeScope _scope;
        private readonly IEventAggregator _eventAggregator;
        private IEnumerable<Notification> _notifications;
        private byte[] _photo;
        private readonly INavigationService _navigationService;
        private readonly IUserDialogs _userDialogs;
        private bool _refreshingNotifications;

        public HomeViewModel(IUserDialogs userDialogs,
                             INavigationService navigationService,
                             IConnectionService connectionService,
                             ICredentialService credentialService,
                             ICredentialAssembler credentialAssembler,
                             IProofAssembler proofAssembler,
                             IRequestPresentationFiller requestPresentationFiller,
                             IConnectionAssembler connectionAssembler,
                             IProofService proofService,
                             IWalletRecordService recordService,
                             IAgentProvider agentContextProvider,
                             IEventAggregator eventAggregator,
                             ILifetimeScope scope) : base(AppResources.HomeTabTitle, userDialogs, navigationService)
        {
            _connectionService = connectionService;
            _credentialService = credentialService;
            _credentialAssembler = credentialAssembler;
            _connectionAssembler = connectionAssembler;
            _proofAssembler = proofAssembler;
            _proofService = proofService;
            _requestPresentationFiller = requestPresentationFiller;
            _recordService = recordService;
            _agentContextProvider = agentContextProvider;
            _eventAggregator = eventAggregator;
            _scope = scope;
            _userDialogs = userDialogs;
            _navigationService = navigationService;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshNotifications();

            //_eventAggregator.GetEventByType<ApplicationEvent>()
            //                .Where(_ => _.Type == ApplicationEventType.ConnectionsUpdated)
            //                .Subscribe(async _ => await RefreshNotifications());

            await base.InitializeAsync(navigationData);
        }

        public byte[] Photo
        {
            get => _photo;
            set => this.RaiseAndSetIfChanged(ref _photo, value);
        }

        public IEnumerable<Notification> Notifications
        {
            get => _notifications;
            set => this.RaiseAndSetIfChanged(ref _notifications, value);
        }

        public bool RefreshingNotifications
        {
            get => _refreshingNotifications;
            set => this.RaiseAndSetIfChanged(ref _refreshingNotifications, value);
        }

        private async Task SelectPhoto()
        {
            await CrossMedia.Current.Initialize();
            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                int bla = 1;
            }
            var mediaOptions = new PickMediaOptions()
            {
                PhotoSize = PhotoSize.Full
            };
            var selectedImageFile = await CrossMedia.Current.PickPhotoAsync(mediaOptions);
            if (selectedImageFile == null)
            {
                await DialogService.AlertAsync("Test", "HaHa");
            }
            MemoryStream mem = new MemoryStream();
            selectedImageFile.GetStream().CopyTo(mem);
            Photo = mem.ToArray();
            //selectedImage.Source = ImageSource.FromStream(() => selectedImageFile.GetStream());
        }

        public async Task RefreshNotifications()
        {
            RefreshingNotifications = true;

            IAgentContext context = await _agentContextProvider.GetContextAsync();

            List<ConnectionRecord> connectionRecords = await _connectionService.ListAsync(context);

            List<CredentialRecord> credentialsRecords = await _credentialService.ListAsync(context);

            List<ProofRecord> proofRecords = await _proofService.ListAsync(context);

            IList<Notification> notificationViewModels = new List<Notification>();

            foreach (ConnectionRecord connectionRecord in connectionRecords)
            {
                Notification notificationViewModel = new Notification();

                notificationViewModel.RecordId = connectionRecord.Id;

                notificationViewModel.AgentName = connectionRecord.Alias != null ? connectionRecord.Alias.Name : string.Empty;

                notificationViewModel.Type = NotificationType.Connection;

                switch (connectionRecord.State)
                {
                    case ConnectionState.Invited:
                        notificationViewModel.State = NotificationState.Invited;
                        break;
                    case ConnectionState.Negotiating:
                        notificationViewModel.State = NotificationState.Negotiating;
                        break;
                    case ConnectionState.Connected:
                        notificationViewModel.State = NotificationState.Connected;
                        break;
                }

                notificationViewModel.DateTime = connectionRecord.CreatedAtUtc;

                notificationViewModels.Add(notificationViewModel);
            }

            foreach (CredentialRecord credentialRecord in credentialsRecords)
            {
                Notification notificationViewModel = new Notification();

                notificationViewModel.RecordId = credentialRecord.Id;

                CredentialDefinitionId credentialDefinitionId = CredentialDefinitionId.Parse(credentialRecord.CredentialDefinitionId);

                notificationViewModel.RecordName = credentialDefinitionId.Tag;

                if (credentialRecord.ConnectionId != null)
                {
                    ConnectionRecord connectionRecord = await _connectionService.GetAsync(context, credentialRecord.ConnectionId);

                    notificationViewModel.AgentName = connectionRecord.Alias != null ? connectionRecord.Alias.Name : string.Empty;
                }

                notificationViewModel.Type = NotificationType.Credential;

                switch (credentialRecord.State)
                {
                    case CredentialState.Issued:
                        notificationViewModel.State = NotificationState.Issued;
                        break;
                    case CredentialState.Offered:
                        notificationViewModel.State = NotificationState.Offered;
                        break;
                    case CredentialState.Rejected:
                            notificationViewModel.State = NotificationState.Rejected;
                        break;
                    case CredentialState.Requested:
                        notificationViewModel.State = NotificationState.Requested;
                        break;
                    case CredentialState.Revoked:
                        notificationViewModel.State = NotificationState.Revoked;
                        break;
                }
                notificationViewModel.DateTime = credentialRecord.CreatedAtUtc;

                notificationViewModels.Add(notificationViewModel);
            }

            foreach (var proofRecord in proofRecords)
            {
                Notification notificationViewModel = new Notification();

                notificationViewModel.RecordId = proofRecord.Id;

                ProofRequest proofRequest = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);

                notificationViewModel.RecordName = proofRequest.Name;

                if (proofRecord.ConnectionId != null)
                {
                    ConnectionRecord connectionRecord = await _connectionService.GetAsync(context, proofRecord.ConnectionId);

                    notificationViewModel.AgentName = connectionRecord.Alias != null ? connectionRecord.Alias.Name : string.Empty;
                }

                notificationViewModel.Type = NotificationType.ProofRequest;

                switch (proofRecord.State)
                {
                    case ProofState.Proposed:
                        notificationViewModel.State = NotificationState.Proposed;
                        break;
                    case ProofState.Requested:
                        notificationViewModel.State = NotificationState.Requested;
                        break;
                    case ProofState.Accepted:
                        notificationViewModel.State = NotificationState.Accepted;
                        break;
                    case ProofState.Rejected:
                        notificationViewModel.State = NotificationState.Rejected;
                        break;
                }

                notificationViewModel.DateTime = proofRecord.CreatedAtUtc;

                notificationViewModels.Add(notificationViewModel);
            }

            Notifications = notificationViewModels.OrderByDescending(pr => pr.DateTime).ToList();

            RefreshingNotifications = false;
        }

        private async Task ConfigureSettings()
        {
            await NavigationService.NavigateToAsync<AccountViewModel>();
        }

        private async Task DecodeQrCodeFromUrl()
        {
            PromptResult pResult = await UserDialogs.Instance.PromptAsync(new PromptConfig
            {
                InputType = InputType.Url,
                OkText = "Decode",
                CancelText = "Cancel",
                Title = "Decode QR code from URL",
            });
            if (pResult.Ok && !string.IsNullOrWhiteSpace(pResult.Text))
            {
                await ScanInvite(pResult.Text);
            }
        }

        public async Task ScanInvite(string resultText)
        {
            var context = await _agentContextProvider.GetContextAsync();

            AgentMessage message = await MessageDecoder.ParseMessageAsync(resultText);

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

        public async Task SelectConnection(Notification notification)
        {
            IAgentContext context = await _agentContextProvider.GetContextAsync();

            switch(notification.Type)
            {
                case NotificationType.Connection:
                    ConnectionRecord connectionRecord = await _connectionService.GetAsync(context, notification.RecordId);
                    ConnectionViewModel connectionViewModel = _connectionAssembler.Assemble(connectionRecord);
                    await NavigationService.NavigateToAsync(connectionViewModel, NavigationType.Modal);
                    break;
                case NotificationType.Credential:
                    CredentialRecord credentialRecord = await _credentialService.GetAsync(context, notification.RecordId);
                    CredentialViewModel credentialViewModel = await _credentialAssembler.Assemble(credentialRecord);
                    await NavigationService.NavigateToAsync(credentialViewModel, NavigationType.Modal);
                    break;
                case NotificationType.ProofRequest:
                    ProofRecord proofRecord = await _proofService.GetAsync(context, notification.RecordId);
                    ProofRequestViewModel proofRequest = await _proofAssembler.Assemble(proofRecord);
                    await _requestPresentationFiller.Fill(proofRequest);
                    await NavigationService.NavigateToAsync(proofRequest, NavigationType.Modal);
                    break;
            }
        }

        public ICommand SelectPhotoCommand => new Command(async () => await SelectPhoto());

        public ICommand RefreshCommand => new Command(async () => await RefreshNotifications());

        public ICommand SelectNotificationCommand => new Command<Notification>(async (notification) =>
        {
            if (notification != null) await SelectConnection(notification);
        });

        public ICommand ConfigureSettingsCommand => new Command(async () => await ConfigureSettings());

        public ICommand DecodeQrCodeFromUrlCommand => new Command(async () => await DecodeQrCodeFromUrl());

    }
}
