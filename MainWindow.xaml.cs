using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpticalFiberAssembly
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serialPort;
        Thread readThread;
        bool readThreadOn;

        public MainWindow()
        {
            InitializeComponent();

            GetPorts();
            serialPort = new SerialPort("COM10", 9600);
            readThread = new Thread(ReadSerial);
        }


        /// <summary>
        /// 获取目前可用的端口并更新 UI
        /// </summary>
        private void GetPorts()
        {
            portBox.ItemsSource = SerialPort.GetPortNames();
        }

        private void ReadSerial()
        {
            while (readThreadOn)
            {
                try
                {
                    string message = serialPort.ReadLine();

                    if (message != null)
                    {
                        // 使用Dispatcher将操作调度到UI线程上
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 在UI线程上更新UI元素
                            debugBlock.Text += message;
                        });
                    }
                }
                catch (TimeoutException) { }
                catch (OperationCanceledException) { }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (portBox.Text != null)
            {
                serialPort.PortName = portBox.SelectedItem as string;
            }
        }

        private void BaudBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (baudBox.Text != null)
            {
                string s = baudBox.SelectedItem as string;
                try
                {
                    serialPort.BaudRate = Convert.ToInt32(s);
                }
                catch (Exception)
                {

                }
            }
        }

        private void PortRefresh_Click(object sender, RoutedEventArgs e)
        {
            GetPorts();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                //readThread.Join();
                readThreadOn = false;
                serialPort.Close();
                btnConnect.Content = "连接";
                debugBlock.Text += "serial closed\n";
            }
            else
            {
                serialPort.Open();
                readThreadOn = true;
                readThread.Start();
                btnConnect.Content = "断开连接";
                debugBlock.Text += "serial opened\n";
                Console.WriteLine("Port name:{0}, baud rate:{1}",
                    serialPort.PortName, serialPort.BaudRate);

                //while (serialPort.BytesToRead > 0)
                //{
                //    string mes = serialPort.ReadLine();
                //    debugBlock.Text += mes;
                //}
            }
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            //byte[] code = [CmdCode.GET_STATUS];
            byte[] code = [(byte)0x14, (byte)0x00];
            serialPort.Write(code, 0, code.Length);
        }
    }
}
