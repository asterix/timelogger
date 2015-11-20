using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO.IsolatedStorage;
using System.IO;

namespace Timelog
{
    public partial class Error : PhoneApplicationPage
    {
        public Error()
        {
            InitializeComponent();
        }

        public static Exception Exception;

        public static string catcher;
        

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string FileName = "error_log.txt";

            //Display on the console
            ErrorText.Text = Exception.ToString();

            IsolatedStorageFile errorfile = IsolatedStorageFile.GetUserStoreForApplication();
            
            errorfile.DeleteFile(FileName);
            //IsolatedStorageFileStream fileStream = errorfile.CreateFile(FileName);
            //fileStream.Close();
            System.IO.StreamWriter stream = new System.IO.StreamWriter(new IsolatedStorageFileStream(FileName, System.IO.FileMode.OpenOrCreate, errorfile));
            stream.Write(Exception.ToString());
            stream.Close();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            //Remove all back entries - Back press will exit application
            base.OnBackKeyPress(e);
        }
    }
}