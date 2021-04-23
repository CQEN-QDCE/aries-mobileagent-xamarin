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

            ProofRequestViewModel proofRequest2 = _scope.Resolve<ProofRequestViewModel>(new NamedParameter("proof", proofRecord));

            proofRequest2.Id = proofRecord.Id;

            //proof.IsNew = proofRecord.State == ProofState.Requested && string.IsNullOrEmpty(proofRecord.GetTag("IsNew"));

            if (proofRecord.CreatedAtUtc.HasValue)
            {
                //proof.Alias = proofRecord.CreatedAtUtc.Value.ToLocalTime().ToString();
            }

            if (proofRecord.ProofJson != null)
            {
                var partialProof = JsonConvert.DeserializeObject<PartialProof>(proofRecord.ProofJson);
                var proofRequest = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);
                proofRequest2.Attributes.Clear();
                foreach (var revealedAttributeKey in partialProof.RequestedProof.RevealedAttributes.Keys)
                {
                    var proofAttribute = new ViewModels.ProofRequests.ProofAttributeViewModel
                    {
                        Name = proofRequest.RequestedAttributes[revealedAttributeKey].Name, // TODO: Que faire pour gérer l'attribut Names?
                        IsPredicate = false,
                        IsRevealed = true,
                        Type = "Text",
                        Value = partialProof.RequestedProof.RevealedAttributes[revealedAttributeKey].Raw
                    };
                    proofRequest2.Attributes.Add(proofAttribute);
                }
            } 
            else
            {
                ProofRequest proofRequest = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);
                proofRequest2.Attributes.Clear();
                foreach(KeyValuePair<string, ProofAttributeInfo> requestedAttribute in proofRequest.RequestedAttributes)
                {
                    proofRequest2.Attributes.Add(new ProofAttributeViewModel
                    {
                        Id = requestedAttribute.Key,
                        Name = requestedAttribute.Value.Name // TODO: Que faire pour gérer l'attribut Names?
                    });
                }
                foreach (KeyValuePair<string, ProofPredicateInfo> requestedAttribute in proofRequest.RequestedPredicates)
                {
                    proofRequest2.Attributes.Add(new ProofAttributeViewModel
                    {
                        Id = requestedAttribute.Key,
                        Name = requestedAttribute.Value.Name, // TODO: Que faire pour gérer l'attribut Names?
                        IsPredicate = true
                    });
                }
            }

            proofRequest2.State = ProofStateTranslator.Translate(proofRecord.State);

            return proofRequest2;
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