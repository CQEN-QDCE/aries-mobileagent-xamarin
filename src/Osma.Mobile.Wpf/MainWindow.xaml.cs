using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

namespace Osma.Mobile.Wpf
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            InitializeComponent();
            Forms.Init();
            Microsoft.Extensions.Hosting.IHost host = Osma.Mobile.App.App.BuildHost().Build();
            Mobile.App.App application = host.Services.GetRequiredService<Osma.Mobile.App.App>();
            LoadApplication(application);
        }
    }
}
