using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Osma.Mobile.App.Services;
using FluentAssertions;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Features.IssueCredential;

namespace Osma.Mobile.App.UnitTests.Services
{
    [TestClass]
    public class ProofCredentialFilterUnitTests
    {
        [TestMethod]
        public void GivenNewInstance_WhenArgumentsAreNull_ThenThrowException()
        {
            Constructor(() => new ProofCredentialFilter(null, "")).Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("restrictions");
            Constructor(() => new ProofCredentialFilter(new List<AttributeFilter>(), null)).Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("attributeName");
        }

        [TestMethod]
        public void GivenProofRequest_WhenAttributeHaveCredentialDefinitionIdRestrictions_ThenShouldReturnOnlyCorrespondingCredentialRecords()
        {
            List<AttributeFilter> restrictions = new List<AttributeFilter> 
            {
                new AttributeFilter { CredentialDefinitionId = "XpHQgyg7Dd4WerGtGCL3KN:3:CL:1614:vc-authn-oidc1" },
                new AttributeFilter { CredentialDefinitionId = "XpHQgyg7Dd4WerGtGCL3KN:3:CL:1614:vc-authn-oidc2" }
            };

            ProofCredentialFilter proofCredentialFilter = new ProofCredentialFilter(restrictions, "Attr1");

            IList<CredentialRecord> results = proofCredentialFilter.Filter(GetCredentialRecords());

            results.Should().HaveCount(2);
        }

        static Action Constructor<T>(Func<T> func)
        {
            return () => func();
        }

        private IList<CredentialRecord> GetCredentialRecords()
        {
            IList<CredentialRecord> credentialRecords = new List<CredentialRecord>
            {
                new CredentialRecord
                {
                    ConnectionId = "f8a9fb89-38ef-4317-811a-ab48196ca888",
                    CredentialAttributesValues = new List<CredentialPreviewAttribute> { new CredentialPreviewAttribute { Name = "Attr1", Value = "Val1" } },
                    CredentialDefinitionId = "XpHQgyg7Dd4WerGtGCL3KN:3:CL:1614:vc-authn-oidc1",
                    CredentialId = "ac6c10c9-7b8c-4a3b-837e-c8ba8c7a9d81",
                    SchemaId = "XpHQgyg7Dd4WerGtGCL3KN:2:CQEN_AUTHENTICATION:1.0",
                    State = CredentialState.Issued
                },
                new CredentialRecord
                {
                    ConnectionId = "f8a9fb89-38ef-4317-811a-ab48196ca888",
                    CredentialAttributesValues = new List<CredentialPreviewAttribute> { new CredentialPreviewAttribute { Name = "Attr1", Value = "Val1" } },
                    CredentialDefinitionId = "XpHQgyg7Dd4WerGtGCL3KN:3:CL:1614:vc-authn-oidc2",
                    CredentialId = "ac6c10c9-7b8c-4a3b-837e-c8ba8c7a9d81",
                    SchemaId = "XpHQgyg7Dd4WerGtGCL3KN:2:CQEN_AUTHENTICATION:1.0",
                    State = CredentialState.Issued
                }
            };

            return credentialRecords;
        }
    }
}
