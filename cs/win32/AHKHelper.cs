#region copyright
/**
// --------------------------------------------------------------------------------
// 文件名：AHKHelper.cs
// 作者：刹那 https://x.com/chanawudi
// 公司：https://x.com/chanawudi
// 更新日期：2025，2，27，13:55
// 版权所有 © Your Company. 保留所有权利。
// --------------------------------------------------------------------------------
*/
#endregion
using AutoHotkey.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XChrome.cs.win32
{
    public class AHKHelper
    {
        /// <summary>
        /// 获得窗口标识
        /// </summary>
        /// <param name="ahk_pid">进程pid</param>
        /// <param name="ahk_exe">exe名字</param>
        /// <param name="ahk_class">窗口class类</param>
        /// <param name="ahk_hwndId">句柄ID</param>
        /// <returns></returns>
        public static string GetWindowStr(string ahk_hwndId = "",string ahk_pid = "", string ahk_exe = "", string ahk_class = "")
        {
            string s = "";
            if (ahk_pid != "")
            {
                s += " ahk_pid " + ahk_pid;
            }
            if (ahk_exe != "")
            {
                s += " ahk_exe " + ahk_exe;
            }
            if (ahk_class != "")
            {
                s += " ahk_class " + ahk_class;
            }
            if (ahk_hwndId != "")
            {
                s += " ahk_id " + ahk_hwndId;
            }
            return s;
        }


        public static void DoScript(string str)
        {


            //            //hook通达信的滚轮
            //            string str = @"#IfWinActive, ahk_class TdxW_MainFrame_Class ahk_exe TdxW.exe
            //Wheelup:: send,{Up} ;定义【滚轮向前】分时放大
            //WheelDown:: send,{Down} ;定义【滚轮向后】分时缩小
            //MButton::send,^0";
            //            var ahk = AutoHotkeyEngine.Instance; 
            //            ahk.ExecRaw(str);


            var ahk = AutoHotkeyEngine.Instance;
            ahk.ExecRaw(str);
        }

        /// <summary>
        /// 点击窗口位置
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="ctype"></param>
        /// <param name="WindowStr">窗口字符串，用 AHKScriptHelper.GetWindowStr 获得</param>
        /// <param name="clickCount"></param>
        public static void ClickWindow_ByXY(int x,int y, ClickType ctype, string WindowStr, int clickCount = 1)
        {
            string s = "ControlClick ,X"+x+" Y"+y+", "+ WindowStr + ", , "+ ctype .ToString()+ ", "+ clickCount + ", , , ";
            DoScript(s);
        }

        /// <summary>
        /// 点击窗口中某个控件
        /// </summary>
        /// <param name="controlClassName">控件的类</param>
        /// <param name="ctype"></param>
        /// <param name="WindowStr"></param>
        /// <param name="clickCount"></param>
        public static void ClickWindow_ByClassName(string ControlClassNameOrText, ClickType ctype, string WindowStr, int clickCount = 1)
        {
            string s = "ControlClick ,"+ ControlClassNameOrText + ", " + WindowStr + ", , " + ctype.ToString() + ", " + clickCount + ", , , ";
            DoScript(s);
        }


        /// <summary>
        /// 设置空间焦点
        /// </summary>
        /// <param name="ControlClassNameOrText"></param>
        /// <param name="WindowStr"></param>
        public static void SetControlFocus(string ControlClassNameOrText, string WindowStr)
        {
            string s = "ControlFocus , "+ ControlClassNameOrText + ", "+ WindowStr + ", , , ";
            DoScript(s);
        }

        /// <summary>
        /// 获取控件的文本
        /// </summary>
        /// <param name="ControlClassNameOrText"></param>
        /// <param name="WindowStr"></param>
        /// <returns></returns>
        public static string GetControlText(string ControlClassNameOrText, string WindowStr)
        {
            string s = "ControlGetText ,outtext ," + ControlClassNameOrText + ", " + WindowStr;

            var ahk = AutoHotkeyEngine.Instance;
            ahk.ExecRaw(s);
            return ahk.GetVar("outtext");
        }

        /// <summary>
        /// 设置控件的文本
        /// </summary>
        /// <param name="ControlClassNameOrText"></param>
        /// <param name="text"></param>
        /// <param name="WindowStr"></param>
        public static void SetControlText(string ControlClassNameOrText,string text, string WindowStr)
        {
            string s = "ControlSetText , "+ ControlClassNameOrText + ", "+ text + ","+WindowStr;
            DoScript(s);
        }


        /// <summary>
        /// 发送按键或者文字，
        /// 参考：https://wyagd001.github.io/zh-cn/docs/commands/Send.htm
        /// </summary>
        /// <param name="ControlClassNameOrText"></param>
        /// <param name="KeyOrText"></param>
        /// <param name="WindowStr"></param>
        public static void SendKeyToControl(string ControlClassNameOrText, string KeyOrText, string WindowStr)
        {
            string s = "ControlSend, "+ ControlClassNameOrText + ", "+ KeyOrText + ","+ WindowStr;
            DoScript(s);
        }

        /// <summary>
        /// 获得控件的大小和位置
        /// </summary>
        /// <param name="ControlClassNameOrText"></param>
        /// <param name="WindowStr"></param>
        /// <returns></returns>
        public static Rect GetControlRect(string ControlClassNameOrText, string WindowStr)
        {
            string s = " ControlGetPos, x, y, w, h,"+ControlClassNameOrText+","+WindowStr;
            var ahk = AutoHotkeyEngine.Instance;
            ahk.ExecRaw(s);
            string x = ahk.GetVar("x");
            string y = ahk.GetVar("y");
            string w = ahk.GetVar("w");
            string h = ahk.GetVar("h");
            Rect r = new Rect() { 
                x=Convert.ToInt32(x),
                y=Convert.ToInt32(y),
                width=Convert.ToInt32(w),
                height=Convert.ToInt32(h)
            };
            return r;
        }

        /// <summary>
        /// 移动控件和改变大小
        /// </summary>
        /// <param name="ControlClassNameOrText"></param>
        /// <param name="r"></param>
        /// <param name="WindowStr"></param>
        public static void MoveControl(string ControlClassNameOrText, Rect r, string WindowStr)
        {
            string s = "ControlMove, "+ ControlClassNameOrText + ", "+r.x+", "+r.y+", "+r.width+", "+r.height+" , "+WindowStr;
            DoScript(s);
        }

        /// <summary>
        /// 获得列表的内容，每个 \n结尾
        /// </summary>
        /// <param name="ControlClassNameOrText"></param>
        /// <param name="WindowStr"></param>
        /// <returns></returns>
        public static string GetControlInfo_List(string ControlClassNameOrText,string WindowStr)
        {
            string s = "ControlGet, OutputVar, List , , "+ ControlClassNameOrText + ", "+ WindowStr;
            var ahk = AutoHotkeyEngine.Instance;
            ahk.ExecRaw(s);
            string tt = ahk.GetVar("OutputVar");
            return tt;
        }


        //https://wyagd001.github.io/zh-cn/docs/commands/ControlGet.htm  扩展 controlget 到时候看这里


        /// <summary>
        /// 选中列表值
        /// checkIndex 从1 开始
        /// </summary>
        /// <param name="ControlClassNameOrText"></param>
        /// <param name="checkIndex"></param>
        /// <param name="WindowStr"></param>
        public static void SetControlInfo_List(string ControlClassNameOrText,int checkIndex, string WindowStr)
        {
            string s = "Control, Choose, "+ checkIndex + " , "+ ControlClassNameOrText + ", "+WindowStr;
            DoScript(s);
        }



        public static void Test()
        {
            string str = "Control, Choose, 2 , ComboBox2, ahk_pid 73600 ahk_exe 微软语音合成助手1.3.exe";
            DoScript(str);
        }
    }

    public enum ClickType
    {
        LEFT= 0,
        RIGHT= 1,
        MIDDLE=2
    }

    public class Rect
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }
}
