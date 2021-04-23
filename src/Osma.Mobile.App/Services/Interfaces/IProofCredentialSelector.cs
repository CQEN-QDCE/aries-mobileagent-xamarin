using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Osma.Mobile.App.Services.Interfaces
{
    public interface IProofCredentialSelector
    {
        Task<IList<CredentialRecord>> Select(ProofRequest proofRequest, string proofAttributeName);
    }
}
