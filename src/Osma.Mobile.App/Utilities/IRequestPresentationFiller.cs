using Osma.Mobile.App.ViewModels.ProofRequests;
using System.Threading.Tasks;

namespace Osma.Mobile.App.Utilities
{
    public interface IRequestPresentationFiller
    {
        Task Fill(ProofRequestViewModel proof);
    }
}
