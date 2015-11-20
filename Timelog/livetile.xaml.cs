using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.Windows.Threading;

namespace Timelog
{
    public partial class livetile : PhoneApplicationPage
    {
        public static string LiveTileDBString = "Data Source=isostore:/livetile.sdf";
        private DispatcherTimer timer;

        public livetile()
        {
            InitializeComponent();

            //instantiate db
            livedb = new LiveTileDB(LiveTileDBString);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(4);
            timer.Tick += TimerExpiry;
        }

        //Execute on opening the page
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //DataContext for the ContentPanel-MainPage
            if (e.NavigationMode != NavigationMode.New)
            {

            }

            UpdateTile();
            timer.Start();
        }

        private void TimerExpiry(Object sender, EventArgs e)
        {
            timer.Stop();
            CancelEventArgs cc = new CancelEventArgs();
            cc.Cancel = true;

            OnBackKeyPress(cc);
        }

        private void UpdateTile()
        {
            LiveEntry entry = GetEntry();
            EntryState state = UpdateState(entry.state);
            string statestr = "";

            switch (state)
            {
                case EntryState.LOGGEDIN:
                    entry.timein = GethhmmStoredCulture(DateTime.Now);
                    statestr = "Lunch-In";
                    break;
                case EntryState.LUNCHIN:
                    entry.lunchin = GethhmmStoredCulture(DateTime.Now);
                    statestr = "Lunch-Out";
                    break;
                case EntryState.LUNCHOUT:
                    entry.lunchout = GethhmmStoredCulture(DateTime.Now);
                    statestr = "Exit";
                    break;
                case EntryState.EXIT:
                    entry.timeout = GethhmmStoredCulture(DateTime.Now);
                    statestr = "Time-In";
                    break;
                case EntryState.INIT:
                    statestr = "Time-In";
                    break;
            }

            entry.state = (int)state;
            UpdateLiveDB(entry);
            UpdateLiveTile(statestr, (int)state);
        }

        //Returns hh:mm (as in the stored culture) from the passed DateTime
        public string GethhmmStoredCulture(DateTime dat)
        {
            string today = dat.ToString(MainPage.culture);
            today = today.Substring(today.IndexOf(' ') + 1, 5); //Take just hh:mm
            return today;
        }

        private void UpdateLiveDB(LiveEntry entry)
        {
            LiveEntry query = (from row in livedb.entry where row.key == "mainkey" select row).SingleOrDefault();
            query = entry;
            livedb.SubmitChanges();
        }

        private LiveEntry GetEntry()
        {
            LiveEntry query = (from row in livedb.entry where row.key == "mainkey" select row).SingleOrDefault();
            if (query == null)
            {
                LiveEntry first = new LiveEntry();
                first.date = "00.00.1234";
                first.timein = "00:00";
                first.timeout = "00:00";
                first.lunchin = "00:00";
                first.lunchout = "00:00";
                first.state = 0;
                first.key = "mainkey";

                livedb.entry.InsertOnSubmit(first);
                livedb.SubmitChanges();

                query = (from row in livedb.entry where row.key == "mainkey" select row).SingleOrDefault();
            }

            return query;
        }

        //Data base context class -> StorageDB
        public class LiveTileDB : DataContext
        {
            public LiveTileDB(string dbnamestring)
                : base(dbnamestring)
            {
                //Nothing here 
            }

            public Table<LiveEntry> entry;
        }

        private void UpdateLiveTile(string update, int count)
        {
            //Create and pin the tile
            ShellTile Tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("Title=TimeLogger"));

            if (Tile != null)
            {
                StandardTileData data = new StandardTileData();
                //Foreground data
                data.Title = "TimeLogger";
                data.BackgroundImage = new Uri("/livetile.jpg", UriKind.Relative);
                data.Count = count;
                data.BackTitle = "TimeLogger";
                data.BackContent = "Capture>>"+update;
                Tile.Update(data);
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
        }

        private LiveTileDB livedb;

        public enum EntryState
        {
            INIT = 0,
            LOGGEDIN,
            LUNCHIN,
            LUNCHOUT,
            EXIT
        }

        [Table]
        public class LiveEntry
        {
            private string _date;
            [Column]
            public string date { get { return _date; } set { if (value != _date) _date = value; } }

            private string _timein;
            [Column]
            public string timein { get { return _timein; } set { if (value != _timein) _timein = value; } }

            private string _lunchin;
            [Column]
            public string lunchin { get { return _lunchin; } set { if (value != _lunchin) _lunchin = value; } }

            private string _lunchout;
            [Column]
            public string lunchout { get { return _lunchout; } set { if (value != _lunchout) _lunchout = value; } }

            private string _timeout;
            [Column]
            public string timeout { get { return _timeout; } set { if (value != _timeout) _timeout = value; } }

            private int _state;
            [Column]
            public int state { get { return _state; } set { if (value != _state) _state = value; } }

            private string _key;
            [Column(IsPrimaryKey = true, DbType = "NVarChar(10) NOT NULL", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
            public string key { get { return _key; } set { if (value != _key) _key = value; } }
        };

        //This method implements the state machine cycle for punching
        private EntryState UpdateState(int state)
        {
            state++;

            if (state > (int)EntryState.EXIT)
            {
                state = (int)EntryState.INIT;
            }

            return (EntryState)state;
        }
    }
}