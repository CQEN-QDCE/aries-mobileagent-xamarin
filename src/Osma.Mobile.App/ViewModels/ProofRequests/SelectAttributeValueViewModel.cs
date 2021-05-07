using Acr.UserDialogs;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class SelectAttributeValueViewModel : ABaseViewModel
    {
        private readonly ProofRequest _proofRequest;

        private readonly IProofCredentialSelector _proofCredentialSelector;

        private readonly Action<ProofCredential> _selection;

        public SelectAttributeValueViewModel(IUserDialogs userDialogs,
                                             INavigationService navigationService,
                                             IProofCredentialSelector proofCredentialSelector,
                                             ProofRequest proofRequest,
                                             string attributeName,
                                             Action<ProofCredential> selection) : base (AppResources.SelectAttributeValueTitle, userDialogs, navigationService)
        {
            _proofRequest = proofRequest;
            _selection = selection;
            _proofCredentialSelector = proofCredentialSelector;
            SelectedAttributeName = attributeName;
            ProofCredentials = new List<ProofCredential>();
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await FilterCredentialRecords(SelectedAttributeName);

            await base.InitializeAsync(navigationData);
        }

        private async Task FilterCredentialRecords(string attributeName)
        {
            IList<CredentialRecord> credentialRecords = await _proofCredentialSelector.Select(_proofRequest, attributeName);

            IList<ProofCredential> proofCredentials = new List<ProofCredential>();

            foreach (CredentialRecord credentialRecord in credentialRecords)
            {
                foreach (var credentialAttributeValue in credentialRecord.CredentialAttributesValues)
                {
                    if (credentialAttributeValue.Name == attributeName)
                    {
                        var proofCredential = new ProofCredential
                        {
                            AttributeName = attributeName,
                            AttributeValue = credentialAttributeValue.Value.ToString(),
                            IssuedAt = credentialRecord.CreatedAtUtc.HasValue ? credentialRecord.CreatedAtUtc : null,
                            SchemaName = SchemaId.Parse(credentialRecord.SchemaId).Name,
                            CredentialRecord = credentialRecord
                        };
                        proofCredentials.Add(proofCredential);
                    }
                }
            }

            ProofCredentials = proofCredentials;
        }

        #region Bindable Command

        public ICommand AcceptCommand => new Command(async () =>
        {
            if (_selectedProofCredential != null) _selection(_selectedProofCredential);

            await NavigationService.PopModalAsync();
        });

        public ICommand CancelCommand => new Command(async () =>
        {
            await NavigationService.PopModalAsync();
        });

        #endregion Bindable Command

        #region Bindable Properties

        private string _errorLabel = "ErrorLabel";
        private string _availableCount = "0 disponible";
        private string _selectedAttributeName;
        private IList<ProofCredential> _proofCredentials;

        private ProofCredential _selectedProofCredential;

        public string SelectedAttributeName
        {
            get => _selectedAttributeName;
            set => this.RaiseAndSetIfChanged(ref _selectedAttributeName, value);
        }

        public string ErrorLabel
        {
            get => _errorLabel;
            set => this.RaiseAndSetIfChanged(ref _errorLabel, value);
        }

        public ProofCredential SelectedProofCredential
        {
            get => _selectedProofCredential;
            set => this.RaiseAndSetIfChanged(ref _selectedProofCredential, value);
        }

        public IList<ProofCredential> ProofCredentials
        {
            get => _proofCredentials;
            set { this.RaiseAndSetIfChanged(ref _proofCredentials, value); AvailableCount = ProofCredentials.Count < 2 ? ProofCredentials.Count + " disponible" : ProofCredentials.Count + " disponibles"; }
        }

        public string AvailableCount
        {
            get => _availableCount;
            private set => this.RaiseAndSetIfChanged(ref _availableCount, value);
        }

        #endregion Bindable Properties
    }
}