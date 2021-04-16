using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json;
using Osma.Mobile.App.ViewModels.ProofRequests;
using System;
using System.Threading.Tasks;

namespace Osma.Mobile.App.Utilities
{
    public class RequestPresentationFiller : IRequestPresentationFiller
    {
        private readonly IAgentProvider _agentContextProvider;

        private readonly IWalletRecordService _recordService;

        private readonly ICredentialService _credentialService;

        private readonly ILifetimeScope _scope;

        public RequestPresentationFiller(IAgentProvider agentContextProvider, IWalletRecordService recordService, ICredentialService credentialService, ILifetimeScope scope)
        {
            _agentContextProvider = agentContextProvider;
            _recordService = recordService;
            _credentialService = credentialService;
            _scope = scope;
        }

        public async Task Fill(ProofRequestViewModel proof)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var proofRecord = await _recordService.GetAsync<ProofRecord>(context.Wallet, proof.Id);

            var holderProofObject = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);
            //var credentials = await _proofService.ListCredentialsForProofRequestAsync(context, holderProofObject, "username");

            if (proofRecord.State == ProofState.Requested)
            {
                var proofRequest = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson);
                foreach (var requestedAttribute3 in proofRequest.RequestedAttributes)
                {
                    var requestedAttribute = requestedAttribute3.Value;
                    if (requestedAttribute.Restrictions.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(requestedAttribute.Restrictions[0].CredentialDefinitionId))
                        {
                            foreach (var credentialRecord in await _credentialService.ListIssuedCredentialsAsync(context))
                            {
                                if (credentialRecord.CredentialDefinitionId == requestedAttribute.Restrictions[0].CredentialDefinitionId)
                                {
                                    var requestedAttribute2 = new RequestedAttribute
                                    {
                                        CredentialId = credentialRecord.CredentialId,
                                        Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds()
                                    };

                                    //proof.BuildRequestedAttributesPredicatesMap(credentialRecord);
                                    foreach (var pa in proof.Attributes)
                                    {
                                        if (pa.Name == requestedAttribute.Name)
                                        {
                                            foreach (var cav in credentialRecord.CredentialAttributesValues)
                                            {
                                                if (cav.Name == requestedAttribute.Name)
                                                {
                                                    pa.Value = cav.Value.ToString();
                                                    if (proof.RequestedAttributesMap.ContainsKey(requestedAttribute3.Key))
                                                    {
                                                        if (proof.RequestedAttributesMap[requestedAttribute3.Key]?.CredentialId !=
                                                            requestedAttribute2.CredentialId)
                                                            proof.RequestedAttributesMap[requestedAttribute3.Key] = requestedAttribute2;
                                                    }
                                                    else
                                                    {
                                                        proof.RequestedAttributesMap.Add(requestedAttribute3.Key, requestedAttribute2);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            int bla = 1;
        }
    }
}