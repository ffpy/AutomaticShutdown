using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutomaticShutdown
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 临界最大值
        private const int COUNT_NUM = 5;

        // 是否正在运行
        private bool isRunning = false;
        // 是否正在倒计时
        private bool isCountdowning = false;
        // 下限网速(kb/s)
        private int limitSpeed;
        // 倒计时
        private int limitCount;
        // 关机倒计时
        private int shutdownCount;
        // 临界计数
        private int count = 0;

        public MainWindow()
        {
            InitializeComponent();
            // 居中显示
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            showSpeed();
        }

        private void showSpeed()
        {
            List<PerformanceCounter> pcs = new List<PerformanceCounter>();
            List<PerformanceCounter> pcs2 = new List<PerformanceCounter>();
            string[] names = getAdapter();
            foreach (string name in names)
            {
                try
                {
                    PerformanceCounter pc = new PerformanceCounter("Network Interface", "Bytes Received/sec", name.Replace('(', '[').Replace(')', ']'), ".");
                    PerformanceCounter pc2 = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name.Replace('(', '[').Replace(')', ']'), ".");
                    pc.NextValue();
                    pcs.Add(pc);
                    pcs2.Add(pc2);
                }
                catch
                {
                    continue;
                }

            }

            List<PerformanceCounter>[] pcss = new List<PerformanceCounter>[2];
            pcss[0] = pcs;
            pcss[1] = pcs2;
            Thread getSpeedThread = new Thread(new ParameterizedThreadStart(speedThreadRun));
            getSpeedThread.Start(pcss);
        }

        private void speedThreadRun(object obj)
        {
            List<PerformanceCounter>[] pcss = (List<PerformanceCounter>[])obj;
            List<PerformanceCounter> pcs = pcss[0];
            List<PerformanceCounter> pcs2 = pcss[1];
            while (true)
            {
                long recv = 0;
                long sent = 0;
                foreach (PerformanceCounter pc in pcs)
                {
                    recv += Convert.ToInt32(pc.NextValue()) / 1024;
                }
                foreach (PerformanceCounter pc in pcs2)
                {
                    sent += Convert.ToInt32(pc.NextValue()) / 1024;
                }
                speedLabel.Dispatcher.BeginInvoke(new Action(() => speedLabel.Content = "下载: " + recv + "kb/s" + "\t上传: " + sent + "kb/s"));
                // Console.WriteLine("recv: " + recv + "kb/s" + " ,send:" + sent + "kb/s");

                running(recv, sent);

                Thread.Sleep(1000);
            }
        }

        private void running(long recv, long sent)
        {
            if (!isRunning) return;
            long speed = getSpeedByType(recv, sent);
            if (isCountdowning)
            {
                // 检查切换状态
                if (speed >= limitSpeed)
                {
                    count++;
                    if (count >= COUNT_NUM)
                    {
                        // 取消倒计时状态
                        cancelCountdown();
                        return;
                    }
                }

                shutdownCount = Math.Max(shutdownCount - 1, 0);
                this.Dispatcher.Invoke(new Action(() => shutdownCountdownLabel.Content = shutdownCount + ""));
                if (shutdownCount <= 0)
                {
                    // 倒计时结束，关机
                    Process.Start("c:/windows/system32/shutdown.exe", "-s -t 60");

                    Environment.Exit(0);
                }
            }
            else
            {
                // 检查切换状态
                if (speed < limitSpeed)
                {
                    count++;
                    if (count >= COUNT_NUM)
                    {
                        // 开始倒计时状态
                        isCountdowning = true;
                        count = 0;

                        shutdownCount = limitCount;
                        this.Dispatcher.Invoke(new Action(() => shutdownCountdownLabel.Content = shutdownCount + ""));
                        return;
                    }
                }
            }
        }

        private void cancelCountdown()
        {
            isCountdowning = false;
            count = 0;

            this.Dispatcher.Invoke(new Action(() => shutdownCountdownLabel.Content = "-"));
        }

        private long getSpeedByType(long recv, long sent)
        {
            return this.Dispatcher.Invoke(new Func<long>(() =>
            {
                long speed = recv;
                if (rbOnlyDown.IsChecked.Value)
                {
                    speed = recv;
                }
                else if (rbOnlyUp.IsChecked.Value)
                {
                    speed = sent;
                }
                else if (rbDownAndUp.IsChecked.Value)
                {
                    speed = recv + sent;
                }
                return speed;
            }));
        }

        private string[] getAdapter()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            string[] name = new string[adapters.Length];
            int index = 0;
            foreach (NetworkInterface ni in adapters)
            {
                name[index] = ni.Description;
                index++;
            }
            return name;
        }

        private void tbLimit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9]+");
            e.Handled = re.IsMatch(e.Text);
        }

        private void tbCountdown_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9]+");
            e.Handled = re.IsMatch(e.Text);
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                startButton.Content = "确定";
                setEditEnable(true);
                cancelCountdown();
            }
            else
            {
                try
                {
                    limitSpeed = Convert.ToInt16(tbLimit.Text);
                    limitCount = Convert.ToInt16(tbCountdown.Text);
                }
                catch (FormatException fe)
                {
                    MessageBox.Show("解析数字错误");
                    return;
                }

                startButton.Content = "取消";
                setEditEnable(false);
            }
            isRunning = !isRunning;
        }

        private void setEditEnable(bool enable)
        {
            tbLimit.IsEnabled = enable;
            tbCountdown.IsEnabled = enable;
            rbOnlyDown.IsEnabled = enable;
            rbOnlyUp.IsEnabled = enable;
            rbDownAndUp.IsEnabled = enable;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
