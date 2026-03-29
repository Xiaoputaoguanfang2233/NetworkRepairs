using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32; // 添加注册表命名空间

namespace NetworkTroubleshooter
{
    public partial class MainWindow : Window
    {
        // 定义注册表路径和键名
        private readonly string _registryPath = @"SOFTWARE\NetworkTroubleshooter";
        private readonly string _registryKeyName = "LastActivationTime";

        private VpnManager vpn = new VpnManager();
        private bool _isCleaningUp = false; // 防止重复清理

        public MainWindow()
        {
            // 新增：先检查激活状态
            if (!IsActivationValid())
            {
                // 激活码验证失败或已过期，直接退出程序
                Application.Current.Shutdown();
                return;
            }

            InitializeComponent();
            Logger.Info("\n应用程序启动");
            DelProxyandVPN(0);
            this.Closing += MainWindow_Closing; // 注册关闭事件
        }

        // 新增：检查激活是否有效
        private bool IsActivationValid()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_registryPath))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(_registryKeyName);
                        if (value != null && DateTime.TryParse(value.ToString(), out DateTime lastActivationDate))
                        {
                            // 计算时间差
                            TimeSpan timeSinceLastActivation = DateTime.Now - lastActivationDate;

                            // 如果未超过30天，则验证有效
                            if (timeSinceLastActivation.TotalDays <= 30)
                            {
                                Logger.Info($"验证有效，距离上次激活已过去 {timeSinceLastActivation.Days} 天。");
                                return true;
                            }
                            else
                            {
                                Logger.Info($"验证已过期，距离上次激活已过去 {timeSinceLastActivation.Days} 天，需要重新验证。");
                            }
                        }
                    }
                    else
                    {
                        Logger.Info("未找到激活记录，需要首次验证。");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("读取注册表时发生错误", ex);
                // 读取失败，视为需要重新验证
            }

            // 如果注册表项不存在、解析失败或已过期，则弹出激活窗口
            return ShowActivationWindow();
        }

        // 新增：保存当前时间为最新激活时间到注册表
        private void SaveActivationTime()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_registryPath))
                {
                    if (key != null)
                    {
                        // 写入当前UTC时间，以避免时区问题
                        key.SetValue(_registryKeyName, DateTime.UtcNow.ToString("o"), RegistryValueKind.String);
                        Logger.Info("激活时间已保存至注册表。");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("保存激活时间到注册表时发生错误", ex);
                // 也可以给用户一个UI提示
                MessageBox.Show("保存激活状态时发生错误，可能会导致下次启动时需要重新验证。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // 新增：激活码验证窗口
        private bool ShowActivationWindow()
        {
            // 激活码示例（实际应用中应存储在安全位置）
            const string validActivationCode = "\\@(^O^)@/";

            // 创建验证窗口
            var activationWindow = new ActivationWindow();
            activationWindow.ActivationCode = validActivationCode;

            // 显示模态窗口并等待结果
            bool? result = activationWindow.ShowDialog();

            if (result == true)
            {
                // 验证成功，保存当前时间
                SaveActivationTime();
                return true;
            }

            return false; // 验证失败或取消
        }

        // 窗口关闭前的清理操作
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DelProxyandVPN(1);
        }

        // ... [其余代码保持不变] ...

        private async Task DelProxyandVPN(int num)
        {
            if (_isCleaningUp) return;
            _isCleaningUp = true;

            try
            {
                if (num == 1)
                {
                    bool createProxyChecked = await Application.Current.Dispatcher.InvokeAsync(() => chkCreateProxy.IsChecked == true);

                    if (!createProxyChecked)
                        await Task.Run(() => vpn.DeleteVpn("以太网 4"));
                    else
                        await Task.Run(() => vpn.ClearAndDisableSystemProxy());
                }
                else
                {
                    await Task.WhenAll(
                        Task.Run(() => vpn.ClearAndDisableSystemProxy()),
                        Task.Run(() => vpn.DeleteVpn("以太网 4"))
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.Error("删除设置时发生错误", ex);
            }
            finally
            {
                _isCleaningUp = false;
            }
        }
        private void chkCreateProxy_Checked(object sender, RoutedEventArgs e)
        {
            chkAdvancedSettings.Visibility = Visibility.Visible;
        }

        private void chkCreateProxy_Unchecked(object sender, RoutedEventArgs e)
        {
            chkAdvancedSettings.Visibility = Visibility.Collapsed;
            chkAdvancedSettings.IsChecked = false;
        }

        private void txtAdvanced_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            chkCreateProxy.Visibility = chkCreateProxy.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;

            if (chkCreateProxy.Visibility != Visibility.Visible)
            {
                chkAdvancedSettings.Visibility = Visibility.Collapsed;
                chkAdvancedSettings.IsChecked = false;
            }
            else
            {
                if (chkCreateProxy.IsChecked == true)
                {
                    chkAdvancedSettings.Visibility = Visibility.Visible;
                }
            }
        }
        private async void btnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetUiState(isProcessing: true);

                if (chkCreateProxy.IsChecked == true)
                {
                    txtStatus.Text = "正在进行快速修复...";
                    await Task.Delay(1000);
                    string proxyServer;
                    if (chkAdvancedSettings.IsChecked == true)
                        proxyServer = $"10.88.202.73:10002";
                    else
                        proxyServer = $"10.88.202.73:10001";
                    bool success = await Task.Run(() => vpn.SetSystemProxy(true, proxyServer, ""));

                    if (success)
                    {
                        pBar.IsIndeterminate = false;
                        pBar.Value = 100;
                        txtStatus.Text = "快速修复完成";
                        await Task.Delay(1000);

                        pnlProgress.Visibility = Visibility.Collapsed;
                        pnlResult.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("设置系统代理失败。请检查是否有权限修改注册表。",
                            "代理设置失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ResetUi();
                    }
                }
                else
                {
                    txtStatus.Text = "正在扫描网络状态...";
                    await Task.Delay(1000);

                    txtStatus.Text = "正在尝试建立安全连接...";

                    bool success = await Task.Run(() => vpn.CreateAndConnectVpn(
                        "以太网 4", "10.88.202.73", "ps", @"\@(^O^)@/", "pysyzx"));

                    if (success)
                    {
                        pBar.IsIndeterminate = false;
                        pBar.Value = 100;
                        txtStatus.Text = "连接已建立。";
                        await Task.Delay(800);

                        pnlProgress.Visibility = Visibility.Collapsed;
                        pnlResult.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("网络验证失败。请确保：\n1. 以管理员身份运行此程序\n2. 账号密码及密钥正确",
                            "连接失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ResetUi();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生非预期错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error("操作发生非预期错误", ex);
                ResetUi();
            }
        }

        private void SetUiState(bool isProcessing)
        {
            pnlWelcome.Visibility = isProcessing ? Visibility.Collapsed : Visibility.Visible;
            pnlProgress.Visibility = isProcessing ? Visibility.Visible : Visibility.Collapsed;
            btnNext.IsEnabled = !isProcessing;
            pBar.IsIndeterminate = isProcessing;
        }

        private void ResetUi()
        {
            SetUiState(isProcessing: false);
            pnlResult.Visibility = Visibility.Collapsed;
            pBar.IsIndeterminate = false;
            pBar.Value = 0;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnCloseTroubleshooter_Click(object sender, RoutedEventArgs e) => this.Close();

        private async void BackToMainPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnCancel.IsEnabled = false;

                bool isProxyMode = chkCreateProxy.IsChecked == true;

                await Task.Run(() =>
                {
                    if (isProxyMode)
                        vpn.ClearAndDisableSystemProxy();
                    else
                        vpn.DeleteVpn("以太网 4");
                });
                ResetUi();
            }
            catch (Exception ex)
            {
                Logger.Error("回退修复时发生错误", ex);
                MessageBox.Show("回退修复时发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetUi(); // 即使出错也尝试返回主页
            }
            finally
            {
                btnCancel.IsEnabled = true;
            }
        }
    }
}