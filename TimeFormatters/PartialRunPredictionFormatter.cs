using System;

namespace LiveSplit.TimeFormatters
{
    public class PartialRunPredictionFormatter : ITimeFormatter
    {
        public TimeAccuracy Accuracy { get; set; }

        public PartialRunPredictionFormatter(TimeAccuracy accuracy)
        {
            Accuracy = accuracy;
        }

        public string Format(TimeSpan? time)
        {
            var formatter = new RegularTimeFormatter(Accuracy);
            if (time == null)
                return TimeFormatConstants.DASH;
            else
                return formatter.Format(time);
        }
    }
}
