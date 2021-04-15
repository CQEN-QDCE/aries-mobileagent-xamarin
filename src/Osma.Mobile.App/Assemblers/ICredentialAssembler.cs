using Hyperledger.Aries.Features.IssueCredential;
using Osma.Mobile.App.ViewModels.Credentials;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osma.Mobile.App.Assemblers
{
    public interface ICredentialAssembler
    {
        Task<CredentialViewModel> Assemble(CredentialRecord credentialRecord);

        Task<IList<CredentialViewModel>> AssembleMany(IList<CredentialRecord> credentialRecords);
    }
}
