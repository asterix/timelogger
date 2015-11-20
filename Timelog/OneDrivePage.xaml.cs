using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Live;
using Microsoft.Live.Controls;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Threading;

namespace Timelog
{
    public partial class OneDrivePage : PhoneApplicationPage
    {
        public OneDrivePage()
        {
            InitializeComponent();

            //Start progress bar
            performanceProgressBar.IsIndeterminate = true;
        }

        private LiveConnectClient client;
        private static bool LoginStatus = false;
        public static int FileIndex = 0;
        private IsolatedStorageFileStream fileStream = null;

        //Execute on opening the page
        /*
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }*/

        private void OnBackKeyPress()
        {
            NavigationService.GoBack();
        }

        private void OneDriveSessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {
            if (e != null && e.Status == LiveConnectSessionStatus.Connected)
            {
                this.client = new LiveConnectClient(e.Session);
                infoTextBlock.Text = "    Logged into OneDrive!";
                client.UploadCompleted += new EventHandler<LiveOperationCompletedEventArgs>(Upload_Completed);
                //client.UploadProgressChanged 
                LoginStatus = true;
            }
            else if (e != null && e.Status == LiveConnectSessionStatus.NotConnected)
            {
                this.client = null;
                LoginStatus = false;
                infoTextBlock.Text = "          Not logged in!";
            }
            else
            {
                this.client = null;
                LoginStatus = false;
                infoTextBlock.Text = "          Not logged in!";

                /*if (e.Error != null)
                {
                    Error.Exception = e.Error;
                    NavigationService.Navigate(new Uri("/Error.xaml", UriKind.Relative));
                }*/
            }


            //Stop progress bar
            performanceProgressBar.IsIndeterminate = false;
        }

        private void upsky_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            uploadOneFile(GetNextFileToUpload());
        }

        //Upload a file
        private void uploadOneFile(string FileName)
        {
            if (LoginStatus == true)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    //Start progress bar
                    performanceProgressBar.IsIndeterminate = true;

                    fileStream = null;
                    fileStream = store.OpenFile(FileName, FileMode.Open, FileAccess.Read);
                    try
                    {
                        client.UploadAsync("me/SkyDrive", FileName, fileStream, OverwriteOption.Overwrite);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Sign into OneDrive first!");
            }
        }

        private string GetNextFileToUpload()
        {
            string FileName = String.Empty;
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();

            while (FileName == String.Empty)
            {
                //Choose the file
                FileName = ExportPage.ExportFileName;
                switch (FileIndex)
                {
                    case 0:
                        FileName += ".txt";
                        break;
                    case 1:
                        FileName += ".csv";
                        break;
                    case 2:
                        FileName += ".xlsx";
                        break;
                    default:
                        FileIndex = -1;
                        break;
                }


                if (!store.FileExists(FileName))
                {
                    FileName = String.Empty;

                    if (FileIndex == -1)
                    {
                       FileName = "endOfFiles";
                    }
                }

                FileIndex++;
            }

            return FileName;
        }

        void Upload_Completed(object sender, LiveOperationCompletedEventArgs e)
        {
            string FileName = String.Empty;
            if (e.Error == null)
            {
                MessageBox.Show("Uploaded a file successfully!");
            }
            else
            {
                MessageBox.Show("Upload failure!");
                Error.Exception = e.Error;
                NavigationService.Navigate(new Uri("/Error.xaml", UriKind.Relative));
            }

            //Close the old file stream
            fileStream.Close();

            //Get the new file
            FileName = GetNextFileToUpload();

            if (FileName.CompareTo("endOfFiles") == 0)
            {
                //Disable progress bar
                performanceProgressBar.IsIndeterminate = false;
            }
            else
            {
                uploadOneFile(FileName);
            }
        }
    }
}