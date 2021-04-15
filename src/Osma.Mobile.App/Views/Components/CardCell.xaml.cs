using System.Globalization;
using Xamarin.Forms;

namespace Osma.Mobile.App.Views.Components
{
    public partial class CardCell : ViewCell
    {
        public CardCell()
        {
            InitializeComponent();
        }

        public static readonly BindableProperty TitleProperty = BindableProperty.Create("Title", typeof(string), typeof(CardCell), "", propertyChanged: TitlePropertyChanged);

        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        private static void TitlePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CardCell cell = (CardCell)bindable;
            Device.BeginInvokeOnMainThread(() =>
            {
                cell.TitleLabel.Text = newValue?.ToString();
            });
        }

        public static readonly BindableProperty AttributeCountProperty =
BindableProperty.Create("AttributeCount", typeof(string), typeof(CardCell), "",
propertyChanged: AttributeCountPropertyChanged);

        public string AttributeCount
        {
            get { return (string)GetValue(AttributeCountProperty); }
            set { SetValue(AttributeCountProperty, value); }
        }

        private static void AttributeCountPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CardCell cell = (CardCell)bindable;
            Device.BeginInvokeOnMainThread(() =>
            {
                cell.AttributeCountLabel.Text = newValue.ToString();
                if (string.IsNullOrWhiteSpace(newValue.ToString()))
                {
                    Grid.SetRowSpan(cell.AttributeCountLabel, 2);
                }
                else
                {
                    Grid.SetRowSpan(cell.AttributeCountLabel, 1);
                }
            });
        }

        public static readonly BindableProperty SubtitleProperty =
            BindableProperty.Create("Subtitle", typeof(string), typeof(CardCell), "",
            propertyChanged: SubtitlePropertyChanged);

        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        private static void SubtitlePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CardCell cell = (CardCell)bindable;
            Device.BeginInvokeOnMainThread(() =>
            {
                cell.SubtitleLabel.Text = newValue == null ? string.Empty : newValue.ToString();
                if (newValue == null || string.IsNullOrWhiteSpace(newValue.ToString()))
                {
                    Grid.SetRowSpan(cell.TitleLabel, 2);
                }
                else
                {
                    Grid.SetRowSpan(cell.TitleLabel, 1);
                }
            });
        }

        public static readonly BindableProperty DateTimeProperty =
            BindableProperty.Create("DateTime", typeof(string), typeof(CardCell), "",
            propertyChanged: DateTimePropertyChanged);

        public string DateTime
        {
            get { return (string)GetValue(DateTimeProperty); }
            set { SetValue(DateTimeProperty, value); }
        }

        private static void DateTimePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CardCell cell = (CardCell)bindable;
            Device.BeginInvokeOnMainThread(() =>
            {
                //cell.DateTimeLabel.Text = string.IsNullOrWhiteSpace(newValue.ToString()) ? string.Empty : AppResources.CrendentialIssuedLabel + " " + newValue.ToString().Split(' ')[0];
                cell.DateTimeLabel.Text = string.IsNullOrWhiteSpace(newValue.ToString()) ? string.Empty : newValue.ToString().Split(' ')[0];
                if (string.IsNullOrWhiteSpace(newValue.ToString()))
                {
                    Grid.SetRowSpan(cell.TitleLabel, 2);
                }
                else
                {
                    Grid.SetRowSpan(cell.TitleLabel, 1);
                }
            });
        }
    }
}