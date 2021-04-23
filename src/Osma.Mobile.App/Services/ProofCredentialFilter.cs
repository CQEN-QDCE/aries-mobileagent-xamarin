using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Osma.Mobile.App.Utilities;
using System;
using System.Collections.Generic;

namespace Osma.Mobile.App.Services
{
    public class ProofCredentialFilter
    {
        private readonly IList<AttributeFilter> _restrictions;
        private readonly string _attributeName;
        private readonly IList<string> _schemaIds;
        private readonly IList<string> _schemaIssuerDids;
        private readonly IList<string> _schemaNames;
        private readonly IList<string> _schemaVersions;
        private readonly IList<string> _issuerDids;
        private readonly IList<string> _credentialDefinitionIds;
        private readonly IList<AttributeValue> _attributeValues;

        public ProofCredentialFilter(IList<AttributeFilter> restrictions, string attributeName)
        {
            if (restrictions == null) throw new ArgumentNullException(nameof(restrictions));

            if (string.IsNullOrEmpty(attributeName)) throw new ArgumentNullException(nameof(attributeName));

            _restrictions = restrictions;

            _attributeName = attributeName;

            _schemaIds = new List<string>();
            _schemaIssuerDids = new List<string>();
            _schemaNames = new List<string>();
            _schemaVersions = new List<string>();
            _issuerDids = new List<string>();
            _credentialDefinitionIds = new List<string>();
            _attributeValues = new List<AttributeValue>();
            
            Init();
        }

        public IList<CredentialRecord> Filter(IList<CredentialRecord> credentialRecords)
        {
            if (credentialRecords == null) throw new ArgumentNullException(nameof(credentialRecords));

            IList<CredentialRecord> filteredCredentialRecords = new List<CredentialRecord>();

            foreach (CredentialRecord credentialRecord in credentialRecords)
            {
                if (ContainsAttributeName(credentialRecord))
                {
                    if (_schemaIds.Count > 0 && !_schemaIds.Contains(credentialRecord.SchemaId)) continue;
                    if (_schemaNames.Count > 0 || _schemaVersions.Count > 0 || _issuerDids.Count > 0)
                    {
                        SchemaId schemaId = SchemaId.Parse(credentialRecord.SchemaId);
                        if (_schemaNames.Count > 0 && !_schemaNames.Contains(schemaId.Name)) continue;
                        if (_schemaVersions.Count > 0 && !_schemaVersions.Contains(schemaId.Version)) continue;
                        if (_schemaIssuerDids.Count > 0 && !_schemaIssuerDids.Contains(schemaId.Did)) continue;
                    }
                    if (_issuerDids.Count > 0 && !_issuerDids.Contains(CredentialDefinitionId.Parse(credentialRecord.CredentialDefinitionId).Did)) continue;
                    if (_credentialDefinitionIds.Count > 0 && !_credentialDefinitionIds.Contains(credentialRecord.CredentialDefinitionId)) continue;
                    if (_attributeValues.Count > 0 )
                    {
                        bool found = false;
                        foreach(AttributeValue attributeValue in _attributeValues)
                        {
                            foreach(CredentialPreviewAttribute credentialPreviewAttribute in credentialRecord.CredentialAttributesValues)
                            {
                                if (credentialPreviewAttribute.Name == attributeValue.Name && credentialPreviewAttribute.Value.ToString() == attributeValue.Value)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }
                        if (!found) continue;
                    }

                    filteredCredentialRecords.Add(credentialRecord);
                }
            }

            return filteredCredentialRecords;
        }

        private void Init()
        {
            //foreach (AttributeFilter attributeFilter in _restrictions)
            //{
            //    if (!string.IsNullOrWhiteSpace(attributeFilter.SchemaId)) _schemaIds.Add(attributeFilter.SchemaId);
            //}

            //foreach (AttributeFilter attributeFilter in _restrictions)
            //{
            //    if (!string.IsNullOrWhiteSpace(attributeFilter.SchemaIssuerDid)) _schemaIssuerDids.Add(attributeFilter.SchemaIssuerDid);
            //}

            //foreach (AttributeFilter attributeFilter in _restrictions)
            //{
            //    if (!string.IsNullOrWhiteSpace(attributeFilter.SchemaName)) _schemaNames.Add(attributeFilter.SchemaName);
            //}

            //foreach (AttributeFilter attributeFilter in _restrictions)
            //{
            //    if (!string.IsNullOrWhiteSpace(attributeFilter.SchemaVersion)) _schemaVersions.Add(attributeFilter.SchemaVersion);
            //}

            //foreach (AttributeFilter attributeFilter in _restrictions)
            //{
            //    if (!string.IsNullOrWhiteSpace(attributeFilter.IssuerDid)) _issuerDids.Add(attributeFilter.IssuerDid);
            //}

            //foreach (AttributeFilter attributeFilter in _restrictions)
            //{
            //    if (!string.IsNullOrWhiteSpace(attributeFilter.CredentialDefinitionId)) _credentialDefinitionIds.Add(attributeFilter.CredentialDefinitionId);
            //}

            //foreach (AttributeFilter attributeFilter in _restrictions)
            //{
            //    if (attributeFilter.AttributeValue != null && 
            //        !string.IsNullOrWhiteSpace(attributeFilter.AttributeValue.Name) && 
            //        !string.IsNullOrWhiteSpace(attributeFilter.AttributeValue.Value)) _attributeValues.Add(attributeFilter.AttributeValue);
            //}

            foreach (AttributeFilter attributeFilter in _restrictions)
            {
                if (!string.IsNullOrWhiteSpace(attributeFilter.SchemaId)) _schemaIds.Add(attributeFilter.SchemaId);
                if (!string.IsNullOrWhiteSpace(attributeFilter.SchemaIssuerDid)) _schemaIssuerDids.Add(attributeFilter.SchemaIssuerDid);
                if (!string.IsNullOrWhiteSpace(attributeFilter.SchemaName)) _schemaNames.Add(attributeFilter.SchemaName);
                if (!string.IsNullOrWhiteSpace(attributeFilter.SchemaVersion)) _schemaVersions.Add(attributeFilter.SchemaVersion);
                if (!string.IsNullOrWhiteSpace(attributeFilter.IssuerDid)) _issuerDids.Add(attributeFilter.IssuerDid);
                if (!string.IsNullOrWhiteSpace(attributeFilter.CredentialDefinitionId)) _credentialDefinitionIds.Add(attributeFilter.CredentialDefinitionId);
                if (attributeFilter.AttributeValue != null &&
                    !string.IsNullOrWhiteSpace(attributeFilter.AttributeValue.Name) &&
                    !string.IsNullOrWhiteSpace(attributeFilter.AttributeValue.Value)) _attributeValues.Add(attributeFilter.AttributeValue);
            }
        }

        private bool ContainsAttributeName(CredentialRecord credentialRecord)
        {
            foreach (CredentialPreviewAttribute credentialAttribute in credentialRecord.CredentialAttributesValues)
            {
                if (credentialAttribute.Name == _attributeName) return true;
            }
            return false;
        }
    }
}
