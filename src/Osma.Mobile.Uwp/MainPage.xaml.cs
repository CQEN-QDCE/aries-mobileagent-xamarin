using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using ZXing.Net.Mobile.Forms.WindowsUniversal;
using Windows.UI.Core;
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
