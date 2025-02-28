#region copyright
/**
// --------------------------------------------------------------------------------
// 文件名：XChromeManager.cs
// 作者：刹那 https://x.com/chanawudi
// 公司：https://x.com/chanawudi
// 更新日期：2025，2，27，13:55
// 版权所有 © Your Company. 保留所有权利。
// --------------------------------------------------------------------------------
*/
#endregion
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Shapes;
using XChrome.cs.db;
using XChrome.cs.tools.YTools;
using XChrome.cs.win32;
using XChrome.pages;

namespace XChrome.cs
{
    public class XChromeManager
    {
        private static IPlaywright? playwright = null;

        /// <summary>
        /// 打开着的浏览器
        /// </summary>
        private static Dictionary<long,XChrome> runing_xchrome = new Dictionary<long,XChrome>();
        private static readonly object _lock_runing_xchrome = new object();
        //是否已经启动定时调整页面
        public static bool _is_jober_AdjustmentView = false;



        /// <summary>
        /// 打开一个浏览器
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static async Task OpenChrome(XChrome x)
        {

            if(playwright==null)
            {
                playwright = await Playwright.CreateAsync();
            }



            //;
            // 拼接出 AppData 目录："C:\Users\chanawudi\AppData"
            //string appDataPath = Path.Combine(userProfile, "AppData");


            //一些参数
            #region =====启动参数=========

            List<string> args = new List<string>();
            args.Add("--disable-features=IsolateOrigins,site-per-process");
            args.Add("--disable-features=ChromeLabs");

            //插件
            List<string> extensionList = new List<string>();
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (Directory.Exists(userProfile))
            {
                string expath = System.IO.Path.Combine(userProfile, "AppData", "Local", "Google", "Chrome", "User Data", "Default", "Extensions");
                string[] el=x.Extensions.Split("|");
                foreach (string ext in el) {
                    var _e = ext.Trim();
                    string _ep = expath + "\\" + _e;
                    if (System.IO.Path.Exists(_ep)) { 
                        var ds=System.IO.Directory.GetDirectories(_ep);
                        if (ds.Count() > 0) {
                            string _ep1 = ds[0];
                            extensionList.Add(_ep1);
                        }
                    }
                }
            }
            if (extensionList.Count() > 0) {
                string _exlist = string.Join(",", extensionList);
                args.Add($"--disable-extensions-except="+ _exlist);
                args.Add($"--load-extension="+ _exlist);
            }

           


            #endregion

            #region =====指纹参数======

            //解读xchrome环境evns
            JObject evnJ = JObject.Parse(string.IsNullOrEmpty(x.Evns)? "{}": x.Evns);
            var options = new BrowserTypeLaunchPersistentContextOptions()
            {
                UserAgent = x.UserAgent,
                ExecutablePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".playwright", "chrome", "chromium-1117", "chrome-win", "chrome.exe"),
                //ExecutablePath= "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
                Headless = false,
                Proxy = getProxy(x.Proxy),
                Args = args,
                Env = new Dictionary<string, string>
                    {
                        { "GOOGLE_API_KEY", "AIzaSyCkfPOPZXDKNn8hhgu3JrA62wIgC93d44k" },
                        { "GOOGLE_DEFAULT_CLIENT_ID", "811574891467.apps.googleusercontent.com" },
                        { "GOOGLE_DEFAULT_CLIENT_SECRET", "kdloedMFGdGla2P1zacGjAQh" }
                    },
                //关闭
                IgnoreDefaultArgs = new[] { "--enable-automation", "--no-sandbox" }
            };
            if (evnJ["Locale"] != null)
            {
                options.Locale = evnJ["Locale"]?.ToString();
            }
            if (evnJ["TimezoneId"] != null)
            {
                options.TimezoneId = evnJ["TimezoneId"]?.ToString();
            }
            if (evnJ["ViewportSize"] != null)
            {
                options.ViewportSize = new ViewportSize
                {
                    Width = Convert.ToInt32(evnJ["ViewportSize"]["width"].ToString()),
                    Height = Convert.ToInt32(evnJ["ViewportSize"]["height"].ToString())
                };
            }
            if (evnJ["DeviceScaleFactor"] != null)
            {
                options.DeviceScaleFactor = (float)Convert.ToDecimal(evnJ["DeviceScaleFactor"]?.ToString());
            }
            if (evnJ["IsMobile"] != null)
            {
                options.IsMobile = evnJ["IsMobile"]?.ToString() == "1" ? true : false;
            }

            if (evnJ["HasTouch"] != null)
            {
                options.HasTouch = evnJ["HasTouch"]?.ToString() == "1" ? true : false;
            }
            if (evnJ["Geolocation"] != null)
            {
                var Latitude = (float)Convert.ToDecimal(evnJ["Geolocation"]["Latitude"].ToString());
                var Longitude = (float)Convert.ToDecimal(evnJ["Geolocation"]["Longitude"].ToString());
                options.Geolocation = new Geolocation { Latitude = Latitude, Longitude = Longitude };
            }

            
            //headers
            Dictionary<string, string> headers = null; 
            if (evnJ["ExtraHTTPHeaders"] != null)
            {
                headers = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(evnJ["ExtraHTTPHeaders"].ToString());
            }
            else
            {
                headers = new Dictionary<string, string>();
            }
            headers.Add("User-Agent", x.UserAgent);


            #endregion


            //创建context
            var context = await playwright.Chromium.LaunchPersistentContextAsync(x.DataPath, options );
            x.BrowserContext = context;
            //加入缓存
            lock (_lock_runing_xchrome)
                runing_xchrome.AddOrReplace(x.Id, x);

            //设置headers
            if (headers != null)
                await context.SetExtraHTTPHeadersAsync(headers);


            #region=====绑定事件=====

            //当关闭时,需要通知主窗口
            context.Close += (sender, ibrowsercontext) =>
            {
                lock (_lock_runing_xchrome)
                {
                    if(runing_xchrome.ContainsKey(x.Id))runing_xchrome.Remove(x.Id);
                }
                CManager.notify_online(new List<long> { x.Id},false);   
            };
            CManager.notify_online(new List<long> { x.Id }, true);
            

            //新建页面的时候，需要更新内部page大小
            context.Page += async (_, page) =>
            {
               // iszzz = false;
                var v = x.ViewportSize;
                if(v!=null)
                {
                    await page.SetViewportSizeAsync(v.Width, v.Height);
                    //iszzz = true;
                }
            };

            
            

            #endregion



            #region=====打开首页=====

            string html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>环境：{x.Id.ToString()}</title>
  <style>
    /* 清除默认的 margin 和 padding，确保页面占满整个视口 */
    html, body {{
      margin: 0;
      padding: 0;
      height: 100%;
    }}
    /* 设置 iframe 占满整个视口，自适应大小 */
    iframe {{
      width: 100%;
      height: 100%;
      border: none;
      margin:0;
    }}
  </style>
</head>
<body>
  <!-- 此处 src 可替换为你需要加载的页面地址 -->
  <iframe id='mainiframe' src=""""></iframe>
  <script>setTimeout(()=>{{document.getElementById('mainiframe').src='{cs.Config.chrome_start_page}'}},200)</script>
</body>
</html>
";
            //
            //打开页面
            IPage? page =  context.Pages[0];
            await page.SetContentAsync(html);

           

            #endregion



            #region=====绑定句柄=====

            //查找句柄
            string className = "Chrome_WidgetWin_1";
            long xhwd = 0;
            int timeoutNum = 30;
            while (xhwd == 0)
            {
                xhwd = Win32Helper.FindWindow(className, "环境：" + x.Id.ToString()+ " - Chromium").ToInt64();
                if (xhwd != 0) break;
                await Task.Delay(500);
                timeoutNum--;
                if (timeoutNum == 0) {
                    cs.Loger.Err("获取环境浏览器句柄失败！");
                    break;
                }
            }
            x.Hwnd = xhwd;

            #endregion


            if (!_is_jober_AdjustmentView)
            {
                _is_jober_AdjustmentView = true;
                cs.JoberManager.AddJob(jober_AdjustmentView);
            }
        }


        /// <summary>
        /// 获得所有运行中xchrome
        /// </summary>
        /// <returns></returns>
        public static Dictionary<long, XChrome> GetRuningXchromes()
        {
            return runing_xchrome;
        }


        /// <summary>
        /// 群控复制操作方法，已经是task内无需异步
        /// 鼠标点击
        /// </summary>
        /// <param name="except_id"></param>
        /// <param name="xRatio">相对主控的位置比例</param>
        /// <param name="yRatio"></param>
        public static void CopyControl_Click(long except_id,double xRatio, double yRatio,string clickType="Left")
        {
            var idslist = runing_xchrome.Keys.ToArray();
            foreach (var id in idslist) { 
                if (id == except_id) continue;
                var xchrome=runing_xchrome[id];
                IntPtr hwd = (IntPtr)xchrome.Hwnd;
                Win32Helper.GetWindowRect(hwd, out var rect);
                int w=rect.Right - rect.Left;
                int h=rect.Bottom - rect.Top;
                //int x=rect.Left+Convert.ToInt32(w*xRatio);
                //int y = rect.Top + Convert.ToInt32(h * xRatio);
                int x = cs.ControlManager._isRatio ? Convert.ToInt32(w * xRatio) : Convert.ToInt32(xRatio);
                int y = cs.ControlManager._isRatio ? Convert.ToInt32(h * yRatio) : Convert.ToInt32(yRatio);


                // 构造 lParam 参数：低 16 位为 x 坐标，高 16 位为 y 坐标
                IntPtr lParam = new IntPtr((y << 16) | (x & 0xFFFF));

                // wParam：表示键盘修饰符（如 Ctrl、Shift 等），此处设为 0
                IntPtr wParam = IntPtr.Zero;

                // 1. 记录目标窗口在 Z 顺序中的相对位置：获取其上一个窗口句柄
                //IntPtr prevHwnd = Win32Helper.GetWindow(hwd, 3);

                if (clickType == "Left")
                {
                    // 发送鼠标左键按下消息
                    Win32Helper.SendMessage(hwd, Win32Helper.WM_LBUTTONDOWN, wParam, lParam);
                    // 延时 10 毫秒，模拟自然点击
                    //Thread.Sleep(50);
                    // 发送鼠标左键抬起消息
                    //Win32Helper.SendMessage(hwd, Win32Helper.WM_LBUTTONUP, wParam, lParam);
                }
                else
                {
                    Win32Helper.SendMessage(hwd, Win32Helper.WM_RBUTTONDOWN, wParam, lParam);
                    //Thread.Sleep(50);
                   // Win32Helper.SendMessage(hwd, Win32Helper.WM_RBUTTONUP, wParam, lParam);
                }

                //if (prevHwnd != IntPtr.Zero)
                //{
                //    Win32Helper.SetWindowPos(hwd, prevHwnd,
                //        0, 0, 0, 0,
                //         0x0002 | 0x0001 | 0x0010);
                //}

            }
        }


        public static void CopyControl_ClickUp(long except_id, double xRatio, double yRatio, string clickType = "Left")
        {
            var idslist = runing_xchrome.Keys.ToArray();
            foreach (var id in idslist)
            {
                if (id == except_id) continue;
                var xchrome = runing_xchrome[id];
                IntPtr hwd = (IntPtr)xchrome.Hwnd;
                Win32Helper.GetWindowRect(hwd, out var rect);
                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;
                //int x=rect.Left+Convert.ToInt32(w*xRatio);
                //int y = rect.Top + Convert.ToInt32(h * xRatio);
                int x = cs.ControlManager._isRatio ? Convert.ToInt32(w * xRatio) : Convert.ToInt32(xRatio);
                int y = cs.ControlManager._isRatio ? Convert.ToInt32(h * yRatio) : Convert.ToInt32(yRatio);


                // 构造 lParam 参数：低 16 位为 x 坐标，高 16 位为 y 坐标
                IntPtr lParam = new IntPtr((y << 16) | (x & 0xFFFF));

                // wParam：表示键盘修饰符（如 Ctrl、Shift 等），此处设为 0
                IntPtr wParam = IntPtr.Zero;

                // 1. 记录目标窗口在 Z 顺序中的相对位置：获取其上一个窗口句柄
                //IntPtr prevHwnd = Win32Helper.GetWindow(hwd, 3);

                if (clickType == "Left")
                {
                    // 发送鼠标左键按下消息
                    //Win32Helper.SendMessage(hwd, Win32Helper.WM_LBUTTONDOWN, wParam, lParam);
                    // 延时 10 毫秒，模拟自然点击
                    //Thread.Sleep(50);
                    // 发送鼠标左键抬起消息
                    Win32Helper.SendMessage(hwd, Win32Helper.WM_LBUTTONUP, wParam, lParam);
                }
                else
                {
                    //Win32Helper.SendMessage(hwd, Win32Helper.WM_RBUTTONDOWN, wParam, lParam);
                    //Thread.Sleep(50);
                    Win32Helper.SendMessage(hwd, Win32Helper.WM_RBUTTONUP, wParam, lParam);
                }

                //if (prevHwnd != IntPtr.Zero)
                //{
                //    Win32Helper.SetWindowPos(hwd, prevHwnd,
                //        0, 0, 0, 0,
                //         0x0002 | 0x0001 | 0x0010);
                //}

            }
        }


        /// <summary>
        /// 群控复制操作方法，已经是task内无需异步
        /// 滚轮
        /// </summary>
        /// <param name="except_id"></param>
        /// <param name="xRatio"></param>
        /// <param name="yRatio"></param>
        /// <param name="Delta"></param>
        public static void CopyControl_Wheel(long except_id, double xRatio, double yRatio,int Delta)
        {
            var idslist = runing_xchrome.Keys.ToArray();
            foreach (var id in idslist)
            {
                if (id == except_id) continue;
                var xchrome = runing_xchrome[id];
                IntPtr hwd = (IntPtr)xchrome.Hwnd;
                Win32Helper.GetWindowRect(hwd, out var rect);
                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;
                //int x=rect.Left+Convert.ToInt32(w*xRatio);
                //int y = rect.Top + Convert.ToInt32(h * xRatio);
                int x = rect.Left+ (cs.ControlManager._isRatio ? Convert.ToInt32(w * xRatio) : Convert.ToInt32(xRatio));
                int y =rect.Top+ (cs.ControlManager._isRatio ? Convert.ToInt32(h * yRatio) : Convert.ToInt32(yRatio));



                IntPtr lParam = new IntPtr((y << 16) | (x & 0xFFFF));
                // 低 16 位为键盘修饰键状态（一般设为 0）
                int wParamValue = (Delta << 16);
                IntPtr wParam = new IntPtr(wParamValue);
                // 向目标窗口发送 WM_MOUSEWHEEL 消息
                bool result =Win32Helper.PostMessage(hwd, 0x020A, wParam, lParam);
               
                
            }
        }

        public static void CopyControl_keyPress(long except_id,char _char)
        {
            var idslist = runing_xchrome.Keys.ToArray();
            foreach (var id in idslist)
            {
                if (id == except_id) continue;
                var xchrome = runing_xchrome[id];
                IntPtr hwd = (IntPtr)xchrome.Hwnd;


                // 将字符转换为 WM_CHAR 消息的 wParam 值
                IntPtr wParam = new IntPtr(_char);
                // 对于 WM_CHAR，此处 lParam 中包含附加信息，可简单设为 0
                IntPtr lParam = IntPtr.Zero;

                // 使用 PostMessage 发送 WM_CHAR 消息
                bool success = Win32Helper.PostMessage(hwd, 0x0102, wParam, lParam);

            }

        }

        public static void CopyControl_keyDownOther(long except_id, Keys key)
        {
            var idslist = runing_xchrome.Keys.ToArray();
            foreach (var id in idslist)
            {
                if (id == except_id) continue;
                var xchrome = runing_xchrome[id];
                IntPtr hwd = (IntPtr)xchrome.Hwnd;
                Win32Helper.PostMessage(hwd, 0x0100, new IntPtr((int)key), IntPtr.Zero);

            }

        }

        
        
        public static void CopyControl_MouseMove(long except_id, double xRatio, double yRatio)
        {
            var idslist = runing_xchrome.Keys.ToArray();
            foreach (var id in idslist)
            {
                if (id == except_id) continue;
                var xchrome = runing_xchrome[id];
                IntPtr hwd = (IntPtr)xchrome.Hwnd;
                int lParam = (Convert.ToInt32(xRatio) & 0xFFFF) | ((Convert.ToInt32(yRatio)& 0xFFFF) << 16);
                IntPtr lParamPtr = new IntPtr(lParam);
                // 如果需要，wParam 可以携带一些标志，如鼠标按钮状态，这里暂时设置为 0
                IntPtr wParam = IntPtr.Zero;
                //Debug.WriteLine(xRatio+",,"+yRatio);
                // 通过 SendMessage 将 WM_MOUSEMOVE 消息发送到目标窗口
                Win32Helper.SendMessage(hwd, 0x0200, wParam, lParamPtr);

            }
        }

        public static void CopyControl_MouseHover(long except_id, double xRatio, double yRatio)
        {
            var idslist = runing_xchrome.Keys.ToArray();
            foreach (var id in idslist)
            {
                if (id == except_id) continue;
                var xchrome = runing_xchrome[id];
                IntPtr hwd = (IntPtr)xchrome.Hwnd;
                //Debug.WriteLine(xRatio+","+yRatio);
                int lParam = (Convert.ToInt32(yRatio) << 16) | (Convert.ToInt32(xRatio) & 0xFFFF);
                Win32Helper.PostMessage(hwd, 0x02A1, IntPtr.Zero, new IntPtr(lParam));

                

            }
        }

        /// <summary>
        /// 重新排列窗口
        /// </summary>
        /// <param name="type">0平铺，1重叠</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="licount"> 列数</param>
        /// <param name="screen"></param>
        /// <returns></returns>
        public static async Task ArrayChromes(int type,string width,string height,string licount, int screenIndex)
        {
            //获得屏幕
            Screen screen = Screen.AllScreens[screenIndex];
            //屏幕位置 workarea
            var workarea = screen.WorkingArea;
            //自定义
            bool isCustom = (width != "" && height != "" && licount != "");
            if (isCustom) { type = 0; }

            //平铺
            if (type == 0)
            {
                //长宽不填，表示默认
                if (!isCustom)
                {
                    int _screen_width = screen.Bounds.Width;
                    int _screen_height = screen.Bounds.Height;
                    int startTop = workarea.Top;
                    int startLeft = workarea.Left;

                    var windowlist = ComputeAdaptiveWindowPositions(_screen_width, _screen_height, runing_xchrome.Count);
                    var idslist = runing_xchrome.Keys.ToArray();

                    for (int i = 0; i < windowlist.Count; i++)
                    {
                        var xchrome_id = idslist[i];
                        var windowPos = windowlist[i];
                        XChrome xchrome = runing_xchrome[xchrome_id];
                        IntPtr hwd = (IntPtr)xchrome.Hwnd;
                        Win32Helper.ChangeWindowPos(hwd, startLeft + windowPos.Left, startTop + windowPos.Top, windowPos.Width, windowPos.Height);
                        //await Task.Delay(200);
                    }
                }
                //自定义填写
                else
                {
                    int _width = width.TryToInt32(100);
                    int _height = height.TryToInt32(100);
                    int _licount = licount.TryToInt32(3);

                    int startTop = workarea.Top;
                    int startLeft = workarea.Left;
                    int current_left = startLeft;
                    var idslist = runing_xchrome.Keys.ToArray();
                    for (int i = 0; i < idslist.Length; i++)
                    {
                        var xchrome_id = idslist[i];
                        XChrome xchrome = runing_xchrome[xchrome_id];
                        IntPtr hwd = (IntPtr)xchrome.Hwnd;
                        current_left = startLeft + (i % _licount) * _width;
                        //需要换行
                        if (i % _licount == 0 && i != 0)
                        {
                            startTop += _height;
                            current_left = startLeft;
                        }
                        Win32Helper.ChangeWindowPos(hwd, current_left, startTop, _width, _height);
                        //await Task.Delay(200);
                    }
                }
            }
            //重叠排序
            else
            {
                var idslist = runing_xchrome.Keys.ToArray();
                int current_left = workarea.Left;
                int _width = workarea.Width - idslist.Length*30;
                int _height=workarea.Height - 20;

                for (int i = 0; i < idslist.Length; i++)
                {
                    var xchrome_id = idslist[i];
                    XChrome xchrome = runing_xchrome[xchrome_id];
                    IntPtr hwd = (IntPtr)xchrome.Hwnd;
                    
                    Win32Helper.ChangeWindowPos(hwd, current_left, workarea.Top, _width, _height);
                    current_left += 30;
                    //await Task.Delay(200);
                }
            }
            
        }

        /// <summary>
        /// 调整浏览器内部页面大小
        /// </summary>
        /// <returns></returns>
        public static async Task AdjustmentView()
        {
            var idslist = runing_xchrome.Keys.ToArray();
            for (int i = 0; i < idslist.Length; i++)
            {
                var xchrome_id = idslist[i];
                XChrome xchrome = runing_xchrome[xchrome_id];
                await AdjustmentOneView(xchrome);
            }
        }

        private static async Task AdjustmentOneView(XChrome xchrome)
        {
            IntPtr hwd = (IntPtr)xchrome.Hwnd;
            //总窗口
            Win32Helper.GetWindowRect(hwd, out var ret);
            //内容窗口
            var legacywindow_hwd = Win32Helper.FindWindowEx(hwd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window");
            Win32Helper.RECT clientRect = new Win32Helper.RECT();
            Win32Helper.GetWindowRect(legacywindow_hwd, out clientRect);
            int ww = clientRect.Right - clientRect.Left;
            int hh = clientRect.Bottom - clientRect.Top;
            //Debug.WriteLine(ww+",,"+hh);
            xchrome.ViewportSize=new ViewportSize { Width = ww, Height = hh };
            foreach (var page in xchrome.BrowserContext.Pages)
            {
                await page.SetViewportSizeAsync(ww, hh);
            }
            //chrome的一个bug，修复
            Win32Helper.ChangeWindowPos(hwd, ret.Left, ret.Top, ret.Right - ret.Left, ret.Bottom - ret.Top);

        }



     
        /// <summary>
        /// 动态调整内部view的jober
        /// </summary>
        public static async Task jober_AdjustmentView()
        {
           
            //激活窗口
            IntPtr foreHwd=(IntPtr)win32.Win32Helper.GetForegroundWindow();
            if (foreHwd == IntPtr.Zero) { return; }
            //如果激活窗口是
            bool isXchrome = false;
            XChrome xchrome = null;
            var idslist = runing_xchrome.Keys.ToArray();
            for(int i = 0; i < idslist.Length; i++)
            {
                var id = idslist[i];
                if (!runing_xchrome.ContainsKey(id)) return;
                xchrome = runing_xchrome[id];
                IntPtr hwd = (IntPtr)xchrome.Hwnd;
                if (hwd == foreHwd)
                {
                    isXchrome = true;
                    break;
                }
            }
            if (!isXchrome) { return; }
            //Debug.WriteLine("开始调整1..");
            //开始检测调整窗口
            var legacywindow_hwd = Win32Helper.FindWindowEx(foreHwd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window");
            Win32Helper.RECT clientRect = new Win32Helper.RECT();
            Win32Helper.GetWindowRect(legacywindow_hwd, out clientRect);
            int ww = clientRect.Right - clientRect.Left;
            int hh = clientRect.Bottom - clientRect.Top;
            //总窗口
            Win32Helper.GetWindowRect(foreHwd, out var ret);
            if (ww != xchrome.LegacyWindowWidth || hh != xchrome.LegacyWindowHeight)
            {
                //Debug.WriteLine("开始调整2..");
                

                xchrome.LegacyWindowWidth = ww;
                xchrome.LegacyWindowHeight = hh;
                //xchrome.ViewportSize = new ViewportSize { Width = ww, Height = hh };
                foreach (var page in xchrome.BrowserContext.Pages)
                {
                    //page.set
                    await page.SetViewportSizeAsync(ww, hh);
                }
                Win32Helper.ChangeWindowPos(foreHwd, ret.Left, ret.Top, ret.Right - ret.Left, ret.Bottom - ret.Top);
            }

            if (ControlManager.IsRunning())
            {
                ControlManager.SetMainXchrome(xchrome.Id, foreHwd,ret.Left,ret.Right,ret.Top,ret.Bottom); 
            }

            


        }
     


        /// <summary>
        /// 计算位置分布
        /// </summary>
        /// <param name="screenWidth"></param>
        /// <param name="screenHeight"></param>
        /// <param name="windowCount"></param>
        /// <returns></returns>
        private static List<XWindowRect> ComputeAdaptiveWindowPositions(int screenWidth, int screenHeight, int windowCount)
        {
            List<XWindowRect> windows = new List<XWindowRect>();
            if (windowCount <= 0)
                return windows;

            // 根据窗口总数确定最大列数
            int maxColumns = (int)Math.Ceiling(Math.Sqrt(windowCount));
            // 根据最大列数确定总行数
            int rows = (int)Math.Ceiling((double)windowCount / maxColumns);

            // 统一窗口大小（均分屏幕）
            int windowWidth = screenWidth / maxColumns;
            int windowHeight = screenHeight / rows;

            int windowIndex = 0;
            for (int row = 0; row < rows; row++)
            {
                // 如果为最后一行，窗口数使用剩余的；否则当前行使用满列数
                int windowsInRow = (row == rows - 1 && windowCount % maxColumns != 0)
                                   ? windowCount % maxColumns
                                   : maxColumns;

                // 此处采用左对齐，所以水平偏移始终为 0
                int offsetX = 0;

                for (int col = 0; col < windowsInRow; col++)
                {
                    int left = offsetX + col * windowWidth;
                    int top = row * windowHeight;
                    windows.Add(new XWindowRect(left, top, windowWidth, windowHeight));
                    windowIndex++;
                    if (windowIndex >= windowCount)
                        break;
                }
            }

            

            return windows;
        }



        /// <summary>
        /// 关闭所有浏览器
        /// </summary>
        /// <returns></returns>
        public static async Task CloseAllChrome()
        {
            List<XChrome>? list = null;
            lock (_lock_runing_xchrome) {
                list = runing_xchrome.Select(it=>it.Value).ToList();
            }
            if (list == null) return;
            for (int i = 0; i < list.Count; i++) {
                var bcontext= list[i].BrowserContext;
                try
                {
                    await bcontext?.CloseAsync();
                }
                catch (Exception ex) { }
            }
        }

       


      


        /// <summary>
        /// 通过proxy的字符串，变成 Proxy对象
        /// </summary>
        /// <param name="proxy"></param>
        /// <returns></returns>
        private static Proxy? getProxy(string proxy = "")
        {
            if (proxy != "")
            {
                if (!proxy.StartsWith("http") && !proxy.StartsWith("socks5"))
                {
                    proxy = "http://" + proxy;
                }
                var _proxy = new Proxy();
                string[] pp = proxy.Split(":");
                _proxy.Server = pp[0] + ":" + pp[1] + ":" + pp[2];
                if (pp.Length > 3)
                {
                    _proxy.Username = pp[3];
                }
                if (pp.Length > 4)
                {
                    _proxy.Password = pp[4];
                }
                return _proxy;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 程序结束的时候调用
        /// </summary>
        /// <returns></returns>
        public static async Task DisposePlayWright()
        {
            if (playwright == null) return;
            try { playwright.Dispose(); } catch { } 
        }




       
    }

    public class XChrome
    {
        public long Hwnd { get; set; } = 0;

        public IBrowserContext? BrowserContext { get; set; } = null;

        public long Id { get; set; } = 0;

        public string Name { get; set; } = "";
        public string DataPath { get; set; } = "";
        public string Proxy { get; set; } = "";
        public string UserAgent { get; set; } = "";

        public string Evns { get; set; } = "";

        public string Extensions { get; set; } = "";

        public ViewportSize? ViewportSize { get; set; } = null;

        //内部页面大小
        public int LegacyWindowWidth = 0;
        public int LegacyWindowHeight = 0;

    }

    public struct XWindowRect
    {
        public int Left;   // 窗口左上角的 X 坐标
        public int Top;    // 窗口左上角的 Y 坐标
        public int Width;  // 窗口宽度
        public int Height; // 窗口高度

        public XWindowRect(int left, int top, int width, int height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return $"Left: {Left}, Top: {Top}, Width: {Width}, Height: {Height}";
        }
    }
}
