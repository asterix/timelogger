using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Net;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using System.IO.IsolatedStorage;
using System.IO;
using System.Text;
using Microsoft.Phone.Tasks;


namespace Timelog
{
    public partial class ExportPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        public static contextForDate MonthSel;
        public static string ExportFileName;

        public class contextForDate
        {
            public DateTime MonthSelected {get; set;}

            public contextForDate()
            {
                MonthSelected = new DateTime();
            }
        }

        private MainPage.StorageDB exportDB;

        public ExportPage()
        {
            InitializeComponent();

            MonthSel = new contextForDate();
            exportDB = new MainPage.StorageDB(MainPage.DBNameString);

            MonthSel.MonthSelected = DateTime.Now;
            ContentPanel.DataContext = MonthSel;
        }

        public string GetddmmyyyyStoredCultureExp(DateTime dat)
        {
            string today = dat.ToString(MainPage.culture);
            today = today.Substring(0, today.IndexOf(' ')); //Take dd.mm.yyyy
            return today;
        }

        //Database binding change updates
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion


        //Handle flick on the screen
        private void Flick_handler(object sender, FlickGestureEventArgs e)
        {
            if (e.HorizontalVelocity > Timelog.MainPage.FLICKVELOCITY)
            {
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
            else if (e.HorizontalVelocity < -Timelog.MainPage.FLICKVELOCITY)
            {
                
            }
        }

        private void NavigateToAbout(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void OnBackKeyPress()
        {
            NavigationService.GoBack();
        }

        private void SkyDrive_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DateTime ReqdMonth = new DateTime(MonthSel.MonthSelected.Year, MonthSel.MonthSelected.Month, 1);
            string FileName = "Timelogs_" + ReqdMonth.Month.ToString() + "-" + ReqdMonth.Year.ToString();
            bool anythingtoupload = false;

            //Delete all files in isolated storage
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            store.DeleteFile(FileName);
            store.DeleteFile(FileName + ".txt");
            store.DeleteFile(FileName + ".csv");
            store.DeleteFile(FileName + ".xlsx");

            //To be used to export
            ExportFileName = FileName;

            //Write the text file
            if (NeedText.IsChecked.HasValue && NeedText.IsChecked.Value)
            {
                anythingtoupload = true;
                WriteToTxt(MonthSel.MonthSelected, FileName);
            }

            //Write CSV
            if (NeedCSV.IsChecked.HasValue && NeedCSV.IsChecked.Value)
            {
                anythingtoupload = true;
                WriteToCSV(MonthSel.MonthSelected, FileName);
            }

            //Write XLSX
            if (NeedXLS.IsChecked.HasValue && NeedXLS.IsChecked.Value)
            {
                //anythingtoupload = true;
            }

            if (anythingtoupload)
            {
                NavigationService.Navigate(new Uri("/OneDrivePage.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show("Select at least one format");
            }
            
        }

        private DateTime ParseStringToDateTime(string date)
        {
            return DateTime.Parse((string)date, Timelog.MainPage.culture);
        }

        //Gets the row against the date from the database
        public MainPage.DBRow QueryRowInDb(string date)
        {
            var query = (from row in exportDB.logentries where row.Date == date select row).SingleOrDefault();
            return (MainPage.DBRow)query;
        }

        private StringBuilder WriteToTxt(DateTime SelectedMonth, string TxtFile)
        {
            StringBuilder content = new StringBuilder();
            //Init to the first of the Month
            DateTime ReqdMonth = new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1);
            int Month = ReqdMonth.Month;

            content.AppendLine("Timelog for the month : " + ReqdMonth.Month.ToString() + "-" + ReqdMonth.Year.ToString());
            content.AppendLine("");
            content.AppendLine("+--Date---+---In-Time---+---Out-Time--+--Lunch--+--Clocked--+");

            while (ReqdMonth.Month == Month)
            {
                MainPage.DBRow query = QueryRowInDb(GetddmmyyyyStoredCultureExp(ReqdMonth));

                if (query != null)
                {
                    TimeSpan LunchBreak = ParseStringToDateTime(query.LunchOut).Subtract(ParseStringToDateTime(query.LunchIn));
                    string lunchbreak = "00:00";

                    if ((LunchBreak.Hours >= 0) && (LunchBreak.Minutes >= 0) && (query.ClockedTime.CompareTo("00:00") != 0))
                    {
                        lunchbreak = LunchBreak.ToString("hh") + ":" + LunchBreak.ToString("mm");
                    }

                    content.AppendLine(query.Date + " --+-- " + query.TimeIn + " --+-- " + query.TimeOut + " --+-- " + lunchbreak + " --+-- " + query.ClockedTime);
                }
                else
                {
                    content.AppendLine(GetddmmyyyyStoredCultureExp(ReqdMonth) + " --+-- 00:00 --+-- 00:00 --+-- 00:00 --+-- 00:00");
                }

                //Next Day
                ReqdMonth = ReqdMonth.AddDays(1);
            }

            if (NeedText.IsChecked.HasValue && NeedText.IsChecked.Value && TxtFile != String.Empty)
            {
                string FileName = TxtFile + ".txt";
                IsolatedStorageFile txtfile = IsolatedStorageFile.GetUserStoreForApplication();

                System.IO.StreamWriter stream = new System.IO.StreamWriter(new IsolatedStorageFileStream(FileName, System.IO.FileMode.OpenOrCreate, txtfile));
                stream.Write(content.ToString());
                stream.Close();
            }

            return content;
        }

        private void WriteToCSV(DateTime SelectedMonth, string CsvFile)
        {
            StringBuilder content = new StringBuilder();
            //Init to the first of the Month
            DateTime ReqdMonth = new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1);
            int Month = ReqdMonth.Month;

            //Set Columns
            content.AppendLine("Date,In-Time,Out-Time,Lunch,Clocked");

            while (ReqdMonth.Month == Month)
            {
                MainPage.DBRow query = QueryRowInDb(GetddmmyyyyStoredCultureExp(ReqdMonth));

                if (query != null)
                {
                    TimeSpan LunchBreak = ParseStringToDateTime(query.LunchOut).Subtract(ParseStringToDateTime(query.LunchIn));
                    string lunchbreak = "00:00";

                    if ((LunchBreak.Hours >= 0) && (LunchBreak.Minutes >= 0) && (query.ClockedTime.CompareTo("00:00") != 0))
                    {
                        lunchbreak = LunchBreak.ToString("hh") + ":" + LunchBreak.ToString("mm");
                    }

                    content.AppendLine(query.Date + "," + query.TimeIn + "," + query.TimeOut + "," + lunchbreak + "," + query.ClockedTime);
                }
                else
                {
                    content.AppendLine(GetddmmyyyyStoredCultureExp(ReqdMonth) + ",00:00,00:00,00:00,00:00");
                }

                //Next Day
                ReqdMonth = ReqdMonth.AddDays(1);
            }

            if (CsvFile != String.Empty)
            {
                string FileName = CsvFile + ".csv";
                IsolatedStorageFile csvfile = IsolatedStorageFile.GetUserStoreForApplication();

                System.IO.StreamWriter stream = new System.IO.StreamWriter(new IsolatedStorageFileStream(FileName, System.IO.FileMode.OpenOrCreate, csvfile));
                stream.Write(content.ToString());
                stream.Close();
            }
        }

        private void Email_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StringBuilder content = WriteToTxt(MonthSel.MonthSelected, "");
            EmailSender("Timelogger Timesheet", content.ToString());
        }

        public void EmailSender(string subject, string body)
        {
            EmailComposeTask emailtask = new EmailComposeTask();

            emailtask.Subject = subject;
            emailtask.Body = body;

            emailtask.Show();
        }

        private void ExportMail_Click(object sender, EventArgs e)
        {
            Email_Tap(null, null);
        }

        private void ExportSkyDrive_Click(object sender, EventArgs e)
        {
            SkyDrive_Tap(null, null);
        }
    }
}