using System;

// https://github.com/hyperledger/indy-sdk/blob/master/libindy/src/domain/anoncreds/credential_definition.rs
namespace Osma.Mobile.App.Utilities
{
    public class CredentialDefinitionId
    {
        private CredentialDefinitionId()
        {
        }

        public CredentialDefinitionId(string did, int reference, string tag, SignatureType signatureType = SignatureType.CamenischLysyanskaya)
        {
            Did = did;
            Marker = 3;
            SignatureType = signatureType;
            Reference = reference;
            Tag = tag;
        }

        public string Did { get; private set; }

        public int Marker { get; private set; }

        public SignatureType SignatureType { get; private set; }

        public int Reference { get; private set; }

        public string Tag { get; private set; }

        public static CredentialDefinitionId Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new FormatException();

            var tokens = value.Split(':');

            if (tokens.Length != 5) throw new FormatException();

            var credentialDefinitionId = new CredentialDefinitionId();

            credentialDefinitionId.Did = tokens[0];
            credentialDefinitionId.Marker = int.Parse(tokens[1]);
            credentialDefinitionId.SignatureType = tokens[2] == "CL" ? SignatureType.CamenischLysyanskaya : throw new FormatException();
            credentialDefinitionId.Reference = int.Parse(tokens[3]);
            credentialDefinitionId.Tag = tokens[4];

            return credentialDefinitionId;
        }

        public override string ToString()
        {
            return Did + ":" + Marker + ":" + "CL" + ":" + Reference + ":" + Tag;
        }
    }

    // Type of the signature
    public enum SignatureType
    {
        // ZKP scheme CL (Camenisch-Lysyanskaya) is the only type currently supported in Indy.
        CamenischLysyanskaya
    }
}