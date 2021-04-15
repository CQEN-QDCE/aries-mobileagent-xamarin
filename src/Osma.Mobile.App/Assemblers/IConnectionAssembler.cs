using Hyperledger.Aries.Features.DidExchange;
using Osma.Mobile.App.ViewModels.Connections;
using System.Collections.Generic;

namespace Osma.Mobile.App.Assemblers
{
    public interface IConnectionAssembler
    {
        ConnectionViewModel Assemble(ConnectionRecord connectionRecord);

        IList<ConnectionViewModel> AssembleMany(IList<ConnectionRecord> connectionRecords);
    }
}
