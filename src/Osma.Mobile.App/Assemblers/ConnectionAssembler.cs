using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.ProofRequests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osma.Mobile.App.Assemblers
{
    public class ProofAssembler : IProofAssembler
    {
        private readonly IAgentProvider _agentContextProvider;

        private readonly IConnectionService _connectionService;

        private readonly ILifetimeScope _scope;

        public ProofAssembler(IAgentProvider agentContextProvider, IConnectionService connectionService, ILifetimeScope scope)
        {
            _agentContextProvider = agentContextProvider;
            _connectionService = connectionService;
            _scope = scope;
        }

        public async Task<ProofRequestViewModel> Assemble(ProofRecord proofRecord)
        {
            if (proofRecord == null) return null;

            IAgentContext context = await _agentContextProvider.GetContextAsync();

            ProofRequestViewModel proof = _scope.Resolve<ProofRequestViewModel>(new NamedParameter("proof", proofRecord));

            proof.Id = proofRecord.Id;

            proof.IsNew = proofRecord.State == ProofState.Requested && string.IsNullOrEmpty(proofRecord.GetTag("IsNew"));

            if (proofRecord.CreatedAtUtc.HasValue)
            {
                proof.Alias = proofRecord.CreatedAtUtc.Value.ToLocalTime().ToString();
            }

            if (proofRecord.ProofJson != null)
            {
                var partialProof = JsonConvert.DeserializeObject<PartialProof>(proofRecord.ProofJson);
                var proofRequest = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);
                proof.Attributes.Clear();
                foreach (var revealedAttributeKey in partialProof.RequestedProof.RevealedAttributes.Keys)
                {
                    var proofAttribute = new ViewModels.ProofRequests.ProofAttribute
                    {
                        Name = proofRequest.RequestedAttributes[revealedAttributeKey].Name, // TODO: Que faire pour gérer l'attribut Names?
                        IsNotPredicate = true,
                        IsRevealed = true,
                        Type = "Text",
                        Value = partialProof.RequestedProof.RevealedAttributes[revealedAttributeKey].Raw
                    };
                    proof.Attributes.Add(proofAttribute);
                }
            }

            proof.ProofState = ProofStateTranslator.Translate(proofRecord.State);

            return proof;
        }

        public async Task<IList<ProofRequestViewModel>> AssembleMany(IList<ProofRecord> proofRecords)
        {
            if (proofRecords == null) return null;

            IList<ProofRequestViewModel> proofs = new List<ProofRequestViewModel>();

            foreach (ProofRecord proofRecord in proofRecords.OrderByDescending(pr => pr.CreatedAtUtc))
            {
                proofs.Add(await Assemble(proofRecord));
            }

            return proofs;
        }
    }
}