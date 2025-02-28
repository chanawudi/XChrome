#region copyright
/**
// --------------------------------------------------------------------------------
// 文件名：CManager.xaml.cs
// 作者：刹那 https://x.com/chanawudi
// 公司：https://x.com/chanawudi
// 更新日期：2025，2，27，13:56
// 版权所有 © Your Company. 保留所有权利。
// --------------------------------------------------------------------------------
*/
#endregion
using AdonisUI.Controls;
using AutoHotkey.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XChrome.cs;
using XChrome.cs.db;
using XChrome.cs.tools.YTools;
using XChrome.cs.win32;


namespace XChrome.pages
{
    /// <summary>
    /// CManager.xaml 的交互逻辑
    /// </summary>
    public partial class CManager : Page
    {
        // 集合数据将会绑定到 DataGrid 上
        public ObservableCollection<TableItem> TableItems { get; set; } = null;
        public ButtonStatus buttonStatus { get; set; } = new ButtonStatus();

        public CManager()
        {
            InitializeComponent();
        }
        public static CManager _cmanager=null;
        private int pageIndex = 0;
        private int totalPages = 0;
        private string searchKey = "";
        private long searchGroupId = 0;

        
        /// <summary>
        /// 搜索加载
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="groupId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private async Task Search(int pageIndex=1, long groupId=-1,string text="")
        {
            searchKey = text;
            searchGroupId=groupId;
            this.pageIndex= pageIndex;
            RefAsync<int> totalCount = 0;
            int pageSize = 15;

            var db = MyDb.DB;
            var list=await db.Queryable<Chrome,Group>((c,g)=>new JoinQueryInfos(JoinType.Left,c.groupId==g.id))
                .OrderBy((c,g)=>c.id,SqlSugar.OrderByType.Asc)
                .WhereIF(groupId!=-1,(c,g)=>c.groupId==groupId)
                .WhereIF(text!="",(c,g)=>c.name.Contains(text) || c.remark.Contains(text) || c.proxyText.Contains(text) || g.name.Contains(text))
                .Select((c, g) =>new {
                    c.id,c.name,c.proxyText,c.proxy,c.groupId,c.remark,c.createDate,c.doDate,groupName=g.name
                })
                .ToPageListAsync(pageIndex, pageSize,  totalCount);
            db.Close();
            DataContext = null;
            TableItems = null;
            TableItems = new ObservableCollection<TableItem>();
            foreach (var c in list) {
                TableItem t=new TableItem();
                t.Id = c.id;
                t.Ip = c.proxy+"\n"+c.proxyText;
                t.Name = c.name;
                t.Remark = c.remark;
                t.CreateDate = c.createDate.Value;
                t.DoDate = c.doDate.Value;
                t.Check = false;
                t.Group = c.groupName;
                TableItems.Add(t);  
            }
            //绑定
            DataContext = this;

            //绑定分页
            totalPages = (totalCount + pageSize - 1) / pageSize;
            tbCurrentPage.Text = "/ " + totalPages;
            currentPage.Text=pageIndex.ToString();
            //分页enable控制
            if (pageIndex == 1)
            {
                pre_btn.IsEnabled = false;
            }
            else
            {
                pre_btn.IsEnabled = true;
            }
            if (pageIndex == totalPages)
            {
                next_btn.IsEnabled = false;
            }
            else
            {
                next_btn.IsEnabled = true;
            }
            
        }

        /// <summary>
        /// 加载分组
        /// </summary>
        /// <returns></returns>
        private async Task loadGroup()
        {
            var db = cs.db.MyDb.DB;
            var list = await db.Queryable<Group>().OrderBy(it => it.id, SqlSugar.OrderByType.Asc).ToListAsync();
            db.Close();
            Group ga= new Group() { name="所有分组",id=-1};
            groupList.Items.Add(ga);
            foreach (Group group in list)
            {
                groupList.Items.Add(group);
            }

            //加载屏幕
            // 遍历各个屏幕输出详细信息（可选）
            foreach (Screen screen in Screen.AllScreens)
            {
                var c = new ComboBoxItem();
                c.Content = "["+(screen.Primary?"主":"副") +"]" + screen.Bounds.Width+"x"+screen.Bounds.Height+ "["+screen.DeviceName+"]";
                screen_com.Items.Add(c);
            }

            //排列方式

        }

        /// <summary>
        /// 刷新按钮状态
        /// </summary>
        /// <param name="afterNotifyOnline">是否是 通知在线与否 后执行</param>
        private void flushButtons(bool afterNotifyOnline=false)
        {
            //通过消息来，一般是比如用户手动关闭浏览器等
            if (afterNotifyOnline) {
                var hasAnyRuning = TableItems.Where(it => it.IsRunning).Any();
                if (hasAnyRuning) {
                    buttonStatus.Edit = false;
                    buttonStatus.Copy = false;
                    buttonStatus.Del = false;
                    buttonStatus.Run = false;
                    //buttonStatus.Control = false;
                    //buttonStatus.Stop = true;
                    //buttonStatus.Array = true;
                    Dispatcher.Invoke(() => {
                        //表格也不让操作
                        dataGrid.IsEnabled = false;
                    });
                    return;
                }
                else
                {
                    Dispatcher.Invoke(() => {
                        dataGrid.IsEnabled = true;
                    });
                }
            }
            
            var c = TableItems.Where(it => it.Check == true).Count();
            //没有选中，全部黑
            if (c == 0)
            {
                buttonStatus.DisableAll();
            }
            //选中一个，全开
            else if (c == 1)
            {
                buttonStatus.Edit = true;
                buttonStatus.Copy = true;
                buttonStatus.Del = true;
                buttonStatus.Run = true;
                buttonStatus.Control = true;
                buttonStatus.Stop = true;
                buttonStatus.Array = true;
            }
            //选中多个，只有运行和脚本
            else
            {
                buttonStatus.Edit = false;
                buttonStatus.Copy = false;
                buttonStatus.Del = true;
                buttonStatus.Run = true;
                buttonStatus.Control = true;
                buttonStatus.Stop = true;
                buttonStatus.Array = true;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //还没启动
            //script_group.IsEnabled = false;

            _cmanager = this;
            //加载group
            await loadGroup();

            //列出列表
            await Search(1);

            flushButtons();

           

        }



        private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

            int row = -1;
            var _dataGrid = (System.Windows.Controls.DataGrid)sender;
            // 遍历新选中的单元格
            foreach (DataGridCellInfo cellInfo in e.AddedCells)
            {
                object rowData = cellInfo.Item;
                row = _dataGrid.Items.IndexOf(rowData);
                break;
            }
            if (row == -1) return;
            DataGridRow r = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(row);
            //不然选中
            r.IsSelected = false;
            //改变数据
            
            var t=TableItems[row];
            if(t.IsCheckEnble)
                t.Check =!t.Check;
            //判断按钮显示
            flushButtons();

        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            long groupid= groupList.SelectedItem == null ? 1 : (groupList.SelectedItem as Group).id;
            string text = search_text.Text.Trim();
            await Search(1, groupid, text);
        }


        /// <summary>
        /// 全选勾选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckAll_Click(object sender, RoutedEventArgs e)
        {
            bool ischeck = ((System.Windows.Controls.CheckBox)sender).IsChecked ?? false;
            for (int i = 0; i < TableItems.Count; i++)
            {
                TableItems[i].Check = ischeck;
            }
            //判断按钮显示
            flushButtons();
        }


        /// <summary>
        /// 下一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void next_btn_Click(object sender, RoutedEventArgs e)
        {
            if (totalPages == pageIndex) return;
            await Search(pageIndex + 1, searchGroupId, searchKey);
        }
        /// <summary>
        /// 上一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pre_btn_Click(object sender, RoutedEventArgs e)
        {
            if(pageIndex == 1) return;
            await Search(pageIndex - 1, searchGroupId, searchKey);
        }

        /// <summary>
        /// 页码回车
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void currentPage_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var page=currentPage.Text.TryToInt32(-1);
                if(page == -1) page=1;
                if (page < 1) page = 1;
                if(page>totalPages) page=totalPages;
                await Search(page, searchGroupId, searchKey);
                // 如果需要阻止事件继续传递，可以将 Handled 置为 true
                e.Handled = true;
            }
        }

        /// <summary>
        /// 外部添加环境后回调
        /// </summary>
        /// <returns></returns>
        public async Task addOver()
        {
            long groupid = groupList.SelectedItem == null ? 1 : (groupList.SelectedItem as Group).id;
            string text = search_text.Text.Trim();
            await Search(pageIndex, groupid, text);
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            var c = TableItems.Where(it => it.Check == true).First();
            long id = c.Id;

            forms.EditChrome ec = new forms.EditChrome(id);
            ec.Owner = Window.GetWindow(this);
            ec.ShowDialog();
            //刷新管理器
            if(ec.isSuccess)
                await addOver();
            
        }

        /// <summary>
        /// 复制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void copy_btn_Click(object sender, RoutedEventArgs e)
        {
            var i = TableItems.Where(it => it.Check == true).First();
            if (i == null) return;
            long cid=i.Id;
            var db = MyDb.DB;
            var chrome=await db.Queryable<Chrome>().Where(it=>it.id==cid).FirstAsync();

            if (i == null)
            {
                db.Close();
                return;
            }
            chrome.name = "_" + chrome.name;
            chrome.id = 0;
           
           
            try
            {
                await db.Insertable<Chrome>(chrome).ExecuteCommandAsync();
            }
            catch (Exception ee)
            {
                db.Close();
                MainWindow.Toast_Error(ee.Message);
                return;
            }

            db.Close();
            MainWindow.Toast_Success("复制成功！");
            await Search(pageIndex, searchGroupId, searchKey);
            flushButtons();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void del_btn_Click(object sender, RoutedEventArgs e)
        {
            var ilist = TableItems.Where(it => it.Check == true).Select(it => it.Id).ToList();
            if (ilist == null) return;

            //删除提示
            AdonisUI.Controls.MessageBoxResult result = AdonisUI.Controls.MessageBox.Show(
                "删除后将删除所有环境的数据，不能还原，确定要删除吗？",         
                "操作确认",                    
                AdonisUI.Controls.MessageBoxButton.OKCancel,     
                AdonisUI.Controls.MessageBoxImage.Question       
            );
            if (result == AdonisUI.Controls.MessageBoxResult.Cancel) return;


            //删除数据库
            var db = cs.db.MyDb.DB;
            try
            {
                await db.Deleteable<Chrome>().Where(it => ilist.Contains(it.id)).ExecuteCommandAsync();
            }
            catch (Exception ex)
            {
                db.Close();
                MainWindow.Toast_Error(ex.Message);
                return;
            }
            db.Close();

            //删除文件
            bool isSuccess = true;
            foreach(long id in ilist)
            {
                string path = System.IO.Path.Combine(cs.Config.chrome_data_path,id.ToString());
                if (!System.IO.Path.Exists(path)) continue;
                //删除文件夹
                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    cs.Loger.Err("删除文件夹时出错: " + ex.Message);
                }
            }

            if (isSuccess)
            {
                MainWindow.Toast_Success("删除成功！");
                await Search(pageIndex,searchGroupId,searchKey);
                flushButtons();
            } 
            else
            {
                MainWindow.Toast_Error("删除中出现问题，请看日志！");
                return;
            }
            
        }


        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_btn_Click(object sender, RoutedEventArgs e)
        {
            var ilist = TableItems.Where(it => it.Check == true).Select(it => it.Id).ToList();
            if (ilist == null) return;
            //查询库
            var db = MyDb.DB;
            var clist =await db.Queryable<Chrome>().Where(it => ilist.Contains(it.id)).ToListAsync();
            db.Close();
            buttonStatus.Array = false;
            buttonStatus.Stop = false;
            buttonStatus.Control = false;
            //打开
            foreach (var it in clist) { 
                cs.XChrome xc=new cs.XChrome();
                xc.Id = it.id;
                xc.Proxy = it.proxy;
                xc.UserAgent = it.userAgent;
                xc.DataPath=System.IO.Path.Combine(cs.Config.chrome_data_path, it.id.ToString());
                xc.Evns=it.envs??"";
                xc.Name = it.name;
                xc.Extensions = it.extensions;
                await Task.Run(async () => {
                    await XChromeManager.OpenChrome(xc);
                    //打开一个后，排列下
                    
                    await Task.Delay(100);
                });
                pl_btn_Click(null, null);
            }
            await Task.Delay(500);
            pl_btn_Click(null, null);
            buttonStatus.Array = true;
            buttonStatus.Stop = true;
            buttonStatus.Control = true;
        }


        /// <summary>
        /// 外部通知他，显示是否运行中
        /// </summary>
        public static void notify_online(List<long> ids,bool isOnline)
        {
            if(_cmanager==null) return;
            _cmanager.notify_online_real(ids,isOnline);
        }
        private void notify_online_real(List<long> ids, bool isOnline)
        {
            List<TableItem> dolins= new List<TableItem>();
            foreach (var item in TableItems)
            {
                foreach(var id in ids)
                {
                    if (id == item.Id)
                    {
                        dolins.Add(item);
                    }
                }
            }
            for (int i = 0; i < dolins.Count; i++)
            {
                var tb= dolins[i];
                tb.IsRunning = isOnline;
                if (isOnline)
                    tb.IsCheckEnble = false;
                else
                    tb.IsCheckEnble=true;
            }

            flushButtons(true);
            
        }


        /// <summary>
        /// 停止按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void stop_btn_Click(object sender, RoutedEventArgs e)
        {
            var runlist = TableItems.Where(it => it.IsRunning).ToList();
            if (runlist.Count == 0) return;
            await XChromeManager.CloseAllChrome();
            //关闭群控，如果启动的话
            ControlManager.CloseControl();
            //关闭hook
            cs.MouseHookServer.UnIni();
        }

        /// <summary>
        /// 开始排列
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pl_btn_Click(object sender, RoutedEventArgs e)
        {
            int stype=pl_type_com.SelectedIndex;
            string width = pl_width_text.Text.Trim();
            string height=pl_height_text.Text.Trim();
            int screen= screen_com.SelectedIndex;
            string licount= pl_wcount_text.Text.Trim();
            if (pl_custom_check.IsChecked==false)
            {
                width = "";
                height = "";
                licount = "";
            }
            await Task.Run(async () =>
            {
                await XChromeManager.ArrayChromes(stype, width, height, licount, screen);
                await XChromeManager.AdjustmentView();
            });
            

        }

        /// <summary>
        /// 开始群控
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kong_btn_Click(object sender, RoutedEventArgs e)
        {

            forms.InputBox i = new forms.InputBox(this);
            List<ComboBoxItem> list = new List<ComboBoxItem>();
            list.Add(new ComboBoxItem() { Content = "--关闭群控--",Tag=0});
            var runingxchrome = XChromeManager.GetRuningXchromes();
            foreach(var id in runingxchrome.Keys)
            {
                list.Add(new ComboBoxItem() { Content = "【" + id + "】" + runingxchrome[id].Name, Tag =id});
            }


            i.CreateSelect("选择主控", list,"确定","取消",200);
            i.ShowDialog();
            ComboBoxItem selete=i.select_value;
            if (selete == null)
            {
                MainWindow.Toast_Error("没有选中主控");return;
            }
            long idd = Convert.ToInt64(selete.Tag);
            if (idd == 0)
            {
                ControlManager.CloseControl();
                cs.MouseHookServer.UnIni();
                MainWindow.Toast_Success("关闭群控成功");
                return;
            }
            //启动控制器,这个最好放在ui线程
            cs.MouseHookServer.Ini();
            //开启群控
            ControlManager.StartControl(Convert.ToInt64(selete.Tag));
            MainWindow.Toast_Success("开启群控成功");


        }

        /// <summary>
        /// 启动相对位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsRatio_check_Click(object sender, RoutedEventArgs e)
        {
            //
            cs.ControlManager._isRatio=IsRatio_check.IsChecked??false;

        }

        /// <summary>
        /// 相对位置的说明
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsRatio_check_help_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string tip = "勾选后，在群控的时候，主窗口和子窗口大小不一样，也可以一样控制，但是会影响一定性能，建议按需勾选";
            System.Windows.MessageBox.Show(tip);
        }

        private async void script_do_btn_Click(object sender, RoutedEventArgs e)
        {
            //测试按钮
            //await XChromeManager.jober_AdjustmentView();
            //XChromeManager._is_jober_AdjustmentView = true;
            var keys=XChromeManager.GetRuningXchromes().Keys.ToArray();
            var xchrome = XChromeManager.GetRuningXchromes()[keys[0]];


            IntPtr hwd = (IntPtr)xchrome.Hwnd;

            //总窗口
            Win32Helper.GetWindowRect(hwd, out var ret);

            var legacywindow_hwd = Win32Helper.FindWindowEx(hwd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window");
            if (legacywindow_hwd == IntPtr.Zero)
            {
                return;
            }
            Win32Helper.RECT clientRect = new Win32Helper.RECT();
            Win32Helper.GetWindowRect(legacywindow_hwd, out clientRect);
            int ww = clientRect.Right - clientRect.Left;
            int hh = clientRect.Bottom - clientRect.Top;




            await xchrome.BrowserContext.Pages[0].SetViewportSizeAsync(ww,hh);



            Win32Helper.ChangeWindowPos(hwd, ret.Left,ret.Top,ret.Right-ret.Left,ret.Bottom-ret.Top);
        }
    }
    public partial class TableItem: ObservableObject
    {
        [ObservableProperty]
        private bool check;

        [ObservableProperty]
        private long id;

        [ObservableProperty]
        private string group;
        [ObservableProperty]
        private string name;
        [ObservableProperty]
        private string ip;
        [ObservableProperty]
        private DateTime doDate;
        [ObservableProperty]
        private DateTime createDate;
        [ObservableProperty]
        private string[] tags;
        [ObservableProperty]
        private string remark;
        [ObservableProperty]
        private bool isRunning = false;
        [ObservableProperty]
        private bool isCheckEnble = true; //是否可以选中

    }
    
    public partial class ButtonStatus : ObservableObject
    {
        [ObservableProperty]
        private bool edit=true;
        [ObservableProperty]
        private bool copy = true;
        [ObservableProperty]
        private bool del = false;
        [ObservableProperty]
        private bool run = false;
        [ObservableProperty]
        private bool stop = false;
        [ObservableProperty]
        private bool array = false;
        [ObservableProperty]
        private bool control = false;
        [ObservableProperty]
        private bool runScript = false;

        public void DisableAll()
        {
            this.Edit = false;
            this.Copy = false;
            this.Del = false;
            this.Run = false;
            this.Stop = false;
            this.Array = false;
            this.Control = false;
            this.RunScript = false;
        }
    }
}
