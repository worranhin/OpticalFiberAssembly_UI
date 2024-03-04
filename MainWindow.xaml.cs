﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        Stepper stepperX;
        CancellationTokenSource readTaskCancelSource;
        CancellationToken readTaskCancelToken;
        Task readTask;
        bool isUpdatingStatus;
        System.Timers.Timer statusTimer;

        public MainWindow()
        {
            InitializeComponent();

            GetPorts();
            serialPort = new SerialPort("COM10", 9600);
            stepperX = new Stepper(1, serialPort);
            readThread = new Thread(ReadSerial);

            // 初始化更新状态信息的定时器
            statusTimer = new System.Timers.Timer(1000);
            statusTimer.Elapsed += StatusTimerEvent;
            statusTimer.AutoReset = true;

            isUpdatingStatus = false;
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
                        DebugMessage("Command: " + ((CmdCode)cmd).ToString() + "\n");
                    }
                }
                catch (TimeoutException) { }
                //catch (OperationCanceledException) {
                //    //return;
                //}
            }
        }

        /// <summary>
        /// 更新状态
        /// </summary>
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

        /// <summary>
        /// 用于更新状态定时器的中断事件
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void StatusTimerEvent(Object source, ElapsedEventArgs e)
        {
            UpdateStatus();
        }

        private static void HandleError(ErrorCode code)
        {
            MessageBox.Show($"error: {code}");
        }

        /// <summary>
        /// 用于在界面中打印调试信息
        /// </summary>
        /// <param name="message">待打印的信息</param>
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
                portBox.IsEnabled = true;
                baudBox.IsEnabled = true;
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
                portBox.IsEnabled = false;
                baudBox.IsEnabled = false;
                btnConnect.Content = "断开连接";
                DebugMessage("serial opened\n");
            }
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            if(!isUpdatingStatus)
            {
                statusTimer.Start();
                btnStatus.Content = "停止更新状态";
                btnStatus.Style = this.FindResource("MaterialDesignRaisedLightButton") as Style;
                isUpdatingStatus = true;
            } else
            {
                statusTimer.Stop();
                btnStatus.Content = "开始更新状态";
                btnStatus.Style = this.FindResource("MaterialDesignRaisedButton") as Style;
                isUpdatingStatus = false;
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                stepperX.Run();
                stepperX.SetSpeed(byte.Parse(xSpeedBox.Text));
                stepperX.SetAcceleration(byte.Parse(xAccBox.Text));
                stepperX.SetTarget(byte.Parse(xTarBox.Text));
            }
            catch (FormatException)
            {
                MessageBox.Show("请输入正确的格式");
            }
            catch (OverflowException)
            {
                MessageBox.Show("数值设置过大");
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("串口未打开");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            stepperX.Stop();
        }
    }
}
