#region copyright
/**
// --------------------------------------------------------------------------------
// 文件名：MainWindow.xaml.cs
// 作者：刹那 https://x.com/chanawudi
// 公司：https://x.com/chanawudi
// 更新日期：2025，2，27，13:57
// 版权所有 © Your Company. 保留所有权利。
// --------------------------------------------------------------------------------
*/
#endregion
using AdonisUI;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AdonisUI.Controls;
using XChrome.pages;
using ToastNotifications;
using ToastNotifications.Position;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using log4net.Config;
using log4net;
using System.IO;
using System.Reflection;
using XChrome.forms;
using XChrome.cs;
using AutoUpdaterDotNET;

namespace XChrome
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisUI.Controls.AdonisWindow
    {
        private bool _Dark=false;
        private Notifier notifier = null;
        private static MainWindow _mainWindow = null;
        public MainWindow()
        {
            InitializeComponent();
            _mainWindow = this;
        }

        #region toast

        public static void Toast_Information(string msg)
        {
            if (MainWindow._mainWindow == null) return;
            if (MainWindow._mainWindow.notifier == null)  return;
            MainWindow._mainWindow.notifier.ShowInformation(msg);
        }
        public static void Toast_Success(string msg)
        {
            if (MainWindow._mainWindow == null) return;
            if (MainWindow._mainWindow.notifier == null) return;
            MainWindow._mainWindow.notifier.ShowSuccess(msg);
        }
        public static void Toast_Warning(string msg)
        {
            if (MainWindow._mainWindow == null) return;
            if (MainWindow._mainWindow.notifier == null) return;
            MainWindow._mainWindow.notifier.ShowWarning(msg);
        }
        public static void Toast_Error(string msg)
        {
            if (MainWindow._mainWindow == null) return;
            if (MainWindow._mainWindow.notifier == null) return;
            MainWindow._mainWindow.notifier.ShowError(msg);
        }


        private void CreateNotifier()
        {
            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.BottomCenter,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });
        }


        #endregion


        /// <summary>
        /// 系统入口
        /// </summary>
        /// <returns></returns>
        private async Task<bool> WenLoaded()
        {
            ////检测更新
            //AutoUpdater.SetOwner(this);
            //AutoUpdater.Start("https://down.web3tool.vip/xchrome/xchrome_update.xml");

            Func<Task> load = async () => {
                //初始化日志
                cs.Loger.DeleteMoreLogers();
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
                //数据库初始化
                await cs.db.MyDb.ini();
                //初始化config
                await cs.Config.ini();
                //启动jobermanager
                cs.JoberManager.Start();
                
                

                await Task.Delay(500);
            };
            Loading l = new Loading(this, load);
            l.ShowDialog();
            return true;
        }


        /// <summary>
        /// 系统关闭
        /// </summary>
        /// <returns></returns>
        private async Task WenCloseing()
        {
            //关闭任务服务
            cs.JoberManager.Stop();
            //关闭 playwirght
            await XChromeManager.DisposePlayWright();
            //关闭控制器
            cs.MouseHookServer.UnIni();

            _mainWindow = null;
        }

        /// <summary>
        /// 全局未捕获错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void GlobException(object? sender,Exception e)
        {
            await WenCloseing();
           
        }



        #region 打开关闭



        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            GlobException(sender, new Exception(e.ToString()));
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            GlobException(sender, new Exception("UnobservedTaskException", e.Exception));
            
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            GlobException(sender, new Exception("UnhandledException", (Exception)e.ExceptionObject));
        }

        private async void AdonisWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            await WenCloseing();
            // 如果窗口处于最大化状态，从 RestoreBounds 获取实际大小和位置
            if (this.WindowState == WindowState.Normal)
            {
                Settings1.Default.WindowWidth= this.Width;
                Settings1.Default.WindowHeight = this.Height;
                Settings1.Default.WindowTop = this.Top;
                Settings1.Default.WindowLeft = this.Left;
            }
            else
            {
                Settings1.Default.WindowWidth = this.RestoreBounds.Width;
                Settings1.Default.WindowHeight = this.RestoreBounds.Height;
                Settings1.Default.WindowTop = this.RestoreBounds.Top;
                Settings1.Default.WindowLeft = this.RestoreBounds.Left;
            }

            Settings1.Default.WindowState = this.WindowState.ToString();
            Settings1.Default.WindowScheme= _Dark?1:0;
            // 保存设置
            Settings1.Default.Save();
        }

        private async void AdonisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            

            //return;
            // 根据保存的设置还原窗口位置、大小和状态
            if (Settings1.Default.WindowWidth > 0)
            {
                this.Width = Settings1.Default.WindowWidth;
            }
            if (Settings1.Default.WindowHeight > 0)
            {
                this.Height = Settings1.Default.WindowHeight;
                this.Top = Settings1.Default.WindowTop;
                this.Left = Settings1.Default.WindowLeft;
            }
            

            // 还原窗口状态
            if (Enum.TryParse(Settings1.Default.WindowState, out WindowState state))
            {
                this.WindowState = state;
            }

            int sc=Settings1.Default.WindowScheme;
            if (sc == 0)
            {
                ResourceLocator.SetColorScheme(Application.Current.Resources, ResourceLocator.LightColorScheme);
                SchemeCheck.IsChecked = false;
                _Dark = false;
            }
            else
            {
                ResourceLocator.SetColorScheme(Application.Current.Resources, ResourceLocator.DarkColorScheme);
                SchemeCheck.IsChecked = true;
                _Dark = true;
            }

            //初始化
            var issuccess= await WenLoaded();
            if (!issuccess)
            {
                Application.Current.Shutdown();
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            //加载环境管理页面
            ListBoxItem_cmanager.IsSelected= true;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            ver_text.Text = $"版本：{version}";

            //创建通知管理器
            CreateNotifier();

            
        }
        #endregion

        /// <summary>
        /// 换皮肤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox c = (CheckBox)sender;
            if (c.IsChecked == true)
            {
                ResourceLocator.SetColorScheme(Application.Current.Resources, ResourceLocator.DarkColorScheme);
                _Dark = true;
            }
            else
            {
                ResourceLocator.SetColorScheme(Application.Current.Resources, ResourceLocator.LightColorScheme);
                _Dark = false;
            }
        }


        /// <summary>
        /// 左边主菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ((ListBox)sender).SelectedIndex;
            if(index == 0)
            {
                MainFrame_other.Visibility = Visibility.Hidden;
                MainFrame_main.Visibility = Visibility.Visible;
            }
            else
            {
                MainFrame_other.Visibility = Visibility.Visible;
                MainFrame_main.Visibility = Visibility.Hidden;
            }
            switch (index) { 
                case 0:
                    //环境管理
                    if (CManager._cmanager==null)
                        MainFrame_main.Navigate(new pages.CManager());
                    break;
                case 1:
                    //分组管理
                    MainFrame_other.Navigate(new pages.GroupManager());
                    break;
                case 2:
                    MainFrame_other.Navigate(new pages.coding());
                    break;
                default:
                    MainFrame_other.Navigate(new pages.coding());
                    break;
            }

        }

        /// <summary>
        /// 添加环境
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            forms.EditChrome ec=new forms.EditChrome(-1);
            ec.Owner = this;
            ec.ShowDialog();
            if (ec.isSuccess)
            {
                //刷新管理器
                if (CManager._cmanager != null)
                {
                    await CManager._cmanager.addOver();
                }
            }
            
        }
    }
}
