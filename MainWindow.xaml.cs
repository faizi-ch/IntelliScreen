using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Layout.Core;
using DevExpress.XtraPrinting.Native;
using Hardcodet.Wpf.TaskbarNotification;
using IntelliScreen.Views;
using Microsoft.Win32;
using DevExpress.Xpf.WindowsUI;
using System.Data.SQLite;
using System.Timers;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core.Native;

namespace IntelliScreen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow
    {
        WinEventDelegate dele = null;
        private OpenFileDialog openFileDialog;
        private int defaultBrightness = 50;
        private string[] selectedProgram;
        private List<string> programs;
        private List<string> programsTitleList;
        private List<byte> brightnesses;
        private List<string> directoriesList;
        //private string[] programs;
        //private byte[] brightnesses;
        private int programsCount = 0;
        private string closedPanelName = "";
        private string closedPanelCaption = "";

        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private Timer timer;
        private bool isMouseUp = false;
        private SQLiteConnection sql_con;
        private SQLiteCommand sql_cmd;
        private SQLiteDataAdapter DB;
        
        public MainWindow()
        {
            InitializeComponent();

            programs = new List<string>();
            brightnesses = new List<byte>();
            programsTitleList = new List<string>();
            directoriesList = new List<string>();
            //defaultBrightness = GetBrightness();

            dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0,
                WINEVENT_OUTOFCONTEXT);


        }

        private void SetConnection()
        {
            sql_con =
                new SQLiteConnection("Data Source=|DataDirectory|Intelliscreen_DB.db;Version=3;New=False;Compress=True;");
        }

        private void ExecuteQuery(string txtQuery)
        {
            SetConnection();
            sql_con.Open();
            sql_cmd = sql_con.CreateCommand();
            sql_cmd.CommandText = txtQuery;
            sql_cmd.ExecuteNonQuery();
            sql_con.Close();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddFlyout.IsOpen = true;
            AddFlyout.PlacementTarget = sender as UIElement;
            
        }

        private void SelectProgram_OnClick(object sender, RoutedEventArgs e)
        {
            openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable Program File (*.exe)|*.exe";
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName=="")
            {
                e.Handled = false;
                return;}
            //selectedProgram = null;
            selectedProgram = openFileDialog.SafeFileName.Split('.');
            SelectProgram.Content = selectedProgram[0];


        }

        private void AddProgramFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (openFileDialog.FileName == "")
            {
                e.Handled = false;
                WinUIMessageBox.Show(null, "No program is selected!", "ERROR", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
               
            }
            AddFlyout.IsOpen = false;
            /*try
            {
                ProgramsComboBox.Items.Add(selectedProgram[0] + " - " + BrightnessTrackBar.Value);
                ProgramsComboBox.RefreshData();
            }
            catch (NullReferenceException )
            {
                WinUIMessageBox.Show(null, "No program is selected!", "ERROR", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }*/
            
            //SetBrightness((byte)defaultBrightness);
            programs.Add(selectedProgram[0]);
            brightnesses.Add((byte) BrightnessTrackBar.Value);


            SelectProgram.Content = "Select Program";

            LayoutPanel layoutPanel = new LayoutPanel();
            TrackBarEdit trackBarEdit = new TrackBarEdit();

            layoutPanel.AllowRestore = true;layoutPanel.Content = trackBarEdit;
            trackBarEdit.Value = brightnesses[programsCount];
            trackBarEdit.EditValueChanged+=new EditValueChangedEventHandler(trackBarEdit_EditValueChanged);
            directoriesList.Add(openFileDialog.FileName);
            Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(openFileDialog.FileName);
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());


            layoutPanel.CaptionImage = imageSource;
            FileVersionInfo myFileVersionInfo =
                FileVersionInfo.GetVersionInfo(openFileDialog.FileName);
            programsTitleList.Add(myFileVersionInfo.FileDescription);

            layoutPanel.Caption = myFileVersionInfo.FileDescription; 
            DockLayoutManager.DockController.Dock(layoutPanel, LayoutGroup1, DockType.Top);
            programsCount += 1;
            openFileDialog.Reset();
            SetBrightness((byte) defaultBrightness);

        }
        private void trackBarEdit_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            LayoutPanel parentLayoutPanel = (LayoutPanel)(((FrameworkElement)((TrackBarEdit) sender).Parent).Parent);
            int parentIndex = programsTitleList.IndexOf(parentLayoutPanel.Caption.ToString());
            brightnesses[parentIndex] = Convert.ToByte(e.NewValue);
            //MessageBox.Show(e.NewValue.ToString());
        }
        private void CancelFlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            AddFlyout.IsOpen = false;
            SetBrightness((byte) defaultBrightness);
        }

        private void BrightnessTrackBar_EditValueChanging(object sender,
            DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            SetBrightness((byte) BrightnessTrackBar.Value);
        }


        static int GetBrightness()
        {
            //define scope (namespace)
            System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

            //define query
            System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightness");

            //output current brightness
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);

            System.Management.ManagementObjectCollection moc = mos.Get();

            //store result
            byte curBrightness = 0;
            foreach (System.Management.ManagementObject o in moc)
            {
                curBrightness = (byte) o.GetPropertyValue("CurrentBrightness");
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();

            return (int) curBrightness;
        }

        static void SetBrightness(byte targetBrightness)
        {
            //define scope (namespace)
            System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");

            //define query
            System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightnessMethods");

            //output current brightness
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);

            System.Management.ManagementObjectCollection moc = mos.Get();

            foreach (System.Management.ManagementObject o in moc)
            {
                o.InvokeMethod("WmiSetBrightness", new Object[] {UInt32.MaxValue, targetBrightness});
                    //note the reversed order - won't work otherwise!
                break; //only work on the first object
            }

            moc.Dispose();
            mos.Dispose();
        }

        delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread,
            uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            IntPtr handle = IntPtr.Zero;
            StringBuilder Buff = new StringBuilder(nChars);
            handle = GetForegroundWindow();
            

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
        static public System.Drawing.Color GetPixelColor(IntPtr hwnd, int x, int y)
        {
            IntPtr hdc = GetDC(hwnd);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(hwnd, hdc);
            System.Drawing.Color color = System.Drawing.Color.FromArgb((int)(pixel & 0x000000FF),
                            (int)(pixel & 0x0000FF00) >> 8,
                            (int)(pixel & 0x00FF0000) >> 16);
            return color;
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetSysColor(int nIndex);
        [DllImport("User32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);
        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (HeaderCheckEdit.IsChecked == true)
            {
                //Log.Text += GetActiveWindowTitle() + "\r\n";
                bool check = false;
                string prcs = "";
                uint pid;

                GetWindowThreadProcessId(hwnd, out pid);
                var pixelColor = GetPixelColor(hwnd, 0, 0);
                foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
                {
                    if (p.Id == pid)
                        prcs = p.ProcessName.ToString();
                }
                if (AutoBrightnessToggle.IsChecked == false)
                {
                    int i;
                    for (i = 0; i < programsCount && programsCount > 0; i++)
                    {
                        if (prcs.Contains(programs[i]))
                        {


                            check = true;
                            //MessageBox.Show(borderColor.R.ToString() + "\n" + borderColor.G.ToString() + "\n" + borderColor.B.ToString());

                            SetBrightness(brightnesses[i]);
                            break;

                        }
                    }
                    if (check)
                    {
                        SetBrightness(brightnesses[i]);
                    }
                    else
                    {
                        SetBrightness((byte)defaultBrightness);
                    }
                }
                else
                {
                    int color = (int)pixelColor.R + (int)pixelColor.G + (int)pixelColor.B;
                    double colorDarkPerc = (color / 765) * 100;
                    byte brightness = (byte)(100 - colorDarkPerc);
                    SetBrightness(brightness);

                }
            }
            else
            {
                return;
            }
            
            

        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private void AutoBrightnessToggle_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (AutoBrightnessToggle.IsChecked==true)
            {
                Add.IsEnabled = false;
                ProgramsComboBox.IsEnabled = false;
                AutoBrightnessContextMenu.IsChecked = true;
            }
            else
            {
                Add.IsEnabled = true;
                ProgramsComboBox.IsEnabled = true;
                AutoBrightnessContextMenu.IsChecked = false;
            }
        }

        private void DockLayoutManager_DockItemClosing(object sender, DevExpress.Xpf.Docking.Base.ItemCancelEventArgs e)
        {
            
            MessageBoxResult msgBoxResult = WinUIMessageBox.Show("Are you sure you want to remove "+e.Item.Caption+"?",
                                     "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (msgBoxResult==MessageBoxResult.Yes)
            {
                closedPanelName = e.Item.Name.ToString();
                closedPanelCaption = e.Item.Caption.ToString();
                //MessageBox.Show(closedPanelName);
                UndoRemovedFlyout.HorizontalAlignment=HorizontalAlignment.Center;
                FlyoutLabelItem.Label = e.Item.Caption+" is removed.  ";
                UndoRemovedFlyout.IsOpen = true;
                UndoRemovedFlyout.Opacity = 0.3;
                UndoRemovedFlyout.PlacementTarget = IntelliScreen as UIElement;
                dispatcherTimer =
                        new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += dispatcherTimer_Tick;
                dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
                dispatcherTimer.Start();

                

            }
            else
            {
                e.Cancel = true;
                return;
            }
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //if (!isMouseUp)
            //{
                UndoRemovedFlyout.IsOpen = false;
                UndoRemovedFlyout.Visibility = Visibility.Hidden;
            //}

            int removedProgramIndex = programsTitleList.IndexOf(closedPanelCaption);
            //MessageBox.Show(closedPanelName+"+\n+"+ removedProgramIndex.ToString());
            if (removedProgramIndex != -1)
            {
                programsTitleList.RemoveAt(removedProgramIndex);
                programs.RemoveAt(removedProgramIndex);
                brightnesses.RemoveAt(removedProgramIndex);
                directoriesList.RemoveAt(removedProgramIndex);
                programsCount--;
                if (programsCount < 0)
                    programsCount = 0;

            }
        }
        private void Timer_ElapsedEventHandler(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            UndoRemovedFlyout.IsOpen = false;
        }
        private void DXWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetConnection();
            SQLiteCommand sqLiteCommand = new SQLiteCommand("SELECT * FROM Programs",sql_con);
            SQLiteDataAdapter sqLiteDataAdapter=new SQLiteDataAdapter(sqLiteCommand);
            DataTable dataTable=new DataTable();
            sqLiteDataAdapter.Fill(dataTable);
            foreach (DataRow dr in dataTable.Rows)
            {
                programsTitleList.Add(dr["Title"].ToString());
                programs.Add(dr["ProcessName"].ToString());
                brightnesses.Add(Convert.ToByte(dr["Brightness"]));
                directoriesList.Add(dr["Directory"].ToString());
            }
            if (programsTitleList.Count>0)
            {
                programsCount = programsTitleList.Count - 1;}
            for (int i = 0; i < directoriesList.Count; i++)
            {
                LayoutPanel layoutPanel = new LayoutPanel();
                TrackBarEdit trackBarEdit = new TrackBarEdit();


                layoutPanel.Content = trackBarEdit;
                trackBarEdit.Value = brightnesses[i];
                trackBarEdit.EditValueChanged += new EditValueChangedEventHandler(trackBarEdit_EditValueChanged);
                
                Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(directoriesList[i]);
                ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());


                layoutPanel.CaptionImage = imageSource;
                FileVersionInfo myFileVersionInfo =
                    FileVersionInfo.GetVersionInfo(directoriesList[i]);
                layoutPanel.Caption = myFileVersionInfo.FileDescription;
                DockLayoutManager.DockController.Dock(layoutPanel, LayoutGroup1, DockType.Top);

                defaultBrightness = GetBrightness();
                DBrightnessTrackBar.Value = GetBrightness();
                DBrightnessContextMenu.Value = GetBrightness();
            }
        }
        private void DXWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult msgBoxResult = WinUIMessageBox.Show("Are you sure you want to exit?",
                                     "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (msgBoxResult == MessageBoxResult.Yes)
            {
                string query = "delete from Programs";
                ExecuteQuery(query);
                for (int i = 0; i < programs.Count; i++)
                {
                    string insertQuery = "INSERT INTO Programs (Id, Title, ProcessName, Brightness, Directory) VALUES('" + i +
                                         "', '" + programsTitleList[i] + "','" + programs[i] + "','" + brightnesses[i] +
                                         "','" + directoriesList[i] + "')";
                    ExecuteQuery(insertQuery);
                }
                TaskbarIcon.Dispose();
                Application.Current.Shutdown();

            }
            else
            {
                e.Cancel = true;
                return;
            }
            
        }

        

        private void DXWindow_StateChanged(object sender, EventArgs e)
        {
            
            
            if (this.WindowState == WindowState.Minimized)
            {
                
                TaskbarIcon.IsEnabled = true;
                TaskbarIcon.Visibility=Visibility.Visible;
                this.Visibility = Visibility.Hidden;
                //Icon myIcon = new Icon("Resources/NotifyIcon.ico");
                TaskbarIcon.ShowBalloonTip("IntelliScreen", "Application is running...", BalloonIcon.None);
            }
            if (this.WindowState == WindowState.Normal || this.WindowState == WindowState.Maximized)
            {
                
                this.Visibility = Visibility.Visible;
            }
        }

        private void TaskbarIcon_OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Visible;
            this.Show();
            this.ShowActivated = true;
            this.WindowState=WindowState.Normal;
        }

        private void ExitContextMenuButton_Click(object sender, RoutedEventArgs e)
        {
            DXWindow_Closing(sender, new CancelEventArgs());
            }

        private void AutoBrightnessContextMenu_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (AutoBrightnessContextMenu.IsChecked==true)
            {
                AutoBrightnessToggle.IsChecked = true;
            }
            else
            {
                AutoBrightnessToggle.IsChecked = false;
            }
            
            AutoBrightnessToggle_EditValueChanged(sender,e);
        }

        private void HeaderCheckEdit_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (HeaderCheckEdit.IsChecked == true)
            { 
                MainAppCheckMenu.IsChecked = true;
                TaskbarIcon.ShowBalloonTip("IntelliScreen", "Application is in running state...", BalloonIcon.None);
            }
            else
            {
                MainAppCheckMenu.IsChecked = false;
            }
        }

        private void DBrightnessTrackBar_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            defaultBrightness = (byte) DBrightnessTrackBar.Value;
            DBrightnessContextMenu.Value = DBrightnessTrackBar.Value;}

        private void DBrightnessContextMenu_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            DBrightnessTrackBar.Value = DBrightnessContextMenu.Value;
            DBrightnessTrackBar_EditValueChanged(sender,e);
        }

        private void AutoBrightnessContextMenu_CheckedChanged(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (AutoBrightnessContextMenu.IsChecked == true)
            {
                AutoBrightnessToggle.IsChecked = true;
            }
            else
            {
                AutoBrightnessToggle.IsChecked = false;
            }

            }

        private void UndoRemovedFlyout_MouseEnter(object sender, MouseEventArgs e)
        {
            dispatcherTimer.Stop();
            //UndoRemovedFlyout.IsOpen = true;
            //UndoRemovedFlyout.Visibility=Visibility.Visible;
            //UndoRemovedFlyout.StaysOpen = true;

        }

        private void UndoRemovedFlyout_MouseLeave(object sender, MouseEventArgs e)
        {
            dispatcherTimer =
                        new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        private void UndoRemovedFlyout_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dispatcherTimer.Stop();
            //isMouseUp = true;
            UndoRemovedFlyout.StaysOpen = true;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            string n = closedPanelName;
            DockLayoutManager.LayoutController.Restore(DockLayoutManager.ClosedPanels[closedPanelName]);
            DockLayoutManager.DockController.Restore(DockLayoutManager.ClosedPanels[closedPanelName]);
            TaskbarIcon.ShowBalloonTip("IntelliScreen", "Restored successfully!", BalloonIcon.None);
        }

        private void MainAppCheckMenu_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (MainAppCheckMenu.IsChecked == true)
            {
                HeaderCheckEdit.IsChecked = true;
            }
            else
            {
                HeaderCheckEdit.IsChecked = false;
            }
            HeaderCheckEdit_EditValueChanged(sender, e);
        }

        private void ThemeCheckEdit_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (ThemeCheckEdit.IsChecked == true)
            {
                DevExpress.Xpf.Core.ApplicationThemeHelper.ApplicationThemeName =
                    DevExpress.Xpf.Core.Theme.MetropolisLightName;
            }
            else
            {
                DevExpress.Xpf.Core.ApplicationThemeHelper.ApplicationThemeName =
                    DevExpress.Xpf.Core.Theme.MetropolisDarkName;
            }
        }
    }
}
