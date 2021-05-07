using Acr.UserDialogs;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Decorators.Service;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Storage;
using Osma.Mobile.App.Assemblers;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.Account;
using Osma.Mobile.App.ViewModels.Connections;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using Osma.Mobile.App.ViewModels.Credentials;
using Osma.Mobile.App.ViewModels.Home;
using Osma.Mobile.App.ViewModels.Messages;
using Osma.Mobile.App.ViewModels.ProofRequests;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels
{
    public class MainViewModel : ABaseViewModel
    {
        private readonly IAgentProvider _agentContextProvider;

        private readonly IEventAggregator _eventAggregator;

        private readonly IProofService _proofService;

        private readonly IWalletRecordService _recordService;

        private readonly IRequestPresentationFiller _requestPresentationFiller;

        private IDisposable _subscription;

        private IProofAssembler _proofAssembler;

        public MainViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            HomeViewModel homeViewModel,
            MessagesViewModel messagesViewModel,
            IAgentProvider agentContextProvider,
            ConnectionsViewModel connectionsViewModel,
            CredentialsViewModel credentialsViewModel,
            IEventAggregator eventAggregator,
            IProofService proofService,
            IProofAssembler proofAssembler,
            IWalletRecordService recordService,
            IRequestPresentationFiller requestPresentationFiller,
            AccountViewModel accountViewModel,
            ProofRequestsViewModel proofRequestsViewModel,
            CreateInvitationViewModel createInvitationViewModel
        ) : base(
                nameof(MainViewModel),
                userDialogs,
                navigationService
        )
        {
            Home = homeViewModel;
            Messages = messagesViewModel;
            Connections = connectionsViewModel;
            Credentials = credentialsViewModel;
            Account = accountViewModel;
            ProofRequests = proofRequestsViewModel;
            CreateInvitation = createInvitationViewModel;
            _eventAggregator = eventAggregator;
            _proofService = proofService;
            _recordService = recordService;
            _agentContextProvider = agentContextProvider;
            _requestPresentationFiller = requestPresentationFiller;
            _proofAssembler = proofAssembler;
            MessagingCenter.Subscribe<object>(this, "ScanInvite", async (sender) =>
            {
                await ScanInvite();
            });

            _subscription = _eventAggregator.GetEventByType<ServiceMessageProcessingEvent>()
            .Where(x => x.MessageType == MessageTypes.PresentProofNames.RequestPresentation || x.MessageType == "https://didcomm.org/present-proof/1.0/request-presentation")
            .Subscribe(async x =>
            {
                await DisplayRequestPresentation(x.RecordId);
            });
        }

        private async Task DisplayRequestPresentation(string recordId)
        {
            IAgentContext context = await _agentContextProvider.GetContextAsync();

            ProofRecord proofRecord = await _proofService.GetAsync(context, recordId);

            ProofRequestViewModel proofRequest = await _proofAssembler.Assemble(proofRecord);

            await _requestPresentationFiller.Fill(proofRequest);

            await NavigationService.NavigateToAsync(proofRequest, null, NavigationType.Modal);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await Home.InitializeAsync(null);
            await Messages.InitializeAsync(null);
            await Connections.InitializeAsync(null);
            await Credentials.InitializeAsync(null);
            await Account.InitializeAsync(null);
            await CreateInvitation.InitializeAsync(null);
            await ProofRequests.InitializeAsync(null);
            await base.InitializeAsync(navigationData);
        }

        private async Task ScanInvite()
        {
            var expectedFormat = ZXing.BarcodeFormat.QR_CODE;

            var opts = new ZXing.Mobile.MobileBarcodeScanningOptions { PossibleFormats = new List<ZXing.BarcodeFormat> { expectedFormat } };

            var context = await _agentContextProvider.GetContextAsync();

            var scanner = new ZXing.Mobile.MobileBarcodeScanner();

            var result = await scanner.Scan(opts);

            if (result == null) return;

            AgentMessage message = await MessageDecoder.ParseMessageAsync(result.Text);

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

        #region Bindable Properties

        private HomeViewModel _home;

        public HomeViewModel Home
        {
            get => _home;
            set => this.RaiseAndSetIfChanged(ref _home, value);
        }

        private MessagesViewModel _messages;

        public MessagesViewModel Messages
        {
            get => _messages;
            set => this.RaiseAndSetIfChanged(ref _messages, value);
        }

        private ConnectionsViewModel _connections;

        public ConnectionsViewModel Connections
        {
            get => _connections;
            set => this.RaiseAndSetIfChanged(ref _connections, value);
        }

        private CredentialsViewModel _credentials;

        public CredentialsViewModel Credentials
        {
            get => _credentials;
            set => this.RaiseAndSetIfChanged(ref _credentials, value);
        }

        private AccountViewModel _account;

        public AccountViewModel Account
        {
            get => _account;
            set => this.RaiseAndSetIfChanged(ref _account, value);
        }

        private ProofRequestsViewModel _proofRequests;

        public ProofRequestsViewModel ProofRequests
        {
            get => _proofRequests;
            set => this.RaiseAndSetIfChanged(ref _proofRequests, value);
        }

        private CreateInvitationViewModel _createInvitation;

        public CreateInvitationViewModel CreateInvitation
        {
            get => _createInvitation;
            set => this.RaiseAndSetIfChanged(ref _createInvitation, value);
        }

        #endregion Bindable Properties
    }
}