﻿using Hyperledger.Aries.Features.DidExchange;

namespace Osma.Mobile.App.Events
{
    public enum ApplicationEventType
    {
        ConnectionUpdated,
        CloudAgentsUpdated,
        CredentialUpdated,
        ProofRequestUpdated,
        ProofRequestAtrributeUpdated,
        PassCodeAuthorised,
        PassCodeAuthorisedCloudAgent,
        PassCodeAuthorisedDeleteConnection,
        PassCodeAuthorisedSSO,
        PassCodeAuthorisedDeleteCloudAgent,
        PassCodeAuthorisedCredentialAccept,
        PassCodeAuthorisedCredentialReject,
        PassCodeAuthorisedProofAccept,
        PassCodeAuthorisedProofReject
    }

    public class ApplicationEvent
    {
        public ApplicationEventType Type { get; set; }
        public ConnectionInvitationMessage Invite { get; set; }
        public int Status { get; set; }
    }
}