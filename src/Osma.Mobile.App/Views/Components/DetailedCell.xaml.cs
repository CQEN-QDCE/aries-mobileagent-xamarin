﻿using Xamarin.Forms;

namespace Osma.Mobile.App.Views.Components
{
    public partial class DetailedCell : ViewCell
    {
        // It may be worth it in the future to write custom renderers for each platform as this will give performance benefits

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create("Title", typeof(string), typeof(DetailedCell), "", propertyChanged: TitlePropertyChanged);

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
            DetailedCell cell = (DetailedCell)bindable;
            Device.BeginInvokeOnMainThread(() =>
            {
                cell.TitleLabel.Text = newValue?.ToString();
            });
        }

        public static readonly BindableProperty SubtitleProperty =
            BindableProperty.Create("Subtitle", typeof(string), typeof(DetailedCell), "",
            propertyChanged: SubtitlePropertyChanged);

        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        private static void SubtitlePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            DetailedCell cell = (DetailedCell)bindable;
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
            BindableProperty.Create("DateTime", typeof(string), typeof(DetailedCell), "",
            propertyChanged: DateTimePropertyChanged);

        public string DateTime
        {
            get { return (string)GetValue(DateTimeProperty); }
            set { SetValue(DateTimeProperty, value); }
        }

        private static void DateTimePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            DetailedCell cell = (DetailedCell)bindable;
            Device.BeginInvokeOnMainThread(() =>
            {
                cell.DateTimeLabel.Text = newValue.ToString();
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

        public static readonly BindableProperty TextAnnotationProperty =
BindableProperty.Create("TextAnnotation", typeof(string), typeof(DetailedCell), "",
propertyChanged: TextAnnotationPropertyChanged);

        public string TextAnnotation
        {
            get { return (string)GetValue(TextAnnotationProperty); }
            set { SetValue(TextAnnotationProperty, value); }
        }

        private static void TextAnnotationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            DetailedCell cell = (DetailedCell)bindable;
            Device.BeginInvokeOnMainThread(() =>
            {
                //cell.TextAnnotationLabel.Text = (newValue == null ? string.Empty : newValue.ToString());
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

        public static readonly BindableProperty ImageURLProperty =
            BindableProperty.Create("ImageURL", typeof(string), typeof(DetailedCell), "", propertyChanged: ImageURLPropertyChanged);

        public string ImageURL
        {
            get { return (string)GetValue(ImageURLProperty); }
            set { SetValue(ImageURLProperty, value); }
        }

        private static void ImageURLPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            DetailedCell cell = (DetailedCell)bindable;
            Device.BeginInvokeOnMainThread(() =>
            {
                cell.Image.Source = newValue?.ToString();
            });
        }

        public static readonly BindableProperty IsNewProperty =
            BindableProperty.Create("IsNew", typeof(bool), typeof(DetailedCell), false,
                propertyChanged: IsNewPropertyChanged);

        public bool IsNew
        {
            get { return (bool)GetValue(IsNewProperty); }
            set { SetValue(IsNewProperty, value); }
        }

        private static void IsNewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            DetailedCell cell = (DetailedCell)bindable;
            bool isNew = (bool)newValue;
            Device.BeginInvokeOnMainThread(() =>
            {
                cell.NewLabelContainer.IsVisible = isNew;
                cell.NewLabel.IsVisible = isNew;
                cell.TitleLabel.FontAttributes = isNew ? FontAttributes.Bold : FontAttributes.None;
                cell.View.BackgroundColor = isNew ? Color.FromHex("#f2f7ea") : Color.Transparent;
            });
        }

        public DetailedCell()
        {
            InitializeComponent();
        }
    }
}