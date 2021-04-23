using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Osma.Mobile.App.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osma.Mobile.App.Services
{
    public class ProofCredentialSelector : IProofCredentialSelector
    {
        private readonly IAgentProvider _agentContextProvider;

        private readonly ICredentialService _credentialService;

        public ProofCredentialSelector(IAgentProvider agentContextProvider, ICredentialService credentialService)
        {
            _agentContextProvider = agentContextProvider;
            _credentialService = credentialService;
        }

        private List<AttributeFilter> GetRestrictions(ProofRequest proofRequest, string attributeName)
        {
            List<AttributeFilter> restrictions = new List<AttributeFilter>();

            foreach (ProofAttributeInfo proofAttributeInfo in proofRequest.RequestedAttributes.Values)
            {
                if (proofAttributeInfo.Name == attributeName || (proofAttributeInfo.Names != null && proofAttributeInfo.Names.Contains(attributeName)))
                {
                    restrictions = restrictions.Concat(proofAttributeInfo.Restrictions).ToList();
                }
            }

            foreach (ProofAttributeInfo proofAttributeInfo in proofRequest.RequestedPredicates.Values)
            {
                if (proofAttributeInfo.Name == attributeName || proofAttributeInfo.Names.Contains(attributeName))
                {
                    restrictions = restrictions.Concat(proofAttributeInfo.Restrictions).ToList();
                }
            }

            return restrictions;
        }

        public async Task<IList<CredentialRecord>> Select(ProofRequest proofRequest, string attributeName)
        {
            IAgentContext context = await _agentContextProvider.GetContextAsync();

            List<CredentialRecord> credentialRecords = await _credentialService.ListAsync(context);

            credentialRecords = credentialRecords.Where(cr => cr.State == CredentialState.Issued).ToList();

            List<AttributeFilter> restrictions = GetRestrictions(proofRequest, attributeName);

            ProofCredentialFilter proofCredentialFilter = new ProofCredentialFilter(restrictions, attributeName);

            return proofCredentialFilter.Filter(credentialRecords);
        }
    }
}
