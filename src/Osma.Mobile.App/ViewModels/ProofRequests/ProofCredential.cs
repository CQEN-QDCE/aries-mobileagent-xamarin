using Hyperledger.Aries.Features.IssueCredential;
using System;

namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class ProofCredential
    {
        public string SchemaName { get; set; }

        public string Issuer { get; set; }

        public DateTime? IssuedAt { get; set; }

        public string AttributeName { get; set; }

        public string AttributeValue { get; set; }

        public bool Selected { get; set; }

        public CredentialRecord CredentialRecord { get; set; }
    }
}