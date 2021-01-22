using Hyperledger.Aries.Features.PresentProof;
using System;

namespace Osma.Mobile.App.Utilities
{
    public class ProofStateTranslator
    {
        public static string Translate(ProofState value)
        {
            switch(value)
            {
                case ProofState.Proposed:
                    return AppResources.ProofStateProposed;
                case ProofState.Requested:
                    return AppResources.ProofStateRequested;
                case ProofState.Accepted:
                    return AppResources.ProofStateAccepted;
                case ProofState.Rejected:
                    return AppResources.ProofStateRejected;
            }
            throw new Exception();
        }
    }
}
