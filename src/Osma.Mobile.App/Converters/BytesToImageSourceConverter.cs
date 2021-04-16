using System;
using System.Globalization;
using System.IO;
using Xamarin.Forms;

namespace Osma.Mobile.App.Converters
{
    public class BytesToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ImageSource objImageSource;
            //
            if (value != null)
            {
                byte[] bytImageData = (byte[])value;
                //
                objImageSource = ImageSource.FromStream(() => new MemoryStream(bytImageData));
            }
            else
            {
                objImageSource = null;
            }
            //
            return objImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}