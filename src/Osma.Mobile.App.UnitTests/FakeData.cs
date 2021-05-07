using System.Collections.Generic;
using Hyperledger.Aries.Features.IssueCredential;

namespace Osma.Mobile.App.UnitTests
{
    public static class FakeData
    {
        //public static Credentials Credentials { get }
        public static class Credentials
        {
            public static CredentialRecord BirthCertificate = new CredentialRecord
            {
                ConnectionId = "f8a9fb89-38ef-4317-811a-ab48196ca888",
                CredentialAttributesValues = new List<CredentialPreviewAttribute> 
                { 
                    new CredentialPreviewAttribute { Name = "First Name", Value = "David" },
                    new CredentialPreviewAttribute { Name = "Last Name", Value = "Copperfield" },
                    new CredentialPreviewAttribute { Name = "Birth Date", Value = "09-16-1956" },
                    new CredentialPreviewAttribute { Name = "Sex", Value = "Male" }
                },
                CredentialDefinitionId = "XpHQgyg7Dd4WerGtGCL3KN:3:CL:1:Birth Certificate",
                CredentialId = "ac6c10c9-7b8c-4a3b-837e-c8ba8c7a9d81",
                SchemaId = "XpHQgyg7Dd4WerGtGCL3KN:2:Birth Certificate:1.0",
                State = CredentialState.Issued
            };
            public static CredentialRecord DriverLicense = new CredentialRecord
            {
                ConnectionId = "d7z3dg90-27sy-4534-716s-ab26581ca123",
                CredentialAttributesValues = new List<CredentialPreviewAttribute>
                {
                    new CredentialPreviewAttribute { Name = "License Number", Value = "L2983898" },
                    new CredentialPreviewAttribute { Name = "First Name", Value = "David" },
                    new CredentialPreviewAttribute { Name = "Last Name", Value = "Copperfield" },
                    new CredentialPreviewAttribute { Name = "Address", Value = "2570 24TH STREET ANYTOWN, CA 95818" },
                    new CredentialPreviewAttribute { Name = "Birth Date", Value = "09-16-1956" },
                    new CredentialPreviewAttribute { Name = "Class", Value = "C" },
                    new CredentialPreviewAttribute { Name = "Sex", Value = "Male" },
                    new CredentialPreviewAttribute { Name = "Hair", Value = "Brown" },
                    new CredentialPreviewAttribute { Name = "Eyes", Value = "Brown" },
                    new CredentialPreviewAttribute { Name = "Height", Value = "5'-05\"" },
                    new CredentialPreviewAttribute { Name = "Weight", Value = "125 lbs" }
                },
                CredentialDefinitionId = "AxKLpqg9Bn6KzxJdUIL2LO:3:CL:2:Driver License",
                CredentialId = "ac6c10c9-7b8c-4a3b-837e-c8ba8c7a9d81",
                SchemaId = "AxKLpqg9Bn6KzxJdUIL2LO:2:Driver License:1.0",
                State = CredentialState.Issued
            };
            public static CredentialRecord ClubMemberCard = new CredentialRecord
            {
                ConnectionId = "x9q8ss23-39hj-7820-991z-xg29846as222",
                CredentialAttributesValues = new List<CredentialPreviewAttribute>
                {
                    new CredentialPreviewAttribute { Name = "Name", Value = "THE LORDS CLUB" },
                    new CredentialPreviewAttribute { Name = "Membership Number", Value = "5678 780173 01509" }
                },
                CredentialDefinitionId = "BpHQgyg7Hd4WerYtGCK3KS:3:CL:3:The Lords Club Card",
                CredentialId = "ac6c10c9-7b8c-4a3b-837e-c8ba8c7a9d81",
                SchemaId = "BpHQgyg7Hd4WerYtGCK3KS:2:The Lords Club Card:1.0",
                State = CredentialState.Issued
            };
        }
    }
}
