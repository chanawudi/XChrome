#region copyright
/**
// --------------------------------------------------------------------------------
// 文件名：EditChrome.xaml.cs
// 作者：刹那 https://x.com/chanawudi
// 公司：https://x.com/chanawudi
// 更新日期：2025，2，27，13:55
// 版权所有 © Your Company. 保留所有权利。
// --------------------------------------------------------------------------------
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XChrome.cs.db;
using XChrome.cs.tools.YTools;

namespace XChrome.forms
{
    /// <summary>
    /// EditChrome.xaml 的交互逻辑
    /// </summary>
    public partial class EditChrome : AdonisUI.Controls.AdonisWindow
    {
        private string proxytext = "";
        /// <summary>
        /// chrome id ，创建的时候设置-1
        /// </summary>
        private long id = -1;
        //是否点击了确认按钮
        public bool isSuccess = false;
        public EditChrome(long id)
        {
            InitializeComponent();
            this.id = id;
        }

        /// <summary>
        /// 随机生成user-agent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rand_useragent_Click(object sender, RoutedEventArgs e)
        {
            string ag = GenerateRandomChromeUserAgent();
            useragent_text.Text = ag;
        }

        /// <summary>
        /// 加载分组
        /// </summary>
        /// <returns></returns>
        private async Task loadGroup()
        {
            var db = cs.db.MyDb.DB;
            var list=await db.Queryable<Group>().OrderBy(it=>it.id,SqlSugar.OrderByType.Asc).ToListAsync();
            db.Close();

            foreach (Group group in list) { 
                groupList.Items.Add(group);
            }
        }

        /// <summary>
        /// 修改的时候加载数据
        /// </summary>
        /// <returns></returns>
        private async Task loadData()
        {
            var db = cs.db.MyDb.DB;
            var o=await db.Queryable<Chrome>().Where(it => it.id == id).FirstAsync();
            db.Close ();
            if (o == null)
            {
                MainWindow.Toast_Error("没有找到环境");
                return;
            }

            chromeName.Text=o.name;
            useragent_text.Text=o.userAgent;
            remark_text.Text=o.remark;
            proxy_text.Text = o.proxy;
            proxytext = o.proxyText;
            chrome_id_text.Text=id.ToString();
            othertxt.Text = o.envs;
            exlist_text.Text = (o.extensions ?? "").Replace("|","\r\n");
            //设置分组
            long groupid = o.groupId.Value;
            var group=groupList.Items.OfType<Group>().FirstOrDefault(it=>it.id == groupid);
            if(group != null)groupList.SelectedItem = group;
        }


        private async void AdonisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //exlist_text
            //加载分组
            await loadGroup();

            if (id != -1)
            {
                //加载
                okbtntxt.Text = "确定修改";
                id_tip.Visibility = Visibility.Visible;
                id_value.Visibility = Visibility.Visible;
                await loadData();
            }
            else
            {
                //创建
                //随机useragent
                string ag = YUtils.GetRandomUserAgent(YUtils.GetTime13(DateTime.Now).ToString());
                useragent_text.Text = ag;
                okbtntxt.Text = "确定创建";
            }

        }

        /// <summary>
        /// 测试代理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void check_proxy_btn_Click(object sender, RoutedEventArgs e)
        {
            string proxy = proxy_text.Text;
            if (proxy.StartsWith("["))
            {
                MainWindow.Toast_Error("这种格式正在开发中..");
                return;
            }
            proxy = proxy.Replace("：",":");
            check_proxy_btn.IsEnabled = false;
            check_proxy_btn_text.Text = "...";
            string x = "";
            try
            {
                x = await new YHttp().Get().Url("https://myip.ipip.net/").Proxy(proxy).GetAsync_String();
            }
            catch (Exception ee)
            {
                check_proxy_result.Text = ee.Message;
                check_proxy_btn.IsEnabled = true;
                check_proxy_btn_text.Text = "测试代理";
                return;
            }
            if (x == null) {
                check_proxy_result.Text = "获取错误！";
                check_proxy_btn.IsEnabled = true;
            }
            x = x.Replace("\r\n", "\n").Replace("\n", "");
            if (x.IndexOf("来自于：") >= 0)
            {
                proxytext = x.Substring(x.IndexOf("来自于：")+4);
            }
            check_proxy_result.Text = x;
            check_proxy_btn.IsEnabled = true;
            check_proxy_btn_text.Text = "测试代理";
        }

        /// <summary>
        /// 提交
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string name= chromeName.Text.Trim();
            string agent= useragent_text.Text.Trim();
            long groupId = groupList.SelectedItem==null?1:( groupList.SelectedItem as Group).id;
            string remark = remark_text.Text;
            string proxy = proxy_text.Text.Trim();

            Chrome c = new Chrome();
            c.name = name;
            c.userAgent = agent;
            c.proxy = proxy;
            c.proxyText = proxytext;
            c.groupId = groupId;
            c.createDate = DateTime.Now;
            c.doDate = DateTime.Now;
            c.cookie = "";
            c.remark = remark;
            c.tags = "";
            c.envs=othertxt.Text.Trim();
            c.extensions =exlist_text.Text.Trim().Replace("\r\n","|").Replace("\n","|");
            //othertxt

            var db = MyDb.DB;
            try
            {
                if (id == -1)
                {
                    db.Insertable<Chrome>(c).ExecuteCommand();
                }
                else
                {
                    c.id = id;
                    db.Updateable<Chrome>(c).WhereColumns(it => it.id).ExecuteCommand();
                }
            }catch(Exception ev)
            {
                db.Close();
                MainWindow.Toast_Error(ev.Message);
                return;
            }
            
            db.Close();
            MainWindow.Toast_Success("操作成功！");
            isSuccess = true;
            this.Close();

        }

        /// <summary>
        /// 随机生成其他环境
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rand_other_btn_Click(object sender, RoutedEventArgs e)
        {
            var x = GenerateRandomEnvironment();
            string y=Newtonsoft.Json.JsonConvert.SerializeObject(x);
            othertxt.Text = y;
        }

        /// <summary>
        /// 随机生成浏览器环境参数，返回一个 Dictionary，
        /// 包含：UserAgent、Locale、TimezoneId、ViewportSize、DeviceScaleFactor、IsMobile、HasTouch、ExtraHTTPHeaders、Geolocation 等。
        /// </summary>
        /// <returns>包含随机环境配置的 Dictionary</returns>
        private  Dictionary<string, object> GenerateRandomEnvironment()
        {
            var random = new Random();
            var environments = new Dictionary<string, object>();

           

            // 2. 随机区域语言 Locale
            var locales = new[] { "zh-CN", "en-US", "fr-FR", "de-DE", "ja-JP", "es-ES" };
            var locale = locales[random.Next(locales.Length)];

            // 3. 随机时区 TimezoneId
            var timezones = new[] { "Asia/Shanghai", "America/New_York", "Europe/London", "Asia/Tokyo", "Europe/Berlin" };
            var timezoneId = timezones[random.Next(timezones.Length)];

            // 4. 随机视口尺寸（宽度：800-1920, 高度：600-1080）
            int width = random.Next(800, 1921);
            int height = random.Next(600, 1081);
            var viewport = new Dictionary<string, int>
            {
                { "width", width },
                { "height", height }
            };

            // 5. 随机设备像素比例 DeviceScaleFactor（常见值：1.0, 1.25, 1.5, 2.0）
            var scaleFactors = new double[] { 1.0, 1.25, 1.5, 2.0 };
            var deviceScaleFactor = scaleFactors[random.Next(scaleFactors.Length)];

            // 6. 随机是否为移动设备 isMobile 和是否支持触摸 hasTouch
            bool isMobile = random.Next(2) == 0; // 50% 概率
                                                 // 如果是移动设备，则一般支持触摸
            bool hasTouch = isMobile || (random.Next(2) == 0);

            // 7. 根据 Locale 随机生成 Accept-Language HTTP header
            string acceptLanguage;
            switch (locale)
            {
                case "zh-CN":
                    acceptLanguage = "zh-CN,zh;q=0.9,en;q=0.8";
                    break;
                case "en-US":
                    acceptLanguage = "en-US,en;q=0.9";
                    break;
                case "fr-FR":
                    acceptLanguage = "fr-FR,fr;q=0.9,en;q=0.8";
                    break;
                case "de-DE":
                    acceptLanguage = "de-DE,de;q=0.9,en;q=0.8";
                    break;
                case "ja-JP":
                    acceptLanguage = "ja-JP,ja;q=0.9,en;q=0.8";
                    break;
                case "es-ES":
                    acceptLanguage = "es-ES,es;q=0.9,en;q=0.8";
                    break;
                default:
                    acceptLanguage = locale;
                    break;
            }
            var extraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept-Language", acceptLanguage }
            };

            // 8. 随机地理位置（使用一组常见城市的坐标）
            var geolocations = new (double latitude, double longitude)[]
            {
            (31.2304, 121.4737),  // 上海
            (40.7128, -74.0060),  // 纽约
            (51.5074, -0.1278),   // 伦敦
            (35.6895, 139.6917),  // 东京
            (48.8566, 2.3522),    // 巴黎
            (52.5200, 13.4050)    // 柏林
            };
            var geo = geolocations[random.Next(geolocations.Length)];
            var geolocation = new Dictionary<string, double>
            {
                { "Latitude", geo.latitude },
                { "Longitude", geo.longitude }
            };

            // 将所有配置参数放入字典
            environments["Locale"] = locale;
            environments["TimezoneId"] = timezoneId;
            environments["ViewportSize"] = viewport;
            environments["DeviceScaleFactor"] = deviceScaleFactor;
            environments["IsMobile"] = isMobile;
            environments["HasTouch"] = hasTouch;
            environments["ExtraHTTPHeaders"] = extraHTTPHeaders;
            environments["Geolocation"] = geolocation;

            return environments;
        }

        /// <summary>
        /// 随机生成一个 Chrome 浏览器的 User-Agent 字符串
        /// 格式示例：
        /// Mozilla/5.0 (平台) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{major}.0.{build}.{patch} Safari/537.36
        /// </summary>
        /// <returns>随机生成的 Chrome User-Agent 字符串</returns>
        public static string GenerateRandomChromeUserAgent()
        {
            Random rnd = new Random();
            string[] Platforms = new[]
            {
                "Windows NT 10.0; Win64; x64",
                "Macintosh; Intel Mac OS X 10_15_7",
                "X11; Linux x86_64",
            };

            // 随机选择一个平台
            string platform = Platforms[rnd.Next(Platforms.Length)];

            // 生成 Chrome 版本号的各个数字部分
            // major：Chrome 主版本号，范围设定在 90 到 115 之间
            int major = rnd.Next(90, 116); // 随机范围 [90,115]
            // build：生成一个构建号，例如 4000 到 5999
            int build = rnd.Next(4000, 6000);
            // patch：生成一个补丁版本号，例如 10 到 149
            int patch = rnd.Next(10, 150);

            // 构造 Chrome 版本号，可以固定 minor 版本为 0
            string chromeVersion = $"{major}.0.{build}.{patch}";

            // 组装 User-Agent 字符串
            string userAgent = $"Mozilla/5.0 ({platform}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion} Safari/537.36";
            return userAgent;
        }
    }
}
