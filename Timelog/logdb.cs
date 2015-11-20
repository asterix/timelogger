using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Globalization;
using Microsoft.Phone.Tasks;

namespace Timelog
{
    public class StringFormattedEntries
    {
        //To display
        public String DateString;
        public String TimeStringIn;
        public String TimeStringOut;
        public String TimeStringLunchIn;
        public String TimeStringLunchOut;
        public String TimeStringClocked;
    }

    public class DailyEntry: StringFormattedEntries
    {
        private DateTime InternalDate;
        private DateTime InternalTimeIn;
        private DateTime InternalTimeLunchIn;
        private DateTime InternalTimeLunchOut;
        private DateTime InternalTimeOut;
        private TimeSpan InternalClocked;
        
        public DailyEntry()
        {
            DateString = "01-01-1988";
            TimeStringIn = "00:00";
            TimeStringOut = "00:00";
            TimeStringLunchIn = "00:00";
            TimeStringLunchOut = "00:00";
            TimeStringClocked = "00:00";

            InternalDate = DateTime.Now;
            DateString = InternalDate.ToShortDateString();
        }

        public void SetInTime()
        {
            InternalTimeIn = DateTime.Now;
            TimeStringIn = InternalTimeIn.ToShortTimeString();
        }

        public void SetLunchInTime()
        {
            InternalTimeLunchIn = DateTime.Now;
            TimeStringLunchIn = InternalTimeLunchIn.ToShortTimeString();
        }

        public void SetLunchOutTime()
        {
            InternalTimeLunchOut = DateTime.Now;
            TimeStringLunchOut = InternalTimeLunchOut.ToShortTimeString();
        }

        public void SetOutTime()
        {
            InternalTimeOut = DateTime.Now;
            TimeStringOut = InternalTimeOut.ToShortTimeString();
        }

        public void CalculateClocked()
        {
            TimeSpan MornToLunch = InternalTimeLunchIn.Subtract(InternalTimeIn);
            TimeSpan AfterLunchToExit = InternalTimeOut.Subtract(InternalTimeLunchOut);

            InternalClocked = MornToLunch + AfterLunchToExit;

            //Update display strings
            TimeStringClocked = InternalClocked.Hours.ToString() + ":" + InternalClocked.Minutes.ToString();
        }


    }
}
