using System;
using System.Windows.Media;
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
        //ссылки на другие уровни
        private PhysicalLayer.ComHandler comHandler;
        private DatalinkLayer.CanalHandler canalHandler;

        private FileStream sFile; // передаваемый файл
        private FileStream rFile; // принимаемый файл

        private DispatcherTimer timer; // таймер, обновляющий на форме количество принятых байт

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
            timer.Interval = new TimeSpan(0,0,0,0,500); // таймер вызывает функцию раз в 500 мс

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

        /// <summary>
        /// Обработчик события, возникающего при загрузке формы
        /// </summary>
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
                string messageBoxText = "COM-порты отсутствуют.\n Завершение программы";
                string caption = "Ошибка";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                Close();
                // Display message box
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

        /// <summary>
        /// Обработчик события, возникающего при нажатии на кнопку "ОК"
        /// </summary>
        private void butOK_Click(object sender, RoutedEventArgs e)
        {
            PhysicalLayer.PortState portState = canalHandler.Connect(
                cmbCOM.Text,
                cmbBaud.Text,
                cmbParity.Text,
                cmbDataBits.Text,
                cmbStopBits.Text);

            switch (portState)
            {
                case PhysicalLayer.PortState.Connected:
                    ConnectSuccess();
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

        /// <summary>
        /// Обработчик события, возникающего при нажатии на кнопку "Поиск"
        /// </summary>
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

        /// <summary>
        /// Обработчик события, возникающего при нажатии на кнопку "Отправить"
        /// </summary>
        private void butSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sFile = new FileStream(tbName.Text, FileMode.Open);
                string fileName = Path.GetFileName(tbName.Text);
                gbxChooseFile.IsEnabled = false;
                butOK.IsEnabled = false;
                gbxSendProgress.Visibility = Visibility.Visible;
                lblSend.Content = "Передача файла: " + Path.GetFileName(sFile.Name) + ":";
                pbrSendProgress.Maximum = sFile.Length;
                pbrSendProgress.Value = 0.0;
                canalHandler.SendFile(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Обработчик события, возникающего при изменении текста в tbName
        /// </summary>
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

        /// <summary>
        /// Обработчик события, возникающего при закрытии формы
        /// </summary>
        private void WinSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sFile != null || rFile != null)
            {
                MessageBoxResult result = MessageBox.Show("Программа в данный момент занята. Вы действительно хотите выйти из программы?", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// Останавливает программу, когда закрылась программа на другом компьютере
        /// </summary>
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
                MessageBox.Show("Соединение с другим компьютером прервано", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }

        /// <summary>
        /// Показывает информацию о том, что соединение установлено
        /// </summary>
        public void ConnectSuccess()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                lblStatus.Foreground = Brushes.Green;
                lblStatus.Content = "Соединение установлено";
                gbxChooseFile.IsEnabled = true;
                MessageBox.Show("Соединение с другим компьютером установлено", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
        }

        /// <summary>
        /// Показывает Message Box с запросом принятия файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        public void SavePrompt(string fileName)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBoxResult result = MessageBox.Show("Принять файл: " + fileName + "?", "Сообщение", MessageBoxButton.YesNo, MessageBoxImage.Question);
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

        /// <summary>
        /// Открывает диалоговое окно сохранения файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
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
                    lblReceive.Content = "Прием файла: " + Path.GetFileName(rFile.Name);
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

        /// <summary>
        /// Функция, вызываемая таймером
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            lblReceiveProgress.Content = string.Format("Принято {0:f2} КБ", rFile.Length / 1024.0);
        }

        /// <summary>
        /// Останавливает передачу, т.к. передача была отменена на другом компьютере
        /// </summary>
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
                MessageBox.Show("Передача файла была отменена", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
        }

        /// <summary>
        /// Закрывает открытые файлы
        /// </summary>
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

        /// <summary>
        /// Показывает информацию о том, что файл принят
        /// </summary>
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
                MessageBox.Show("Файл успешно принят", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
        }

        /// <summary>
        /// Показывает информацию о том, что файл передан
        /// </summary>
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
                MessageBox.Show("Файл успешно отправлен", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
        }

        /// <summary>
        /// Записывает информацию в файл
        /// </summary>
        /// <param name="bytes">Массив, который нужно записать в файл</param>
        public void WriteToFile(byte[] bytes)
        {
            rFile.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Считывает информацию из файла
        /// </summary>
        /// <param name="bytes">Массив, в который нужно считать из файла</param>
        /// <returns></returns>
        public int ReadFromFile(byte[] bytes)
        {
            int bytesRead = sFile.Read(bytes, 0, bytes.Length);
            Dispatcher.Invoke(new Action(() =>
            {
                pbrSendProgress.Value += bytesRead;
            }));
            return bytesRead;
        }
    }
}
