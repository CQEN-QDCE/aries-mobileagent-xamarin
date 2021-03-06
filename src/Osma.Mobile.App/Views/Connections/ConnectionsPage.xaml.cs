﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Osma.Mobile.App.Views.Connections
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectionsPage : ContentPage
    {
        public ConnectionsPage()
        {
            InitializeComponent();
            var source = ConnectionListView.ItemsSource;
            if (source != null)
            {
                ((ObservableCollection<object>)source).CollectionChanged += ListViewBehaviors_CollectionChanged;
            }
        }

        private void ListViewBehaviors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var source = (ObservableCollection<object>)ConnectionListView.ItemsSource;
            if (source != null)
            {
                ConnectionListView.ScrollTo(source.First(), ScrollToPosition.Start, true);
            }
        }
    }
}