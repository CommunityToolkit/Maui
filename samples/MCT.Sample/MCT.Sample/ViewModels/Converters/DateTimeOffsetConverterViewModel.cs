using System;

namespace MCT.Sample.ViewModels.Converters
{
    public class DateTimeOffsetConverterViewModel : BaseViewModel
    {
        DateTimeOffset theDate = DateTimeOffset.Now;

        public DateTimeOffset TheDate
        {
            get => theDate;
            set => SetProperty(ref theDate, value);
        }
    }
}