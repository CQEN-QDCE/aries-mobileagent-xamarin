using Autofac;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.Credentials;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osma.Mobile.App.Assemblers
{
    public class CredentialAssembler : ICredentialAssembler
    {
        private readonly IAgentProvider _agentContextProvider;

        private readonly IConnectionService _connectionService;

        private readonly ILifetimeScope _scope;

        public CredentialAssembler(IAgentProvider agentContextProvider, IConnectionService connectionService, ILifetimeScope scope)
        {
            _agentContextProvider = agentContextProvider;
            _connectionService = connectionService;
            _scope = scope;
        }

        public async Task<CredentialViewModel> Assemble(CredentialRecord credentialRecord)
        {
            if (credentialRecord ==  null) return null;

            var context = await _agentContextProvider.GetContextAsync();

            CredentialViewModel credential = _scope.Resolve<CredentialViewModel>(new NamedParameter("credential", credentialRecord));

            var credentialDefinitionId = CredentialDefinitionId.Parse(credentialRecord.CredentialDefinitionId);
            credential.CredentialName = credentialDefinitionId.Tag;
            var connectionRecord = await _connectionService.GetAsync(context, credentialRecord.ConnectionId);
            credential.CredentialSubtitle = connectionRecord.Alias.Name;
            credential.IssuedAt = connectionRecord.CreatedAtUtc.HasValue ? connectionRecord.CreatedAtUtc.Value.ToLocalTime() : (DateTime?)null;
            if (credentialRecord.State == CredentialState.Offered)
            {
                var attributes = new List<CredentialAttribute>();
                credential.Attributes = attributes;
                foreach (var credentialPreviewAttribute in credentialRecord.CredentialAttributesValues)
                {
                    attributes.Add(new CredentialAttribute { Name = credentialPreviewAttribute.Name, Value = credentialPreviewAttribute.Value.ToString(), Type = "Text" });
                }
            }

            return credential;
        }

        public async Task<IList<CredentialViewModel>> AssembleMany(IList<CredentialRecord> credentialRecords)
        {
            if (credentialRecords == null) return null;

            IList<CredentialViewModel> credentials = new List<CredentialViewModel>();

            foreach (CredentialRecord credentialRecord in credentialRecords)
            {
                credentials.Add(await Assemble(credentialRecord));
            }

            return credentials;
        }
    }
}
