using Hyperledger.Aries.Features.PresentProof;
using Osma.Mobile.App.ViewModels.ProofRequests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osma.Mobile.App.Assemblers
{
    public interface IProofAssembler
    {
        Task<ProofRequestViewModel> Assemble(ProofRecord proofRecord);

        Task<IList<ProofRequestViewModel>> AssembleMany(IList<ProofRecord> proofRecords);
    }
}