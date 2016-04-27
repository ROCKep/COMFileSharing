using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cursach
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public PhysicalLayer.ComHandler ComManager { get; private set; }
        public CanalLayer.SFile SendFile { get; private set;  }
        public Settings()
        { 
            ComManager = new PhysicalLayer.ComHandler(SendFile);
            SendFile = new CanalLayer.SFile(ComManager);
            InitializeComponent();
        }

        public void ShowMessage(string msg)
        {
            string caption = "File Transfer";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            System.Windows.MessageBox.Show(msg, caption, button, icon);
        }

        private void butBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = String.Empty; // Default file extension
            dlg.Filter = String.Empty; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                tbName.Text = dlg.FileName;
            }
        }

        private void butOpenCom_Click(object sender, RoutedEventArgs e)
        {
            //ComManager.OpenCom(cmbCOM.Text, cmbBaud.Text,
            //     cmbParity.Text, cmbDataBits.Text,
            //     cmbStopBits.Text, ShowMessage);

            //if (ComManager.ComPort.IsOpen)
            //{
            //    butOpenCom.IsEnabled = false;
            //    butCloseCom.IsEnabled = true;
            //    cmbCOM.IsEnabled = false;
            //    cmbBaud.IsEnabled = false;
            //    cmbDataBits.IsEnabled = false;
            //    cmbStopBits.IsEnabled = false;
            //    cmbParity.IsEnabled = false;
            //}

            /*// Configure the message box to be displayed
            string messageBoxText = "Вы собираетесь открыть COM-порт?";
            string caption = "File Transer";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;

            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    // User pressed Yes button
                    // ...
                    break;
                case MessageBoxResult.No:
                    // User pressed No button
                    // ...
                    break;
            }*/

        }

        private void butCloseCom_Click(object sender, RoutedEventArgs e)
        {
            //ComManager.CloseCom(ShowMessage);
            //if (!ComManager.ComPort.IsOpen)
            //{
            //    butCloseCom.IsEnabled = false;
            //    butOpenCom.IsEnabled = true;
            //    cmbCOM.IsEnabled = true;
            //    cmbBaud.IsEnabled = true;
            //    cmbDataBits.IsEnabled = true;
            //    cmbStopBits.IsEnabled = true;
            //    cmbParity.IsEnabled = true;
            //}

            /*// Configure the message box to be displayed
            string messageBoxText = "Вы собираетесь закрыть COM-порт?";
            string caption = "File Transer";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;

            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    // User pressed Yes button
                    // ...
                    break;
                case MessageBoxResult.No:
                    // User pressed No button
                    // ...
                    break;
            }*/

        }

        private void butOK_Click(object sender, RoutedEventArgs e)
        {
            PhysicalLayer.PortState portstate = ComManager.SetupCom(
                    cmbCOM.Text, cmbBaud.Text,
                    cmbParity.Text, cmbDataBits.Text,
                    cmbStopBits.Text);

            if (portstate == PhysicalLayer.PortState.Opened)
            {
                MessageBox.Show("Порт успешно настроен бич");
            }
            else
            {
                MessageBox.Show("Какая то ошибка мне похуй");
            }
        }

        private void butSend_Click(object sender, RoutedEventArgs e)
        {
            this.SendFile.setSendPath(tbName.Text);
            this.SendFile.SendFile();
        }

        private void butChoose_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "Select the directory that you want to use as the default.";

            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
            folderBrowserDialog.ShowDialog();
            tbDist.Text = folderBrowserDialog.SelectedPath;

            //// Configure save file dialog box
            //Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            //dlg.FileName = "Document"; // Default file name
            //dlg.DefaultExt = ".text"; // Default file extension
            //dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            //// Show save file dialog box
            //Nullable<bool> result = dlg.ShowDialog();

            //// Process save file dialog box results
            //if (result == true)
            //{
            //    // Save document
            //    string filename = dlg.FileName;
            //}
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            this.SendFile.setReceivePath(tbDist.Text);
        }

        private void WinSettings_Loaded(object sender, RoutedEventArgs e)
        {
            string[] masCOM;
            masCOM = PhysicalLayer.ComHandler.GetSortedPortNames();
            cmbCOM.Items.Clear();
            foreach (string port in masCOM)
            {
                cmbCOM.Items.Add(port);
                cmbCOM.SelectedItem = cmbCOM.Items[0];
            }

            if (masCOM.Length == 0)
            {
                // Configure the message box to be displayed
                string messageBoxText = "COM-порты отсутствуют.\n Завершение программы.";
                string caption = "File Transfer";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                this.Close();

                // Display message box
                MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);
            }

            butCloseCom.IsEnabled = false;
            butSend.IsEnabled = false;
            butSave.IsEnabled = false;
            butCloseCom.Visibility = Visibility.Hidden;
            butOpenCom.Visibility = Visibility.Hidden;

        }
      
        private void tbName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbName.Text != String.Empty)
            {
                butSend.IsEnabled = true;
            }
            else
            {
                butSend.IsEnabled = false;
            }
        }

        private void tbDist_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbDist.Text != String.Empty)
            {
                butSave.IsEnabled = true;
            }
            else
            {
                butSave.IsEnabled = false;
            }
        }
    }
}
