using Microsoft.VisualStudio.TestTools.UnitTesting;
using Osma.Mobile.App.ViewModels.ProofRequests;
using FluentAssertions;
using NSubstitute;
using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using Hyperledger.Aries.Features.PresentProof;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using Hyperledger.Aries.Features.IssueCredential;

namespace Osma.Mobile.App.UnitTests.ViewModels.ProofRequests
{
    [TestClass]
    public class SelectAttributeValueViewModelUnitTests
    {
        private IUserDialogs _userDialogsMock;
        private INavigationService _navigationServiceMock;
        private IProofCredentialSelector _proofCredentialSelectorMock;
        
        private ProofCredential _selectedProofCredential;

        [TestMethod]
        public async Task GivenSelectAttributeValueViewModel_WhenProofCredentialIsSelected_ThenAcceptCommandReturnIt()
        {
            SelectAttributeValueViewModel subject = await GetSubjectAsync();
            subject.SelectedProofCredential = new ProofCredential();
            ICommand command = subject.AcceptCommand;
            command.Execute(null);
            _selectedProofCredential.Should().NotBeNull();
            _selectedProofCredential.Should().BeEquivalentTo(subject.SelectedProofCredential);
        }

        [TestMethod]
        public async Task GivenSelectAttributeValueViewModel_WhenProofRequestHaveNoRestriction_ThenProofCredentialsContainsAllCredentialsHavingSelectedAttributeName()
        {
            SelectAttributeValueViewModel subject = await GetSubjectAsync();
            subject.ProofCredentials.Should().HaveCount(2);
            subject.ProofCredentials[0].AttributeName.Should().Be("First Name");
            subject.ProofCredentials[0].AttributeValue.Should().Be("David");
            subject.ProofCredentials[0].SchemaName.Should().Be("Birth Certificate");
            subject.ProofCredentials[1].AttributeName.Should().Be("First Name");
            subject.ProofCredentials[1].AttributeValue.Should().Be("David");
            subject.ProofCredentials[1].SchemaName.Should().Be("Driver License");
        }

        private async Task<SelectAttributeValueViewModel> GetSubjectAsync()
        {
            _userDialogsMock = Substitute.For<IUserDialogs>();
            _navigationServiceMock = Substitute.For<INavigationService>();
            _proofCredentialSelectorMock = Substitute.For<IProofCredentialSelector>();
            _proofCredentialSelectorMock.Select(Arg.Any<ProofRequest>(), Arg.Any<string>()).Returns(new List<CredentialRecord> { FakeData.Credentials.BirthCertificate, FakeData.Credentials.DriverLicense, FakeData.Credentials.ClubMemberCard });
            SelectAttributeValueViewModel subject = new SelectAttributeValueViewModel(_userDialogsMock,
                                                                                      _navigationServiceMock,
                                                                                      _proofCredentialSelectorMock,
                                                                                      GetProofRequest(),
                                                                                      "First Name",
                                                                                      (proofCredential) => _selectedProofCredential = proofCredential);
            await subject.InitializeAsync(null);
            return subject;
        }

        private ProofRequest GetProofRequest()
        {
            return new ProofRequest 
            {
                Name = "Proof1",
                Version = "1.0",
                Nonce = "01265389723564",
                NonRevoked = new RevocationInterval { From = 0, To = 1 }
            };
        }
    }
}
