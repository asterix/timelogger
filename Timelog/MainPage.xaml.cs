using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;


namespace Timelog
{
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        
        //Name of the database file on isolated storage
        public static string DBNameString = "Data Source=isostore:/timelog.sdf";

        public static double FLICKVELOCITY = 1500.0;


        //Data base context class -> StorageDB
        public class StorageDB : DataContext
        {
            public StorageDB(string dbnamestring)
                : base(dbnamestring)
            {
                //Nothing here 
            }

            public Table<DBRow> logentries;
        }

        public class CurrentDisplay// : DataContext
        {
            public DBRow selectedEntry;
            public string calculationErrorMessage;

            public CurrentDisplay()
            {
                selectedEntry = new DBRow();
                calculationErrorMessage = "₪";
            }
        }

        //Calculate the clocked time
        public void UpdateClockedTime()
        {
            DateTime timein = ParseTimeString(DisplayDB.selectedEntry.TimeIn);
            DateTime timeout = ParseTimeString(DisplayDB.selectedEntry.TimeOut);
            DateTime lunchin = ParseTimeString(DisplayDB.selectedEntry.LunchIn);
            DateTime lunchout = ParseTimeString(DisplayDB.selectedEntry.LunchOut);
            DisplayDB.selectedEntry.ClockedTime = "00:00";

            TimeSpan firsthalf, secondhalf;

            firsthalf = lunchin.Subtract(timein);
            if ((firsthalf.Minutes < 0) || (firsthalf.Hours < 0))
            {
                DisplayDB.calculationErrorMessage = "₪ clocked time error: lunch-in < time-in";
                UpdateInfoBox();
                return;
            }

            TimeSpan interim = lunchout.Subtract(lunchin);
            if ((interim.Minutes < 0) || (interim.Hours < 0))
            {
                DisplayDB.calculationErrorMessage = "₪ clocked time error: lunch-in > lunch-out";
                UpdateInfoBox();
                return;
            }

            secondhalf = timeout.Subtract(lunchout);
            if ((secondhalf.Minutes < 0) || (secondhalf.Hours < 0))
            {
                DisplayDB.calculationErrorMessage = "₪ clocked time error: lunch-out > time-out";
                UpdateInfoBox();
                return;
            }

            //Cool! Everything went well
            DisplayDB.calculationErrorMessage = "";
            UpdateInfoBox();
            TimeSpan total = firsthalf + secondhalf;

            DisplayDB.selectedEntry.ClockedTime = total.ToString("hh") + ":" + total.ToString("mm");
        }

        public DateTime ParseTimeString(string time)
        {
            DateTime ret = DateTime.Parse((string)time, Timelog.MainPage.culture);
            return ret;
        }

        public void UpdateInfoBox()
        {
            CalcInfoBox.Text = DisplayDB.calculationErrorMessage;
        }

        //database is the datacontext stores all the entries from the database query
        private StorageDB database;
        //Selects the database entry linked to the selected date on the mainpage
        public CurrentDisplay DisplayDB;

        private void UpdateLastLoadedDate(DateTime date)
        {
            LastLoadedDate = date;
        }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            //Instantiate and construct 
            database = new StorageDB(DBNameString);
            //Now
            UpdateLastLoadedDate(DateTime.Now);
            DisplayDB = new CurrentDisplay();

            this.DataContext = this;
            
        }

        //Execute on opening the page
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //DataContext for the ContentPanel-MainPage
            if (e.NavigationMode != NavigationMode.New)
            {
                ContentPanel.DataContext = DisplayDB.selectedEntry;
            }
        }

        //Execute on opening the page
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //_MainPageLoaded = false;
        }

        //Fixed formats for the database
        //g Format Specifier- de-DE Culture- 01.10.2008 17:04 
        public static IFormatProvider culture = new System.Globalization.CultureInfo("de-DE");

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

        public static bool _MainPageLoaded = false;
        public static DateTime LastLoadedDate;
        public void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_MainPageLoaded)
            {
                //Get last loaded date
                string today = GetddmmyyyyStoredCulture(LastLoadedDate);

                DBRow readFromStorage = QueryRowInDb(today);

                //Create an entry against today
                if (readFromStorage == null)
                {
                    //Insert a new row
                    AddRowToDb(today);

                    //Query again
                    readFromStorage = QueryRowInDb(today);
                }

                SetDisplayDB(readFromStorage);

                //DataContext for the ContentPanel-MainPage
                ContentPanel.DataContext = DisplayDB.selectedEntry;
                
                //Never run this again! - Until the page is exited
                _MainPageLoaded = true;
            }

            //Re-calculate clocked time
            UpdateClockedTime();
        }

        //Returns dd.mm.yyyy (as in the storage culture) from the passed DateTime
        public string GetddmmyyyyStoredCulture(DateTime dat)
        {
            string today = dat.ToString(culture);
            today = today.Substring(0, today.IndexOf(' ')); //Take dd.mm.yyyy
            return today;
        }

        //Returns hh:mm (as in the stored culture) from the passed DateTime
        public string GethhmmStoredCulture(DateTime dat)
        {
            string today = dat.ToString(culture);
            today = today.Substring(today.IndexOf(' ') + 1, 5); //Take just hh:mm
            return today;
        }

        private void ToggleSecondaryTile()
        {
            //Create and pin the tile
            ShellTile Tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("Title=TimeLogger"));
            if (Tile == null)
            {
                StandardTileData data = new StandardTileData();
                //Foreground data
                data.Title = "TimeLogger";
                data.BackgroundImage = new Uri("/livetile.jpg", UriKind.Relative);
                data.Count = 0;
                data.BackTitle = "TimeLogger";
                data.BackContent = "Capture>>";
                ShellTile.Create(new Uri("/livetile.xaml?Title=TimeLogger", UriKind.Relative), data);
                //TileToggle.Text = "Unpin quick entry tile";
            }
            else
            {
                Tile.Delete();
                //TileToggle.Text = "Pin quick entry tile";
            }
        }
        
        //Database
        [Table]
        public class DBRow : INotifyPropertyChanged, INotifyPropertyChanging
        {
            //Date entry
            private string _date;
            [Column(IsPrimaryKey = true, DbType = "NVarChar(10) NOT NULL", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
            public string Date
            {
               get
               {
                   return _date;
               }
               set
               {
                   if(_date != value)
                   {
                       NotifyPropertyChanging("Date");
                       _date = value;
                       NotifyPropertyChanged("Date");
                   }
               }
            }

            //Time In entry
            private string _timein;
            [Column]
            public string TimeIn
            {
                get 
                {
                    return _timein;
                }
                set
                {
                    if (_timein != value)
                    {
                        NotifyPropertyChanging("TimeIn");
                        _timein = value;
                        NotifyPropertyChanged("TimeIn");
                    }
                }
            }

            //Time Out entry
            private string _timeout;
            [Column]
            public string TimeOut
            {
                get
                {
                    return _timeout;
                }
                set
                {
                    if (_timeout != value)
                    {
                        NotifyPropertyChanging("TimeOut");
                        _timeout = value;
                        NotifyPropertyChanged("TimeOut");
                    }
                }
            }

            //Lunch in entry
            private string _lunchin;
            [Column]
            public string LunchIn
            {
                get
                {
                    return _lunchin;
                }
                set
                {
                    if (_lunchin != value)
                    {
                        NotifyPropertyChanging("LunchIn");
                        _lunchin = value;
                        NotifyPropertyChanged("LunchIn");
                    }
                }
            }

            //Lunch Out entry
            private string _lunchout;
            [Column]
            public string LunchOut
            {
                get
                {
                    return _lunchout;
                }
                set
                {
                    if (_lunchout != value)
                    {
                        NotifyPropertyChanging("LunchOut");
                        _lunchout = value;
                        NotifyPropertyChanged("LunchOut");
                    }
                }
            }

            //Clocked time entry
            private string _clocked;
            [Column]
            public string ClockedTime
            {
                get
                {
                    return _clocked;
                }
                set
                {
                    if (_clocked != value)
                    {
                        NotifyPropertyChanging("ClockedTime");
                        _clocked = value;
                        NotifyPropertyChanged("ClockedTime");
                    }
                }
            }

            //[Column(IsVersion = true)]
            //public Binary _version;

            //Property changed event handler interface
            #region INotifyPropertyChanged Members
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string property)
            {
                if(PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(property));
                }
            }
            #endregion

            //Property changing event handler interface
            #region INotifyPropertyChanging Members
            public event PropertyChangingEventHandler PropertyChanging;

            private void NotifyPropertyChanging(string property)
            {
                if(PropertyChanging != null)
                {
                    PropertyChanging(this, new PropertyChangingEventArgs(property));
                }
            }
            #endregion
        }

        //Gets the row against the date from the database
        public DBRow QueryRowInDb(string date)
        {
            var query = (from row in database.logentries where row.Date == date select row).SingleOrDefault();
            return (DBRow)query;
        }

        //Creates a row against the date to database
        public void AddRowToDb(string date, string tin = "00:00", string tout = "00:00", string lin = "00:00", string lout = "00:00")
        {
            //Create a default entry
            DBRow tempp = new DBRow()
            {
                Date = date,
                TimeIn = tin,
                TimeOut = tout,
                LunchIn = lin,
                LunchOut = lout,
                ClockedTime = "00:00"
                //_version = new System.Data.Linq.Binary(bytes)
            };

            //Remove all unknown changes
            //RefreshAndDiscardDbChanges();

            //Insert the new row
            ChangeSet cs = database.GetChangeSet();
            
            database.logentries.InsertOnSubmit(tempp);
            database.SubmitChanges();
            //Check if successful!
            var readback = QueryRowInDb(date);

            if (readback == null)
            {
                OutOfMemoryException Expc = new OutOfMemoryException("Entry could not be made into the timelog database");
            } 
        }

        //Update the already present entry in the database
        public void UpdateRowInDb(DBRow newrow)
        {
            ChangeSet cs = database.GetChangeSet(); //debug
            DBRow query = QueryRowInDb(newrow.Date);
            
            query.TimeIn = newrow.TimeIn;
            query.TimeOut = newrow.TimeOut;
            query.LunchIn = newrow.LunchIn;
            query.LunchOut = newrow.LunchOut;
            query.ClockedTime = newrow.ClockedTime;

            cs = database.GetChangeSet(); //debug
            database.SubmitChanges();

            cs = database.GetChangeSet(); //debug
        }

        //Delete row from the database
        public void DeleteRowInDb(string date)
        {
            DBRow query = QueryRowInDb(date);
            if (query != null)
            {
                database.logentries.DeleteOnSubmit(query);
                database.SubmitChanges();
            }
        }

        //Discard all updates to the database
        public void RefreshAndDiscardDbChanges()
        {
            foreach (DBRow row in database.GetChangeSet().Updates)
            {
                database.Refresh(RefreshMode.OverwriteCurrentValues, row);
            }
        }

        //Set values - Prevents Content Panel changes from causing database updates
        public void SetDisplayDB(DBRow query)
        {
            DisplayDB.selectedEntry.Date = query.Date;
            DisplayDB.selectedEntry.TimeIn = query.TimeIn;
            DisplayDB.selectedEntry.TimeOut = query.TimeOut;
            DisplayDB.selectedEntry.LunchIn = query.LunchIn;
            DisplayDB.selectedEntry.LunchOut = query.LunchOut;
            DisplayDB.selectedEntry.ClockedTime = query.ClockedTime;
        }

        public bool HaveEntriesChanged(string date)
        {
            var query = QueryRowInDb(date);

            if ((query == null) ||
               ((query.TimeIn.CompareTo(DisplayDB.selectedEntry.TimeIn) == 0) &&
               (query.TimeOut.CompareTo(DisplayDB.selectedEntry.TimeOut) == 0) &&
               (query.LunchIn.CompareTo(DisplayDB.selectedEntry.LunchIn) == 0) &&
               (query.LunchOut.CompareTo(DisplayDB.selectedEntry.LunchOut) == 0)))
            {
                return false;
            }

            return true;
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            //Throw a dialog to save or discard and save fields if needed
            if (HaveEntriesChanged(DisplayDB.selectedEntry.Date))
            {
                if (MessageBox.Show("Save it now?", "Unsaved Entry", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    Save_Click(null, null);
                }
            }

            base.OnBackKeyPress(e);
        }

        public void Save_Click(object sender, EventArgs e)
        {
            DBRow query = QueryRowInDb(DisplayDB.selectedEntry.Date);
            //Update row
            if (query == null)
            {
                //Add an empty row against the new date
                AddRowToDb(DisplayDB.selectedEntry.Date, DisplayDB.selectedEntry.TimeIn, DisplayDB.selectedEntry.TimeOut, DisplayDB.selectedEntry.LunchIn, DisplayDB.selectedEntry.LunchOut);
            }
            else
            {
                UpdateRowInDb(DisplayDB.selectedEntry);
            }
        }

        public void Delete_Click(object sender, EventArgs e)
        {
            //Prompt the user
            if (MessageBox.Show("Are you sure?", "Delete Entry", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                //Delete entry
                DeleteRowInDb(DisplayDB.selectedEntry.Date);

                //Update display
                string today = GetddmmyyyyStoredCulture(DateTime.Now);
                UpdateLastLoadedDate(DateTime.Now);
                DisplayDB.selectedEntry.Date = today;
                UpdateClockedTime();
            }
        }

        private string SelectClosestAvailableEntry(string date)
        {
            return "crap";
        }

        private void Export_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/ExportPage.xaml", UriKind.Relative));
        }

        private void DatePicker_ValueChanged(object sender, DateTimeValueChangedEventArgs e)
        {
            //DateTime arg = e.NewDateTime ?? e.OldDateTime ?? DateTime.Now;
            if ((e.OldDateTime != null)  && (e.NewDateTime != null))
            {
                if (QueryRowInDb(GetddmmyyyyStoredCulture((DateTime)e.OldDateTime)) != null)
                {
                    if (HaveEntriesChanged(GetddmmyyyyStoredCulture((DateTime)e.OldDateTime)))
                    {
                        if (MessageBox.Show("Save it now?", "Unsaved Entry", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            Save_Click(sender, e);
                        }
                    }
                }

                string newdate = GetddmmyyyyStoredCulture((DateTime)e.NewDateTime);
                UpdateLastLoadedDate((DateTime)e.NewDateTime);
                var query = QueryRowInDb(newdate);

                //Load from database
                if (query != null)
                {
                    SetDisplayDB(query);
                }
                else
                {
                    ChangeSet cs = database.GetChangeSet();
                    //Add an entry
                    AddRowToDb(newdate);
                    query = QueryRowInDb(newdate);
                    SetDisplayDB(query);
                }
             }
        }

        //Handle flick on the screen
        private void Flick_handler(object sender, FlickGestureEventArgs e)
        {
            if (e.HorizontalVelocity > FLICKVELOCITY)
            {
                
            }
            else if (e.HorizontalVelocity < -FLICKVELOCITY)
            {
                NavigationService.Navigate(new Uri("/ExportPage.xaml", UriKind.Relative));
            }
        }

        private void NavigateToAbout(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void TimeIn_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DisplayDB.selectedEntry.TimeIn = GethhmmStoredCulture(DateTime.Now);
        }

        private void LunchIn_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DisplayDB.selectedEntry.LunchIn = GethhmmStoredCulture(DateTime.Now);
        }

        private void LunchOut_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DisplayDB.selectedEntry.LunchOut = GethhmmStoredCulture(DateTime.Now);
        }

        private void TimeOut_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DisplayDB.selectedEntry.TimeOut = GethhmmStoredCulture(DateTime.Now);
        }

        private void PinTile_Click(object sender, EventArgs e)
        {
            ToggleSecondaryTile();
        }

    }

}

namespace Converters
{
    //String to DateTime and vice-versa
    public class StringToDate : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture1)
        {
            try
            {
                //Always parse with the stored culture
                DateTime ret = DateTime.Parse((string)value, Timelog.MainPage.culture);
                return ret;
            }

            catch
            {
                return "01/01/1999";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture1)
        {
            try
            {
                DateTime date = (DateTime)value;
                string str = date.ToString(Timelog.MainPage.culture);
                str = str.Substring(0, str.IndexOf(' '));
                return str;
            }

            catch
            {
                return "01/01/1999";
            }
        }
    }


    //String to Time and vice-versa
    public class StringToTime : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture1)
        {
            try
            {
                //Always parse with the stored culture
                DateTime ret = DateTime.Parse((string)value, Timelog.MainPage.culture);
                return ret;
            }

            catch
            {
                return "00:07";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture1)
        {
            try
            {
                DateTime time = (DateTime)value;
                string str = time.ToString(Timelog.MainPage.culture);
                str = str.Substring(str.IndexOf(' ')+1,5);
                return str;
            }

            catch
            {
                return "00:07";
            }
        }
    }
}