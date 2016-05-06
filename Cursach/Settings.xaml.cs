using System;
using System.Windows.Media;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace Cursach
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private PhysicalLayer.ComHandler comHandler;
        private DatalinkLayer.CanalHandler canalHandler;

        private FileStream sFile;
        private FileStream rFile;

        public Settings()
        { 
            comHandler = new PhysicalLayer.ComHandler();
            canalHandler = new DatalinkLayer.CanalHandler();
            canalHandler.FormsManager = this;
            comHandler.FormsManager = this;
            canalHandler.ComManager = comHandler;
            comHandler.CanalManager = canalHandler;
            InitializeComponent();
        }

        private void WinSettings_Loaded(object sender, RoutedEventArgs e)
        {
            string[] ports;
            ports = PhysicalLayer.ComHandler.GetSortedPortNames();
            cmbCOM.Items.Clear();
            foreach (string port in ports)
            {
                cmbCOM.Items.Add(port);
                cmbCOM.SelectedItem = cmbCOM.Items[0];
            }

            if (ports.Length == 0)
            {
                // Configure the message box to be displayed
                string messageBoxText = "COM-порты отсутствуют.\n Завершение программы.";
                string caption = "File Transfer";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                Close();

                // Display message box
                MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

        private void butOK_Click(object sender, RoutedEventArgs e)
        {
            //butConnect.IsEnabled = false;

            PhysicalLayer.PortState portState = comHandler.OpenCom(
                cmbCOM.Text,
                cmbBaud.Text,
                cmbParity.Text,
                cmbDataBits.Text,
                cmbStopBits.Text);

            switch (portState)
            {
                case PhysicalLayer.PortState.Opened:
                    lblStatus.Foreground = Brushes.Black;
                    lblStatus.Content = "Порт открыт";
                    //butConnect.IsEnabled = true;
                    MessageBox.Show("Порт успешно настроен");
                    break;
                case PhysicalLayer.PortState.Occupied:
                    lblStatus.Foreground = Brushes.Red;
                    lblStatus.Content = "Выбранный порт занят";
                    break;
                case PhysicalLayer.PortState.InvalidArgs:
                    lblStatus.Foreground = Brushes.Red;
                    lblStatus.Content = "Неправильно заданы параметры";
                    break;
                case PhysicalLayer.PortState.Error:
                    lblStatus.Foreground = Brushes.Red;
                    lblStatus.Content = "Ошибка";
                    break;
            }
        }

        //internal void ConnectFail()
        //{
        //    comHandler.CloseCom();
        //    Dispatcher.Invoke(new Action(() =>
        //    {
        //        lblStatus.Foreground = Brushes.Red;
        //        lblStatus.Content = "Порт не открыт";
        //        MessageBox.Show("Не удалось соединиться с другим компьютером");
        //    }));
        //}

        //private void butConnect_Click(object sender, RoutedEventArgs e)
        //{
        //    lblStatus.Foreground = Brushes.Black;
        //    lblStatus.Content = "Соединение с другим компьютером...";
        //    canalHandler.Connect();
        //}

        internal void ConnectBroke()
        {
            CloseFiles();
            comHandler.CloseCom();
            Dispatcher.Invoke(new Action(() =>
            {
                //butConnect.IsEnabled = false;
                lblStatus.Foreground = Brushes.Red;
                lblStatus.Content = "Порт не открыт";
                MessageBox.Show("Соединение с другим компьютером прервано");
            }));
        }

        private void butBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                tbName.Text = dlg.FileName;
            }
        }

        private void butSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sFile = new FileStream(tbName.Text, FileMode.Open);
            }
            catch
            {
                MessageBox.Show("Ошибка при открытии файла");
            }
            string fileName = Path.GetFileName(tbName.Text);
            canalHandler.SendFile(fileName);
        }

        private void tbName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbName.Text != string.Empty)
            {
                butSend.IsEnabled = true;
            }
            else
            {
                butSend.IsEnabled = false;
            }
        }

        internal void SavePrompt(string fileName)
        {
            MessageBoxResult result = MessageBoxResult.None;
            Dispatcher.Invoke(new Action(() =>
            {
                result = MessageBox.Show("Принять файл " + fileName + "?", "Сообщение", MessageBoxButton.YesNo);
            }));
            if (result == MessageBoxResult.Yes)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.FileName = fileName;
                    dlg.Filter = "All Files|*.*";
                    bool? dialogResult = dlg.ShowDialog();
                    if (dialogResult == true)
                    {
                        rFile = new FileStream(dlg.FileName, FileMode.Create);
                        canalHandler.RecieveFile();
                    }
                    else
                    {
                        canalHandler.Abort();
                    }
                }));
            }
            else
            {
                canalHandler.Abort();
            }
        }

        internal void TransmissionCancel()
        {
            sFile.Dispose();
            sFile = null;
            Dispatcher.Invoke(new Action(() => MessageBox.Show("Передача файла была отменена")));
        }

        private void CloseFiles()
        {
            if (sFile != null && rFile != null)
            {
                sFile.Dispose();
                rFile.Dispose();
            }
            else if (rFile != null)
            {
                rFile.Dispose();
            }
            else if (sFile != null)
            {
                sFile.Dispose();
            }
        }

        internal void RecieveSuccess()
        {
            rFile.Dispose();
            rFile = null;
            Dispatcher.Invoke(new Action(() => MessageBox.Show("Файл успешно принят")));
        }

        internal void SendSuccess()
        {
            sFile.Dispose();
            sFile = null;
            Dispatcher.Invoke(new Action(() => MessageBox.Show("Файл успешно отправлен")));
        }

        internal void ConnectSuccess()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                //butConnect.IsEnabled = false;
                lblStatus.Foreground = Brushes.Green;
                lblStatus.Content = "Соединение установлено";
                MessageBox.Show("Соединение с другим компьютером установлено");
            }));
        }

        internal void WriteToFile(byte[] frame)
        {
            rFile.Write(frame, 1, frame.Length - 1);
        }

        internal int ReadFromFile(byte[] frame)
        {
            int bytesRead = sFile.Read(frame, 1, frame.Length - 1);
            return bytesRead;
        }

        private void WinSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sFile != null || rFile != null)
            {
                MessageBoxResult result = MessageBox.Show("Программа в данный момент занята. Вы действительно хотите выйти из программы?", "Внимание", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        CloseFiles();
                        comHandler.CloseCom();
                        break;
                    case MessageBoxResult.No:
                        e.Cancel = true;
                        break;
                }
            }
            else
            {
                comHandler.CloseCom();
            }
        }

        //public void ShowMessage(string msg)
        //{
        //    string caption = "File Transfer";
        //    MessageBoxButton button = MessageBoxButton.OK;
        //    MessageBoxImage icon = MessageBoxImage.Information;
        //    MessageBox.Show(msg, caption, button, icon);
        //}
    }
}
