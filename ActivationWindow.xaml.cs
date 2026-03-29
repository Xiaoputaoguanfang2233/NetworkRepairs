using System.Windows;

namespace NetworkTroubleshooter
{
    /// <summary>
    /// Interaction logic for ActivationWindow.xaml
    /// </summary>
    public partial class ActivationWindow : Window
    {
        // 用于存储正确的激活码
        public string ActivationCode { get; set; }

        public ActivationWindow()
        {
            InitializeComponent();
            // 设置窗口样式
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.NoResize;
            this.Title = "产品激活"; // 修改标题以匹配XAML
        }

        /// <summary>
        /// 当点击“下一步”按钮时触发
        /// </summary>
        private void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            // 获取用户输入的激活码
            string inputCode = txtActivationCode.Text;

            // 比较输入的激活码与预设的激活码
            if (string.IsNullOrEmpty(inputCode))
            {
                // 如果输入为空，给出提示
                MessageBox.Show("请输入产品密钥。", "输入不能为空", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtActivationCode.Focus();
                return;
            }

            if (inputCode == ActivationCode)
            {
                // 验证成功，设置 DialogResult 为 true 并关闭窗口
                // 这将导致 ShowDialog() 返回 true
                MessageBox.Show("产品已成功激活。欢迎使用！\n 请与我们的网站保持联系，产品激活码将在30日后失效", "激活成功", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                // 验证失败，提示错误信息，不清空输入框以便用户再次尝试
                MessageBox.Show("产品密钥无效。请检查密钥并重试。", "激活失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                // 清空输入框并重新聚焦，方便用户输入
                txtActivationCode.Clear();
                txtActivationCode.Focus();
            }
        }

        /// <summary>
        /// 当点击“取消”按钮或按ESC键时触发
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // 设置 DialogResult 为 false 并关闭窗口
            // 这将导致 ShowDialog() 返回 false
            this.DialogResult = false;
            this.Close();
        }
    }
}