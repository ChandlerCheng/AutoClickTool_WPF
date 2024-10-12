using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace AutoClickTool_WPF
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            SystemSetting.RegisterHotKey(hwnd, SystemSetting.HOTKEY_SCRIPT_EN, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F9));
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);
        }

        #region 熱鍵觸發事件
        private const int WM_HOTKEY = 0x0312;
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                // 檢查熱鍵 ID 是否符合
                if (wParam.ToInt32() == SystemSetting.HOTKEY_SCRIPT_EN)
                {
                    // F11 熱鍵觸發，執行相應的動作
                    HotKeyAction.HotKeyAction_Script_EnableSwitch();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
        #endregion
        #region 視窗關閉
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            SystemSetting.UnregisterHotKey(hwnd, SystemSetting.HOTKEY_SCRIPT_EN);
        }
        #endregion
    }
    public class HotKeyAction
    {
        public static void HotKeyAction_Script_EnableSwitch()
        {

        }
    }

    public class GameFunction
    {
        public static bool NormalCheck(bool IsDebug)
        {

            int x_LU, y_LU;
            int xOffset_LU = Coordinate.windowBoxLineOffset + 129;
            int yOffset_LU = Coordinate.windowHOffset;
            x_LU = Coordinate.windowTop[0] + xOffset_LU;
            y_LU = Coordinate.windowTop[1] + yOffset_LU;

            Bitmap levelUpBMP = Properties.Resources.levelup;

            // 從畫面上擷取指定區域的圖像
            Bitmap screenshot_LevelUp = BitmapFunction.CaptureScreen(x_LU, y_LU, 50, 43);

            // 比對圖像
            double Final_LU = BitmapFunction.CompareImages(screenshot_LevelUp, levelUpBMP);

            if (IsDebug == true)
            {
                MessageBox.Show($"玩家檢測值為 '{Final_LU}')\n");
            }

            if (Final_LU > 50)
                return true;
            else
                return false;
        }
        public static bool BattleCheck_Player(bool IsDebug)
        {
            SystemSetting.GetGameWindow();
            int x_key, y_key;
            int xOffset_key = Coordinate.windowBoxLineOffset + 766;
            int yOffset_key = Coordinate.windowHOffset + 98;

            x_key = Coordinate.windowTop[0] + xOffset_key;
            y_key = Coordinate.windowTop[1] + yOffset_key;

            Bitmap fight_keybarBMP = Properties.Resources.fighting_keybar_player;
            // 從畫面上擷取指定區域的圖像
            Bitmap screenshot_keyBar = BitmapFunction.CaptureScreen(x_key, y_key, 33, 34);

            // 比對圖像
            double Final_KeyBar = BitmapFunction.CompareImages(screenshot_keyBar, fight_keybarBMP);

            if (IsDebug == true)
            {
                MessageBox.Show($"玩家檢測值為 '{Final_KeyBar}')\n");
            }

            if (Final_KeyBar > 80)
                return true;
            else
                return false;
        }
    }
    public class BitmapFunction
    {
        public static double CalculateColorRatio(Bitmap bitmap, System.Drawing.Color targetColor)
        {
            // 獲取圖像的寬度和高度
            int width = bitmap.Width;
            int height = bitmap.Height;

            // 初始化目標顏色像素數量
            int targetColorCount = 0;

            // 遍歷圖像的每個像素，計算目標顏色的像素數量
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 取得當前像素的顏色
                    System.Drawing.Color pixelColor = bitmap.GetPixel(x, y);

                    // 檢查像素是否與目標顏色匹配
                    if (pixelColor.R == targetColor.R && pixelColor.G == targetColor.G && pixelColor.B == targetColor.B)
                    {
                        targetColorCount++;
                    }
                }
            }

            // 計算目標顏色的佔據比例
            double targetColorRatio = (double)targetColorCount / (width * height);

            return targetColorRatio;
        }
        public static Bitmap CaptureScreen(int x, int y, int width, int height)
        {
            try
            {
                // 建立 Bitmap，格式為 24bppRgb
                Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // 使用 Graphics 擷取指定範圍的螢幕畫面
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
                }

                return bitmap;  // 返回擷取的 Bitmap
            }
            catch (Exception ex)
            {
                MessageBox.Show($"擷取螢幕失敗：{ex.Message}");
                return null;  // 如果發生錯誤，返回 null
            }
        }
        public static double CompareImages(Bitmap image1, Bitmap image2)
        {
            if (image1.Width != image2.Width || image1.Height != image2.Height)
                throw new ArgumentException("圖像大小不一致");

            int width = image1.Width;
            int height = image1.Height;

            // 計算相似度
            double difference = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    System.Drawing.Color pixel1 = image1.GetPixel(x, y);
                    System.Drawing.Color pixel2 = image2.GetPixel(x, y);

                    // 比較 RGB 差異，允許一定範圍內的容差
                    if (Math.Abs(pixel1.R - pixel2.R) > 10 || Math.Abs(pixel1.G - pixel2.G) > 10 || Math.Abs(pixel1.B - pixel2.B) > 10)
                    {
                        difference++;
                    }
                }
            }

            double totalPixels = width * height;
            double percentageSimilarity = (totalPixels - difference) / totalPixels * 100;

            return percentageSimilarity;
        }
    }
    public class Coordinate
    {
        // 視窗資訊
        public static int[] windowTop = new int[2];
        public static int[] windowBottom = new int[2];
        public static int windowHeigh = 0;
        public static int windowWidth = 0;
        public static int windowHOffset = 30;
        public static int windowBoxLineOffset = 8;
        public static bool IsGetWindows = false;
        /*
                敵方座標(目視)
                67890
                12345
                
                陣列值
                56789
                01234
         */
        public static int[,] Enemy = new int[10, 2];
        public static int[,] checkEnemy = new int[10, 2];
        /*
               我方座標(目視)
               12345
               67890

               陣列值
               01234
               56789
        */
        public static int[,] Friends = new int[10, 2];
        public static void CalculateAllEnemy(int x, int y)
        {
            // 計算第三號敵人的座標 (陣列索引為2)
            Enemy[2, 0] = x;
            Enemy[2, 1] = y;

            // 計算其他敵人的座標
            CalculateTargetCoordinate(Enemy, 2, 1, -68, 56);
            CalculateTargetCoordinate(Enemy, 1, 0, -68, 56);
            CalculateTargetCoordinate(Enemy, 2, 3, 68, -56);
            CalculateTargetCoordinate(Enemy, 3, 4, 68, -56);

            CalculateTargetCoordinate(Enemy, 0, 5, -73, -61);
            CalculateTargetCoordinate(Enemy, 1, 6, -73, -61);
            CalculateTargetCoordinate(Enemy, 2, 7, -73, -61);
            CalculateTargetCoordinate(Enemy, 3, 8, -73, -61);
            CalculateTargetCoordinate(Enemy, 4, 9, -73, -61);
        }
        public static void CalculateAllFriends(int x, int y)
        {
            // 計算第三號敵人的座標 (陣列索引為2)
            Friends[2, 0] = x;
            Friends[2, 1] = y;
            // 計算其他的座標
            CalculateTargetCoordinate(Friends, 2, 1, -68, 56);
            CalculateTargetCoordinate(Friends, 1, 0, -68, 56);
            CalculateTargetCoordinate(Friends, 2, 3, 68, -56);
            CalculateTargetCoordinate(Friends, 3, 4, 68, -56);

            CalculateTargetCoordinate(Friends, 0, 5, 73, 61);
            CalculateTargetCoordinate(Friends, 1, 6, 73, 61);
            CalculateTargetCoordinate(Friends, 2, 7, 73, 61);
            CalculateTargetCoordinate(Friends, 3, 8, 73, 61);
            CalculateTargetCoordinate(Friends, 4, 9, 73, 61);
        }
        public static void CalculateEnemyCheckXY()
        {
            /*
             * 抓取是否有怪物的白色圓環 , 參考最大體型生物為威奇迷宮
             *  20240424 - > 實際上不可行 , 上半截怪物體型大會被遮住(且怪物有循環動作)
             *  下半截則在1號位會有文字遮住怪物圓環的狀況
             */
            checkEnemy[0, 0] = 100;
            checkEnemy[0, 1] = 399;

            CalculateTargetCoordinate(checkEnemy, 0, 1, 72, -54);
            CalculateTargetCoordinate(checkEnemy, 1, 2, 72, -54);
            CalculateTargetCoordinate(checkEnemy, 2, 3, 72, -54);
            CalculateTargetCoordinate(checkEnemy, 3, 4, 72, -54);

            CalculateTargetCoordinate(checkEnemy, 0, 5, -64, -60);
            CalculateTargetCoordinate(checkEnemy, 1, 6, -64, -60);
            CalculateTargetCoordinate(checkEnemy, 2, 7, -64, -60);
            CalculateTargetCoordinate(checkEnemy, 3, 8, -64, -60);
            CalculateTargetCoordinate(checkEnemy, 4, 9, -64, -60);
        }
        // 計算目標的座標
        private static void CalculateTargetCoordinate(int[,] enemyArray, int fromIndex, int toIndex, int xOffset, int yOffset)
        {
            // 获取源坐标的值
            int fromX = enemyArray[fromIndex, 0];
            int fromY = enemyArray[fromIndex, 1];

            // 计算目标坐标并赋值给目标索引
            enemyArray[toIndex, 0] = fromX + xOffset;
            enemyArray[toIndex, 1] = fromY + yOffset;
        }
    }

    public class SystemSetting
    {
        // 引入 RegisterHotKey API
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // 引入 UnregisterHotKey API
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 定義熱鍵 ID，確保唯一
        public const int HOTKEY_SCRIPT_EN = 9000;

        // 匯入 FindWindow
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // 匯入 GetWindowRect
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // 定義 RECT 結構體，對應於 Windows API 的 RECT
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static bool GetWindowCoordinates(string windowTitle)
        {
            IntPtr windowHandle = FindWindow(null, windowTitle);
            if (windowHandle != IntPtr.Zero)
            {
                if (GetWindowRect(windowHandle, out RECT GameWindowsInfor))
                {
                    /*
                        遊戲本體800x600
                        視窗總長816x638
                        視窗邊框約8
                     */
                    Coordinate.windowTop[0] = GameWindowsInfor.Left;
                    Coordinate.windowTop[1] = GameWindowsInfor.Top;
                    Coordinate.windowBottom[0] = GameWindowsInfor.Right;
                    Coordinate.windowBottom[1] = GameWindowsInfor.Bottom;
                    Coordinate.windowHeigh = GameWindowsInfor.Bottom - GameWindowsInfor.Top;
                    Coordinate.windowWidth = GameWindowsInfor.Right - GameWindowsInfor.Left;
                    //MessageBox.Show($"視窗 '{windowTitle}' 的左邊頂點座標為：({GameWindowsInfor.Left}, {GameWindowsInfor.Top})\n" +
                    //$"右邊底部座標為：({GameWindowsInfor.Right}, {GameWindowsInfor.Bottom})\n" +
                    //$"視窗長寬：({GameWindowsInfor.Right - GameWindowsInfor.Left}, {GameWindowsInfor.Bottom - GameWindowsInfor.Top})\n");
                    return true;
                }
                else
                {
                    //MessageBox.Show($"無法獲取視窗 '{windowTitle}' 的座標");
                    return false;
                }
            }
            else
            {
                //MessageBox.Show($"找不到名稱為 '{windowTitle}' 的視窗");
                return false;
            }
        }

        public static void GetGameWindow()
        {
            // 抓取視窗位置 & 視窗長寬數值
            if (!GetWindowCoordinates("FairyLand"))
            {
                Coordinate.IsGetWindows = false;
                return;
            }
            Coordinate.IsGetWindows = true;

            /* 
              中心怪物位置會位於 800x600中的 275,290的位置
              遊戲本體800x600
              視窗總長 Windows 7 = 816x638  , Windows 10 = 816x639
              視窗邊框約8
              視窗標題列約30

               友軍前排3號位529,387
               敵軍前排3號位275,290
            */
            int x_enemy3, y_enemy3;
            int xOffset_enemy3 = Coordinate.windowBoxLineOffset + 275;
            int yOffset_enemy3 = Coordinate.windowHOffset + 290;

            x_enemy3 = Coordinate.windowTop[0] + xOffset_enemy3;
            y_enemy3 = Coordinate.windowTop[1] + yOffset_enemy3;

            Coordinate.CalculateAllEnemy(x_enemy3, y_enemy3);

            int x_friends3, y_friends3;
            int xOffset_friends3 = Coordinate.windowBoxLineOffset + 529;
            int yOffset_friends3 = Coordinate.windowHOffset + 387;

            x_friends3 = Coordinate.windowTop[0] + xOffset_friends3;
            y_friends3 = Coordinate.windowTop[1] + yOffset_friends3;

            Coordinate.CalculateAllFriends(x_friends3, y_friends3);
        }
    }
}
