using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Xamarin.Forms;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Osma.Mobile.Uwp
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class MainPage
    {
        public MainPage()
        {
            //ZXing.Net.Mobile.Forms.WindowsUniversal.Init();
            var t = Windows.Storage.ApplicationData.Current.LocalFolder;
            this.InitializeComponent();
            var host = Osma.Mobile.App.App
   .BuildHost(Assembly.GetExecutingAssembly())
   .Build();

            var application = host.Services.GetRequiredService<Osma.Mobile.App.App>();
            this.LoadApplication(application);

            Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;

            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
        }

        private void CoreWindow_CharacterReceived(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.CharacterReceivedEventArgs args)
        {
            if (args.KeyCode == 27) //Escape
            {
                // your code here fore Escape key
            }
            if (args.KeyCode == 13) //Enter
            {
                // your code here fore Enter key
            }
        }

        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.F1)
            {
                MessagingCenter.Send<object>(this, "ScanInvite");
            }
        }
    }
}