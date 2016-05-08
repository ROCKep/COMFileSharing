using System;
using System.Windows.Media;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Threading;

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

        private DispatcherTimer timer;

        public Settings()
        { 
            comHandler = new PhysicalLayer.ComHandler();
            canalHandler = new DatalinkLayer.CanalHandler();
            canalHandler.FormsManager = this;
            comHandler.FormsManager = this;
            canalHandler.ComManager = comHandler;
            comHandler.CanalManager = canalHandler;
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0,0,0,0,500);
            InitializeComponent();
        }

        ~Settings()
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
            }
            CloseFiles();
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
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

        private void butOK_Click(object sender, RoutedEventArgs e)
        {
            PhysicalLayer.PortState portState = comHandler.OpenCom(
                cmbCOM.Text,
                cmbBaud.Text,
                cmbParity.Text,
                cmbDataBits.Text,
                cmbStopBits.Text);

            switch (portState)
            {
                case PhysicalLayer.PortState.Connected:
                    lblStatus.Foreground = Brushes.Green;
                    lblStatus.Content = "Соединение установлено";
                    gbxChooseFile.IsEnabled = true;
                    MessageBox.Show("Соединение с другим компьютером установлено");
                    break;
                case PhysicalLayer.PortState.Opened:
                    lblStatus.Foreground = Brushes.Black;
                    lblStatus.Content = "Порт открыт";
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

        public void ConnectFail()
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
            }
            CloseFiles();
            comHandler.CloseCom();
            Dispatcher.Invoke(new Action(() =>
            {
                butOK.IsEnabled = true;
                lblStatus.Foreground = Brushes.Red;
                lblStatus.Content = "Порт не открыт";
                gbxChooseFile.IsEnabled = false;
                gbxSendProgress.Visibility = Visibility.Hidden;
                gbxReceiveProgress.Visibility = Visibility.Hidden;
                MessageBox.Show("Соединение с другим компьютером прервано");
            }));
        }

        public void ConnectBroke()
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
            }
            CloseFiles();
            comHandler.CloseCom();
            Dispatcher.Invoke(new Action(() =>
            {
                butOK.IsEnabled = true;
                lblStatus.Foreground = Brushes.Red;
                lblStatus.Content = "Порт не открыт";
                gbxChooseFile.IsEnabled = false;
                gbxSendProgress.Visibility = Visibility.Hidden;
                gbxReceiveProgress.Visibility = Visibility.Hidden;
                MessageBox.Show("Обрыв соединения");
            }));
        }

        public void ConnectSuccess()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                lblStatus.Foreground = Brushes.Green;
                lblStatus.Content = "Соединение установлено";
                gbxChooseFile.IsEnabled = true;
                MessageBox.Show("Соединение с другим компьютером установлено");
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
                string fileName = Path.GetFileName(tbName.Text);
                gbxChooseFile.IsEnabled = false;
                butOK.IsEnabled = false;
                gbxSendProgress.Visibility = Visibility.Visible;
                lblSend.Content = "Передача файла " + Path.GetFileName(sFile.Name) + ":";
                pbrSendProgress.Maximum = sFile.Length;
                pbrSendProgress.Value = 0.0;
                canalHandler.SendFile(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

        public void SavePrompt(string fileName)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBoxResult result = MessageBox.Show("Принять файл " + fileName + "?", "Сообщение", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    SaveFile(fileName);
                }
                else
                {
                    canalHandler.Abort();
                }
            }));
        }

        private void SaveFile(string fileName)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = fileName;
            dlg.Filter = "All Files|*.*";
            bool? dialogResult = dlg.ShowDialog();
            if (dialogResult == true)
            {
                try
                {
                    rFile = new FileStream(dlg.FileName, FileMode.Create);
                    butOK.IsEnabled = false;
                    gbxReceiveProgress.Visibility = Visibility.Visible;
                    lblReceive.Content = "Прием файла " + Path.GetFileName(rFile.Name) + ":";
                    lblReceiveProgress.Content = "Принято 0 КБ";
                    if (!timer.IsEnabled)
                    {
                        timer.Start();
                    }
                    canalHandler.Acknowledge();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    SaveFile(fileName);
                }
            }
            else
            {
                canalHandler.Abort();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lblReceiveProgress.Content = string.Format("Принято {0:f2} КБ", rFile.Length / 1024.0);
        }

        public void TransmissionCancel()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                sFile.Dispose();
                sFile = null;
                if (rFile == null)
                {
                    butOK.IsEnabled = true;
                }
                gbxChooseFile.IsEnabled = true;
                gbxSendProgress.Visibility = Visibility.Hidden;
                MessageBox.Show("Передача файла была отменена");
            }));
        }

        private void CloseFiles()
        {
            if (rFile != null)
            {
                rFile.Dispose();
                rFile = null;
            }
            if (sFile != null)
            {
                sFile.Dispose();
                sFile = null;
            }
        }

        public void ReceiveSuccess()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (timer.IsEnabled)
                {
                    timer.Stop();
                }
                rFile.Dispose();
                rFile = null;
                if (sFile == null)
                {
                    butOK.IsEnabled = true;
                }
                gbxReceiveProgress.Visibility = Visibility.Hidden;
                MessageBox.Show("Файл успешно принят");
            }));
        }

        public void SendSuccess()
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                sFile.Dispose();
                sFile = null;
                if (rFile == null)
                {
                    butOK.IsEnabled = true;
                }
                gbxChooseFile.IsEnabled = true;
                gbxSendProgress.Visibility = Visibility.Hidden;
                MessageBox.Show("Файл успешно отправлен");
            }));
        }

        public void WriteToFile(byte[] frame)
        {
            rFile.Write(frame, 0, frame.Length);
        }

        public int ReadFromFile(byte[] frame)
        {
            int bytesRead = sFile.Read(frame, 0, frame.Length);
            Dispatcher.Invoke(new Action(() =>
            {
                pbrSendProgress.Value += bytesRead;
            }));
            return bytesRead;
        }

        private void WinSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sFile != null || rFile != null)
            {
                MessageBoxResult result = MessageBox.Show("Программа в данный момент занята. Вы действительно хотите выйти из программы?", "Внимание", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
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
