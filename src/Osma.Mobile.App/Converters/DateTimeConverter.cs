using System;
using System.Globalization;
using Xamarin.Forms;

namespace Osma.Mobile.App.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = System.Convert.ToString(parameter) ?? "u";

            //switch (param.ToUpper())
            //{
            //    case "U":
            //        return ((string)value).ToUpper();
            //    case "L":
            //        return ((string)value).ToLower();
            //    default:
            //        return ((string)value);
            //}

            if (value == null || !(value is DateTime)) return string.Empty;

            DateTime datetime = (DateTime)value;

            string date = datetime.Day + " ";

            switch (datetime.Month)
            {
                case 1:
                    date += "JAN";
                    break;

                case 2:
                    date += "FÉV";
                    break;

                case 3:
                    date += "MAR";
                    break;

                case 4:
                    date += "AVR";
                    break;

                case 5:
                    date += "MAI";
                    break;

                case 6:
                    date += "JUI";
                    break;

                case 7:
                    date += "JUI";
                    break;

                case 8:
                    date += "AOU";
                    break;

                case 9:
                    date += "SEP";
                    break;

                case 10:
                    date += "OCT";
                    break;

                case 11:
                    date += "NOV";
                    break;

                case 12:
                    date += "DÉC";
                    break;
            }
            date += " " + datetime.Year;
            date += " | " + datetime.Hour + "H" + (datetime.Minute < 10 ? "0" + datetime.Minute : datetime.Minute.ToString());
            return date;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}