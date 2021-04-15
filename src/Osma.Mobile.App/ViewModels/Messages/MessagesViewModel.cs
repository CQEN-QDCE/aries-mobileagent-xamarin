using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.ViewModels.Account;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.Messages
{
    public class MessagesViewModel : ABaseViewModel
    {
        public MessagesViewModel(IUserDialogs userDialogs, INavigationService navigationService) : base(AppResources.MessagesPageTitle, userDialogs, navigationService)
        {
        }

        private async Task ConfigureSettings()
        {
            await NavigationService.NavigateToAsync<AccountViewModel>();
        }

        private async Task CreateInvitation()
        {
            await NavigationService.NavigateToAsync<CreateInvitationViewModel>();
        }

        public ICommand ConfigureSettingsCommand => new Command(async () => await ConfigureSettings());

        public ICommand CreateInvitationCommand => new Command(async () => await CreateInvitation());
    }
}
