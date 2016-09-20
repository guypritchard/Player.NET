namespace Player.Db
{
    using System;
    using System.IO;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream((byte[])value);
                bi.EndInit();
                return bi;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
