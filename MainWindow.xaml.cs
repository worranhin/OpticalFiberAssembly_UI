using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        Stepper stepperX;
        CancellationTokenSource readTaskCancelSource;
        CancellationToken readTaskCancelToken;
        Task readTask;

        public MainWindow()
        {
            InitializeComponent();

            readTaskCancelSource = new CancellationTokenSource();
            readTaskCancelToken = readTaskCancelSource.Token;


            GetPorts();
            serialPort = new SerialPort("COM10", 9600);
            stepperX = new Stepper(1, serialPort);
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
            readTaskCancelToken.ThrowIfCancellationRequested();
            while (true)
            {
                readTaskCancelToken.ThrowIfCancellationRequested();  // 抛出取消异常并终止
                    if (serialPort.BytesToRead < 2)
                        continue;

                try
                {
                    // 合法性判断
                    int cmd = serialPort.ReadByte();
                    if (cmd == -1)
                        continue;

                    // 处理错误代码
                    int ec = serialPort.ReadByte();
                    if (ec == -1)
                        continue;
                    if (ec != (int)ErrorCode.ERRCODE_OK)
                    {
                        HandleError((ErrorCode)ec);
                        continue;
                    }

                    // 获取状态
                    if ((byte)(cmd & 0x0f) == (byte)CmdCode.GET_STATUS)
                    {
                        string message = serialPort.ReadLine();

                        readTaskCancelToken.ThrowIfCancellationRequested();

                        if (message != null)
                        {
                            StepperStatus stat = JsonSerializer.Deserialize<StepperStatus>(message);
                            int deviceId = (cmd & 0xf0) >> 4;
                            switch (deviceId)
                            {
                                case 1:
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        xPosBox.Text = stat.position.ToString();
                                        xSpeedBox.Text = stat.speed.ToString();
                                        xAccBox.Text = stat.acceleration.ToString();
                                        xTarBox.Text = stat.target.ToString();
                                    }));
                                    break;

                                case 2:
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        yPosBox.Text = stat.position.ToString();
                                        ySpeedBox.Text = stat.speed.ToString();
                                        yAccBox.Text = stat.acceleration.ToString();
                                        yTarBox.Text = stat.target.ToString();
                                    }));
                                    break;

                                case 3:
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        zPosBox.Text = stat.position.ToString();
                                        zSpeedBox.Text = stat.speed.ToString();
                                        zAccBox.Text = stat.acceleration.ToString();
                                        zTarBox.Text = stat.target.ToString();
                                    }));
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        DebugMessage("Command: " + cmd.ToString() + "\n");
                    }
                }
                catch (TimeoutException) { }
                //catch (OperationCanceledException) {
                //    //return;
                //}
            }
        }

        private void UpdateStatus()
        {
            byte[] code;
            code = [(byte)0x14, (byte)0x00];
            serialPort.Write(code, 0, code.Length);
            code = [(byte)0x24, (byte)0x00];
            serialPort.Write(code, 0, code.Length);
            code = [(byte)0x34, (byte)0x00];
            serialPort.Write(code, 0, code.Length);
        }

        private static void HandleError(ErrorCode code)
        {
            MessageBox.Show($"error: {code}");
        }

        private void DebugMessage(string message)
        {
            // 使用Dispatcher将操作调度到UI线程上
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 在UI线程上更新UI元素
                debugBlock.Text += message;
            });
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
            if (baudBox.SelectedItem != null)
            {
                string s = baudBox.SelectedItem as string;
                try
                {
                    serialPort.BaudRate = Convert.ToInt32(s);
                }
                catch (Exception) { }
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
                readTaskCancelSource.Cancel();  // 取消任务

                try
                {
                    readTask.Wait(2000);
                }
                catch (AggregateException except)
                {
                    DebugMessage($"{nameof(AggregateException)} thrown with message: {except.Message}\n");
                }
                catch (OperationCanceledException except)
                {
                    DebugMessage($"{nameof(OperationCanceledException)} thrown with message: {except.Message}\n");
                }
                finally
                {
                    readTaskCancelSource.Dispose();
                    serialPort.Close();
                }

                // UI 处理
                btnConnect.Content = "连接";
                DebugMessage("serial closed\n");
            }
            else
            {
                serialPort.Open();
                stepperX.CommSerial = serialPort;  // 设置电机控制的端口

                // 创建 Task 实例并运行
                readTaskCancelSource = new CancellationTokenSource();
                readTaskCancelToken = readTaskCancelSource.Token;
                readTask = new Task(() => ReadSerial(), readTaskCancelToken, TaskCreationOptions.LongRunning);
                readTask.Start();

                // UI 处理
                btnConnect.Content = "断开连接";
                DebugMessage("serial opened\n");
            }
        }

        private async void BtnConnect_Click_test(object sender, RoutedEventArgs e)
        {
            if (!serialPort.IsOpen)
            {
                serialPort.Open();
                readTaskCancelSource = new CancellationTokenSource();
                readTaskCancelToken = readTaskCancelSource.Token;
                readTask = Task.Run(() => ReadSerial(), readTaskCancelSource.Token);
                DebugMessage("reading\n");
            }
            else
            {
                readTaskCancelSource.Cancel();

                try
                {
                    await readTask;
                }
                catch (OperationCanceledException except)
                {
                    DebugMessage($"{nameof(OperationCanceledException)} thrown with message: {except.Message}\n");
                }
                finally
                {
                    readTaskCancelSource.Dispose();
                    serialPort.Close();
                    DebugMessage("read close\n");
                }
            }
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus();
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            stepperX.Run();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            stepperX.Stop();
        }
    }
}
