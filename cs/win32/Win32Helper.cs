#region copyright
/**
// --------------------------------------------------------------------------------
// 文件名：Win32Helper.cs
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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XChrome.cs.win32
{
    public class Win32Helper
    {
        // 定义 RECT 结构，与 Win32 RECT 结构保持一致
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;   // 左边缘坐标 
            public int Top;    // 顶边缘坐标
            public int Right;  // 右边缘坐标
            public int Bottom; // 底边缘坐标
        }

        /// <summary>
        /// 找窗口
        /// </summary>
        /// <param name="lpClassName"></param>
        /// <param name="lpWindowName"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // 在指定父窗口下查找子窗口句柄
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        // 相关标志
        const uint SWP_NOZORDER = 0x0004;  // 不改变 Z 顺序
        const uint SWP_NOACTIVATE = 0x0010;  // 不激活窗口

        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;

        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;

        // 定义 WM_MOUSEWHEEL 消息（鼠标滚轮消息）代码
        private const int WM_MOUSEWHEEL = 0x020A;
        // WHEEL_DELTA 表示滚轮单步标准值
        private const int WHEEL_DELTA = 120;

        // 获取当前鼠标在屏幕上的坐标
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        // 发送消息给指定窗口（不要求窗口处于激活或聚焦状态）
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd,uint Msg, IntPtr wParam,IntPtr lParam);

        // 定义 POINT 结构，用于获取坐标
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }


        // 导入 user32.dll 中的 SetWindowPos 方法，调整窗口大小和位置
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );

        // 导入 user32.dll 中的 GetForegroundWindow 方法，获取当前前台窗口句柄
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        // 导入 GetWindowRect，用于获取整个窗口的矩形
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
        /// <summary>
        /// 改变窗口位置大小
        /// </summary>
        /// <param name="intptr"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public static void ChangeWindowPos(IntPtr intptr, int x,int y ,int w,int h)
        {
            SetWindowPos(intptr, IntPtr.Zero, x, y, w, h, SWP_NOZORDER | SWP_NOACTIVATE);
        }

    }
}
