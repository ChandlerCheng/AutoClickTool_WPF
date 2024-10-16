using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private static Task actionTask;
        private static bool isEnable = false;
        private static bool isWindowLoaded = false;
        private static int tabFunctionSelected = 0;
        private static CancellationTokenSource cancellationTokenSource;

        #region 取得系統資訊
        // 定義結構來存儲版本資訊
        [StructLayout(LayoutKind.Sequential)]
        public struct OSVERSIONINFOEX
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        // 使用 P/Invoke 調用 RtlGetVersion
        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);
        #endregion
        #region 程式初始化
        public MainWindow()
        {
            InitializeComponent();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            OSVERSIONINFOEX osVersion = new OSVERSIONINFOEX();
            RtlGetVersion(ref osVersion);

            string version = $"{osVersion.dwMajorVersion}.{osVersion.dwMinorVersion}.{osVersion.dwBuildNumber}";

            if (osVersion.dwMajorVersion == 10)
                SystemSetting.isWin10 = true;


            // 此為初始化怪物檢查點 , 務必在視窗打開時優先動作
            Coordinate.CalculateEnemyCheckXY();
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
#if !DEBUG 
            SystemSetting.RegisterHotKey(hwnd, SystemSetting.HOTKEY_SCRIPT_EN, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F11));
#else
            // DEBUG模式下 , 使用F11做熱鍵會干擾編譯時逐步執行
            SystemSetting.RegisterHotKey(hwnd, SystemSetting.HOTKEY_SCRIPT_EN, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F9));
#endif
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);
#if DEBUG
            labelBuildType.Content = "Debug";
#endif
            labelBuildType.Content = "Release";
            isWindowLoaded = true;
        }
        #endregion
        #region 遊戲中動作
        private static void BattleLoop()
        {
            switch (tabFunctionSelected)
            {
                case 2:
                    GameScript.AutoBattle();
                    break;
                case 3:
                    GameScript.AutoDefend();
                    break;
                case 4:
                    GameScript.AutoEnterBattle();
                    break;
                case 5:
                    GameScript.AutoBuff();
                    break;
                case 6:
                    break;
                default:
                    break;
            }
        }
        #endregion
        #region 執行緒動作
        private static async Task ActionLoop(CancellationToken token, MainWindow window)
        {
            while (!token.IsCancellationRequested)
            {
                if (isEnable)
                {
                    // 更新視窗的 Title
                    window.Dispatcher.Invoke(() =>
                    {
                        // 單純想讓Title可以隨語系改變好看用
                        window.Title = Application.Current.Resources["windowTitleFunction"].ToString() + " '" + tabFunctionSelected +
                        "'" + " " + Application.Current.Resources["windowTitleIs"].ToString() + " " +
                        Application.Current.Resources["windowTitleRuning"].ToString();

                    });
                    isEnable = false;
                }
                // 執行你的動作邏輯
                await Task.Delay(100);  // 模擬一些延遲，避免CPU過度佔用
                BattleLoop();
            }
            // 更新視窗的 Title
            window.Dispatcher.Invoke(() =>
            {
                window.Title = Application.Current.Resources["windowTitleSuspending"].ToString();
            });
            isEnable = false;
        }
        public static async void HotKeyAction_Script_EnableSwitch(MainWindow window)
        {

            SystemSetting.GetGameWindow();
#if !DEBUG
            if (Coordinate.IsGetWindows != true)
            {
                MessageBox.Show(Application.Current.Resources["msgDebugGameTitleIsError"].ToString());

                return;
            }
#endif
            // 檢查是否已經有一個執行中的任務
            if (actionTask != null && !actionTask.IsCompleted)
            {
                cancellationTokenSource.Cancel();  // 發送取消請求
                try
                {
                    await actionTask;  // 等待任務結束
                }
                catch (OperationCanceledException)
                {
                    // 任務被取消的情況
                }
                finally
                {
                    cancellationTokenSource.Dispose();  // 清理
                }
                return;
            }

            // 檢查是否需要重新啟動
            if (actionTask == null || actionTask.IsCompleted)
            {
                cancellationTokenSource = new CancellationTokenSource();
                actionTask = Task.Run(() => ActionLoop(cancellationTokenSource.Token, window));
                isEnable = true;
            }
        }
        #endregion
        #region 統整資訊
        public static void setGameScriptFlagDefault()
        {
            GameScript.isPetSupport = false;
            GameScript.isSummonerAttack = false;
        }
        public static void setPlayerActionKey(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0:
                    GameScript.playerActionKey = Key.F5;
                    break;
                case 1:
                    GameScript.playerActionKey = Key.F6;
                    break;
                case 2:
                    GameScript.playerActionKey = Key.F7;
                    break;
                case 3:
                    GameScript.playerActionKey = Key.F8;
                    break;
                case 4:
                    GameScript.playerActionKey = Key.F9;
                    break;
                case 5:
                    GameScript.playerActionKey = Key.F10;
                    break;
                case 6:
                    GameScript.playerActionKey = Key.F12;
                    break;
                default:
                    GameScript.playerActionKey = Key.F5;
                    break;
            }
        }
        public static void setSummonerActionKey(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0:
                    GameScript.summonerActionKey = Key.F5;
                    break;
                case 1:
                    GameScript.summonerActionKey = Key.F6;
                    break;
                case 2:
                    GameScript.summonerActionKey = Key.F7;
                    break;
                case 3:
                    GameScript.summonerActionKey = Key.F8;
                    break;
                case 4:
                    GameScript.summonerActionKey = Key.F9;
                    break;
                case 5:
                    GameScript.summonerActionKey = Key.F10;
                    break;
                case 6:
                    GameScript.summonerActionKey = Key.F12;
                    break;
                default:
                    GameScript.summonerActionKey = Key.F5;
                    break;
            }
        }
        public static void setSummonerAttackKey(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0:
                    GameScript.summonerAttackKey = Key.F5;
                    break;
                case 1:
                    GameScript.summonerAttackKey = Key.F6;
                    break;
                case 2:
                    GameScript.summonerAttackKey = Key.F7;
                    break;
                case 3:
                    GameScript.summonerAttackKey = Key.F8;
                    break;
                case 4:
                    GameScript.summonerAttackKey = Key.F9;
                    break;
                case 5:
                    GameScript.summonerAttackKey = Key.F10;
                    break;
                case 6:
                    GameScript.summonerAttackKey = Key.F12;
                    break;
                default:
                    GameScript.summonerAttackKey = Key.F6;
                    break;
            }
        }
        public static void setPetActionKey(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0:
                    GameScript.petActionKey = Key.F5;
                    break;
                case 1:
                    GameScript.petActionKey = Key.F6;
                    break;
                case 2:
                    GameScript.petActionKey = Key.F7;
                    break;
                case 3:
                    GameScript.petActionKey = Key.F8;
                    break;
                case 4:
                    GameScript.petActionKey = Key.F9;
                    break;
                case 5:
                    GameScript.petActionKey = Key.F10;
                    break;
                case 6:
                    GameScript.petActionKey = Key.F12;
                    break;
                default:
                    GameScript.petActionKey = Key.F5;
                    break;
            }
        }
        public static void setPetSupportTarget(int selectedIndex)
        {
            //tab5comboAutoBuffTarget.SelectedIndex;
            // 檢查選中的索引並執行對應邏輯
            switch (selectedIndex)
            {
                case 0:
                    GameScript.petSupTarget = 0;
                    break;
                case 1:
                    GameScript.petSupTarget = 1;
                    break;
                case 2:
                    GameScript.petSupTarget = 2;
                    break;
                case 3:
                    GameScript.petSupTarget = 3;
                    break;
                case 4:
                    GameScript.petSupTarget = 4;
                    break;
                case 5:
                    GameScript.petSupTarget = 0;
                    GameScript.isPetSupportToEnemy = true;
                    break;
                default:
                    GameScript.petSupTarget = 0;
                    break;
            }
        }
        public static void setAutoBuffTarget(int selectedIndex)
        {
            // 檢查選中的索引並執行對應邏輯
            switch (selectedIndex)
            {
                case 0:
                    GameScript.buffTarget = 0;
                    break;
                case 1:
                    GameScript.buffTarget = 1;
                    break;
                case 2:
                    GameScript.buffTarget = 2;
                    break;
                case 3:
                    GameScript.buffTarget = 3;
                    break;
                case 4:
                    GameScript.buffTarget = 4;
                    break;
                default:
                    GameScript.buffTarget = 0;
                    break;
            }
        }
        public void checkHotkeyGetInfo()
        {
#if DEBUG
            tab2LabelAutoAttackKeyDebug.Content = "";
            tab2LabelPetSupportKeyDebug.Content = "";
            tab3LabelPetSupportKeyDebug.Content = "";
            tab4LabelPetSupportKeyDebug.Content = "";
            tab4LabelSummonAttackKeyDebug.Content = "";
            tab4LabelSummonKeyDebug.Content = ""; ;
            tab5LabelAutoBuffKeyDebug.Content = ""; 
            tab5LabelPetSupportKeyDebug.Content = "";
#endif
            setGameScriptFlagDefault();
            switch (tabFunctionSelected)
            {
                case 2://tab 2 -AutoBattle
                    {
                        GameScript.isPetSupport = true;
                        // 設定自動攻擊熱鍵
                        if (tab2ComboPlayerActionKey.SelectedIndex != -1)
                        {
                            int select;
                            select = tab2ComboPlayerActionKey.SelectedIndex;
                            setPlayerActionKey(select);
#if DEBUG
                            tab2LabelAutoAttackKeyDebug.Content = DebugFunction.feedBackKeyString(GameScript.playerActionKey);
#endif
                        }
                        if (tab2CheckPetSupport.IsChecked == true)
                        {
                            // 設定寵物輔助目標
                            if (tab2comboPetSupportTarget.SelectedIndex != -1)
                            {
                                int select;
                                select = tab2comboPetSupportTarget.SelectedIndex;
                                setPetSupportTarget(select);
                            }
                            // 設定寵物輔助熱鍵
                            if (tab2ComboPetActionKey.SelectedIndex != -1)
                            {
                                int select;
                                select = tab2ComboPetActionKey.SelectedIndex;
                                setPetActionKey(select);
#if DEBUG
                                tab2LabelPetSupportKeyDebug.Content = DebugFunction.feedBackKeyString(GameScript.petActionKey);
#endif
                            }
                        }
                    }
                    break;
                case 3://AutoDefend
                    {
                        if (tab3CheckPetSupport.IsChecked == true)
                        {
                            GameScript.isPetSupport = true;
                            // 設定寵物輔助目標
                            if (tab3comboPetSupportTarget.SelectedIndex != -1)
                            {
                                int select;
                                select = tab3comboPetSupportTarget.SelectedIndex;
                                setPetSupportTarget(select);
                            }
                            // 設定寵物輔助熱鍵
                            if (tab3ComboPetActionKey.SelectedIndex != -1)
                            {
                                int select;
                                select = tab3ComboPetActionKey.SelectedIndex;
                                setPetActionKey(select);
#if DEBUG
                                tab3LabelPetSupportKeyDebug.Content = DebugFunction.feedBackKeyString(GameScript.petActionKey);
#endif
                            }
                        }
                    }
                    break;
                case 4://AutoEnterBattle
                    {
                        // 設定自動招怪熱鍵
                        if (tab4ComboSummonerActionKey.SelectedIndex != -1)
                        {
                            int select;
                            select = tab4ComboSummonerActionKey.SelectedIndex;
                            setSummonerActionKey(select);
#if DEBUG
                            tab4LabelSummonKeyDebug.Content = DebugFunction.feedBackKeyString(GameScript.summonerActionKey);
#endif
                        }
                        if (tab4CheckSummonAttack.IsChecked == true)
                        {
                            GameScript.isSummonerAttack = true;
                            int select;
                            select = tab4ComboSummonerAttackKey.SelectedIndex;
                            setSummonerAttackKey(select);
#if DEBUG
                            tab4LabelSummonAttackKeyDebug.Content = DebugFunction.feedBackKeyString(GameScript.summonerAttackKey);
#endif
                        }
                        if (tab4CheckPetSupport.IsChecked == true)
                        {
                            GameScript.isPetSupport = true;
                            // 設定寵物輔助目標
                            if (tab4comboPetSupportTarget.SelectedIndex != -1)
                            {
                                int select;
                                select = tab4comboPetSupportTarget.SelectedIndex;
                                setPetSupportTarget(select);
                            }
                            // 設定寵物輔助熱鍵
                            if (tab4ComboPetActionKey.SelectedIndex != -1)
                            {
                                int select;
                                select = tab4ComboPetActionKey.SelectedIndex;
                                setPetActionKey(select);
#if DEBUG
                                tab4LabelPetSupportKeyDebug.Content = DebugFunction.feedBackKeyString(GameScript.petActionKey);
#endif
                            }
                        }
                    }
                    break;
                case 5://AutoBuff
                    {
                        // 設定輔助目標
                        if (tab5comboAutoBuffTarget.SelectedIndex != -1)
                        {
                            int select;
                            select = tab5comboAutoBuffTarget.SelectedIndex;
                            setAutoBuffTarget(select);
                        }
                        // 設定輔助熱鍵
                        if (tab5ComboAutoBuffKey.SelectedIndex != -1)
                        {
                            int select;
                            select = tab5ComboAutoBuffKey.SelectedIndex;
                            setPlayerActionKey(select);
#if DEBUG
                            tab5LabelAutoBuffKeyDebug.Content = DebugFunction.feedBackKeyString(GameScript.playerActionKey);
#endif
                        }
                        if (tab5CheckPetSupport.IsChecked == true)
                        {
                            GameScript.isPetSupport = true;
                            // 設定寵物輔助目標
                            if (tab5comboPetSupportTarget.SelectedIndex != -1)
                            {
                                int select;
                                select = tab5comboPetSupportTarget.SelectedIndex;
                                setPetSupportTarget(select);
                            }
                            // 設定寵物輔助熱鍵
                            if (tab5ComboPetActionKey.SelectedIndex != -1)
                            {
                                int select;
                                select = tab5ComboPetActionKey.SelectedIndex;
                                setPetActionKey(select);
#if DEBUG
                                tab5LabelPetSupportKeyDebug.Content = DebugFunction.feedBackKeyString(GameScript.petActionKey);
#endif
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion
        #region 熱鍵觸發事件
        private const int WM_HOTKEY = 0x0312;
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                // 檢查熱鍵 ID 是否符合
                if (wParam.ToInt32() == SystemSetting.HOTKEY_SCRIPT_EN)
                {
                    // 檢查所有熱鍵.目標配置
                    checkHotkeyGetInfo();
                    // 執行相應的腳本
                    HotKeyAction_Script_EnableSwitch(this);
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
        #region 測試功能按鈕
        private void btnCurrentStatusCheck_Click(object sender, RoutedEventArgs e)
        {
            SystemSetting.GetGameWindow();
            if (GameFunction.BattleCheck_Player())
            {
                MessageBox.Show(Application.Current.Resources["msgDebugGameSatsut_Player"].ToString());
            }
            else if (GameFunction.BattleCheck_Pet())
            {
                MessageBox.Show(Application.Current.Resources["msgDebugGameSatsut_Pet"].ToString());
            }
            else if (GameFunction.NormalCheck())
            {
                MessageBox.Show(Application.Current.Resources["msgDebugGameSatsut_NotBattle"].ToString());
            }
            else
            {
                MessageBox.Show(Application.Current.Resources["msgDebugGameSatsut_Exception_Status"].ToString());
            }
        }

        private void btnGetEnemyIndex_Click(object sender, RoutedEventArgs e)
        {
            SystemSetting.GetGameWindow();
            int index = GameFunction.getEnemyCoor();
            if (index > 0)
            {
                MessageBox.Show(Application.Current.Resources["msgDebugGameGetEnemyIndex"].ToString() + index + Application.Current.Resources["msgDebugGameGetEnemyUnit"].ToString());
            }
            else
            {
                MessageBox.Show(Application.Current.Resources["msgDebugGameGetEnemyError"].ToString());
            }
        }

        private void btnGetEnemyIndexBmp_Click(object sender, RoutedEventArgs e)
        {
            SystemSetting.GetGameWindow();
            DebugFunction.captureAllEnemyDotScreen();
        }

        private void btnGetTargetBmp_Click(object sender, RoutedEventArgs e)
        {
            SystemSetting.GetGameWindow();
            if (int.TryParse(this.textGetTargetBmpX.Text, out int x) &&
                int.TryParse(this.textGetTargetBmpY.Text, out int y) &&
                int.TryParse(this.textGetTargetBmpWidth.Text, out int width) &&
                int.TryParse(this.textGetTargetBmpHeight.Text, out int height))
            {
                // 以下為基本需位移的部分 , 功能類需依照遊戲視窗做偏移
                x = x + Coordinate.windowBoxLineOffset + Coordinate.windowTop[0];
                y = y + Coordinate.windowHOffset + Coordinate.windowTop[1];

                DebugFunction.captureTargetScreen(x, y, width, height);
            }
        }
        #endregion
        #region 選擇TAB時設定使用的腳本
        private void tabControlUsingMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 檢查視窗是否已完全載入
            if (!isWindowLoaded)
                return;  // 如果視窗尚未載入完成，直接返回

            // 確認選中項是 TabItem
            if (e.Source is TabControl)
            {
                TabItem selectedTab = (sender as TabControl).SelectedItem as TabItem;
                if (selectedTab != null)
                {
                    // 根據選中的 Tab 執行相應的邏輯
                    switch (selectedTab.Name)
                    {
                        case "tabTestFunction":
                            tabFunctionSelected = 1;
                            break;
                        case "tabAutoBattle":
                            tabFunctionSelected = 2;
                            break;
                        case "tabAutoDefend":
                            tabFunctionSelected = 3;
                            break;
                        case "tabAutoEnterBattle":
                            tabFunctionSelected = 4;
                            break;
                        case "tabAutoBuff":
                            tabFunctionSelected = 5;
                            break;
                        case "tabAuxiliaryFunctions":
                            tabFunctionSelected = 6;
                            break;
                        default:
                            tabFunctionSelected = 1;
                            break;
                    }
                }
            }
        }
        #endregion
        #region 程式語系變更
        private void comboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 檢查選項是否被選中
            if (comboLanguage.SelectedItem != null)
            {
                // 根據 ComboBox 的選擇索引執行不同的動作
                int selectedIndex = comboLanguage.SelectedIndex;

                // 檢查選中的索引並執行對應邏輯
                switch (selectedIndex)
                {
                    case 0: // English
                        SystemSetting.LoadResourceDictionary("en-US.xaml");
                        break;
                    case 1: // 繁體中文
                        SystemSetting.LoadResourceDictionary("zh-TW.xaml");
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion
        #region 檢查輸入字元
        private void textGetTargetBmp_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            InputMethod.SetIsInputMethodEnabled((TextBox)sender, false);

            Regex regex = new Regex("[^0-9]+"); // 非數字的正則表達式
            e.Handled = regex.IsMatch(e.Text);  // 如果輸入非數字，則處理輸入事件為無效
        }
        #endregion
    }
    public class GameScript
    {
        private static int pollingEnemyIndex = 0;
        public static int petSupTarget = 0;
        public static int buffTarget = 0;
        public static bool isPetSupport = false;
        public static bool isPetSupportToEnemy = false;
        public static bool isSummonerAttack = false;
        public static Key playerActionKey = Key.F5;
        public static Key summonerActionKey = Key.F5;
        public static Key summonerAttackKey = Key.F6;
        public static Key petActionKey = Key.F5;
        public static void enemyPolling()
        {
            pollingEnemyIndex++;
            if (pollingEnemyIndex > 9)
                pollingEnemyIndex = 0;
        }
        public static void AutoBattle()
        {
            if (GameFunction.BattleCheck_Player() == true)
            {
                int i = GameFunction.getEnemyCoor();
                if (i > 0)
                {
                    GameFunction.castSpellOnTarget(Coordinate.Enemy[i - 1, 0], Coordinate.Enemy[i - 1, 1], playerActionKey, 10);
                    return;
                }
                GameFunction.castSpellOnTarget(Coordinate.Enemy[pollingEnemyIndex, 0], Coordinate.Enemy[pollingEnemyIndex, 1], playerActionKey, 10);
                enemyPolling();
            }
            else if (GameFunction.BattleCheck_Pet() == true)
            {
                if (isPetSupport)
                {
                    if (isPetSupportToEnemy)
                    {
                        int i = GameFunction.getEnemyCoor();
                        if (i > 0)
                        {
                            GameFunction.castSpellOnTarget(Coordinate.Enemy[i - 1, 0], Coordinate.Enemy[i - 1, 1], petActionKey, 10);
                            return;
                        }
                        GameFunction.castSpellOnTarget(Coordinate.Enemy[pollingEnemyIndex, 0], Coordinate.Enemy[pollingEnemyIndex, 1], petActionKey, 10);
                        enemyPolling();
                    }
                    else
                        GameFunction.castSpellOnTarget(Coordinate.Friends[petSupTarget, 0], Coordinate.Friends[petSupTarget, 1], petActionKey, 10);
                }
                else
                {
                    GameFunction.pressDefendButton();
                }
            }
            else
            {
            }
        }
        public static void AutoDefend()
        {
            if (GameFunction.BattleCheck_Player() == true)
            {
                GameFunction.pressDefendButton();
            }
            else if (GameFunction.BattleCheck_Pet() == true)
            {
                if (isPetSupport)
                {
                    if (isPetSupportToEnemy)
                    {
                        int i = GameFunction.getEnemyCoor();
                        if (i > 0)
                        {
                            GameFunction.castSpellOnTarget(Coordinate.Enemy[i - 1, 0], Coordinate.Enemy[i - 1, 1], petActionKey, 10);
                            return;
                        }
                        GameFunction.castSpellOnTarget(Coordinate.Enemy[pollingEnemyIndex, 0], Coordinate.Enemy[pollingEnemyIndex, 1], petActionKey, 10);
                        enemyPolling();
                    }
                    else
                        GameFunction.castSpellOnTarget(Coordinate.Friends[petSupTarget, 0], Coordinate.Friends[petSupTarget, 1], petActionKey, 10);
                }
                else
                    GameFunction.pressDefendButton();
            }
            else
            {
            }
        }
        public static void AutoEnterBattle()
        {
            if (GameFunction.BattleCheck_Player() == true)
            {
                if (isSummonerAttack)
                {
                    int i = GameFunction.getEnemyCoor();
                    if (i > 0)
                    {
                        GameFunction.castSpellOnTarget(Coordinate.Enemy[i - 1, 0], Coordinate.Enemy[i - 1, 1], summonerAttackKey, 10);
                        return;
                    }
                    GameFunction.castSpellOnTarget(Coordinate.Enemy[pollingEnemyIndex, 0], Coordinate.Enemy[pollingEnemyIndex, 1], summonerAttackKey, 10);
                    enemyPolling();
                }
                else
                    GameFunction.pressDefendButton();
            }
            else if (GameFunction.BattleCheck_Pet() == true)
            {
                if (isPetSupport)
                {
                    if (isPetSupportToEnemy)
                    {
                        int i = GameFunction.getEnemyCoor();
                        if (i > 0)
                        {
                            GameFunction.castSpellOnTarget(Coordinate.Enemy[i - 1, 0], Coordinate.Enemy[i - 1, 1], petActionKey, 10);
                            return;
                        }
                        GameFunction.castSpellOnTarget(Coordinate.Enemy[pollingEnemyIndex, 0], Coordinate.Enemy[pollingEnemyIndex, 1], petActionKey, 10);
                        enemyPolling();
                    }
                    else
                        GameFunction.castSpellOnTarget(Coordinate.Friends[petSupTarget, 0], Coordinate.Friends[petSupTarget, 1], petActionKey, 10);
                }
                else
                {
                    GameFunction.pressDefendButton();
                }
            }
            else
            {
                // 自動招怪
                if (GameFunction.NormalCheck() == true)
                {
                    KeyboardSimulator.KeyPress(summonerActionKey);
                    Thread.Sleep(50);
                }
            }
        }
        public static void AutoBuff()
        {
            if (GameFunction.BattleCheck_Player() == true)
            {
                GameFunction.castSpellOnTarget(Coordinate.Friends[buffTarget, 0], Coordinate.Friends[buffTarget, 1], playerActionKey, 10);
            }
            else if (GameFunction.BattleCheck_Pet() == true)
            {
                if (isPetSupport)
                {
                    if (isPetSupportToEnemy)
                    {
                        int i = GameFunction.getEnemyCoor();
                        if (i > 0)
                        {
                            GameFunction.castSpellOnTarget(Coordinate.Enemy[i - 1, 0], Coordinate.Enemy[i - 1, 1], petActionKey, 10);
                            return;
                        }
                        GameFunction.castSpellOnTarget(Coordinate.Enemy[pollingEnemyIndex, 0], Coordinate.Enemy[pollingEnemyIndex, 1], petActionKey, 10);
                        enemyPolling();
                    }
                    else
                        GameFunction.castSpellOnTarget(Coordinate.Friends[petSupTarget, 0], Coordinate.Friends[petSupTarget, 1], petActionKey, 10);
                }
                else
                {
                    GameFunction.pressDefendButton();
                }
            }
            else
            {
            }
        }
    }
    public class GameFunction
    {
        public static void castSpellOnTarget(int x, int y, Key keyCode, int delay)
        {
            /*
                滑鼠移動到指定座標後 , 按下指定熱鍵 , 點下左鍵
             */
            KeyboardSimulator.KeyPress(keyCode);
            Thread.Sleep(100);
            MouseSimulator.LeftMousePress(x, y);
            Thread.Sleep(delay);
        }
        public static bool NormalCheck()
        {

            int x_LU, y_LU;
            int xOffset_LU = Coordinate.windowBoxLineOffset + 129;
            int yOffset_LU = Coordinate.windowHOffset;
            x_LU = Coordinate.windowTop[0] + xOffset_LU;
            y_LU = Coordinate.windowTop[1] + yOffset_LU;
            Bitmap levelUpBMP;

            if (SystemSetting.isWin10 == true)
                levelUpBMP = Properties.Resources.win10_levelup;
            else
                levelUpBMP = Properties.Resources.win7_levelup;

            // 從畫面上擷取指定區域的圖像
            Bitmap screenshot_LevelUp = BitmapFunction.CaptureScreen(x_LU, y_LU, 50, 43);

            // 比對圖像
            double Final_LU = BitmapFunction.CompareImages(screenshot_LevelUp, levelUpBMP);

            if (DebugFunction.IsDebugMsg == true)
            {
                MessageBox.Show($"玩家檢測值為 '{Final_LU}')\n");
            }

            if (Final_LU > 50)
                return true;
            else
                return false;
        }
        public static bool BattleCheck_Player()
        {
            int x_key, y_key;
            int xOffset_key = Coordinate.windowBoxLineOffset + 766;
            int yOffset_key = Coordinate.windowHOffset + 98;
            Bitmap fight_keybarBMP;

            x_key = Coordinate.windowTop[0] + xOffset_key;
            y_key = Coordinate.windowTop[1] + yOffset_key;

            if (SystemSetting.isWin10 == true)
                fight_keybarBMP = Properties.Resources.win10_fighting_keybar_player;
            else
                fight_keybarBMP = Properties.Resources.win7_fighting_keybar_player;
            // 從畫面上擷取指定區域的圖像
            Bitmap screenshot_keyBar = BitmapFunction.CaptureScreen(x_key, y_key, 33, 34);

            // 比對圖像
            double Final_KeyBar = BitmapFunction.CompareImages(screenshot_keyBar, fight_keybarBMP);

            if (DebugFunction.IsDebugMsg == true)
            {
                MessageBox.Show($"玩家檢測值為 '{Final_KeyBar}')\n");
            }

            if (Final_KeyBar > 80)
                return true;
            else
                return false;
        }
        public static bool BattleCheck_Pet()
        {
            int x_key, y_key;
            int xOffset_key = Coordinate.windowBoxLineOffset + 766;
            int yOffset_key = Coordinate.windowHOffset + 98;
            Bitmap fight_keybarPetBMP;

            x_key = Coordinate.windowTop[0] + xOffset_key;
            y_key = Coordinate.windowTop[1] + yOffset_key;

            if (SystemSetting.isWin10 == true)
                fight_keybarPetBMP = Properties.Resources.win10_fighting_keybar_pet;
            else
                fight_keybarPetBMP = Properties.Resources.win7_fighting_keybar_pet;
            // 從畫面上擷取指定區域的圖像
            Bitmap screenshot_keyBarPet = BitmapFunction.CaptureScreen(x_key, y_key, 33, 34);

            // 比對圖像
            double Final_KeyBar = BitmapFunction.CompareImages(screenshot_keyBarPet, fight_keybarPetBMP);

            if (DebugFunction.IsDebugMsg == true)
            {
                MessageBox.Show($"寵物檢測值為 '{Final_KeyBar}')\n");
            }

            if (Final_KeyBar > 80)
                return true;
            else
                return false;
        }
        public static void pressDefendButton()
        {
            /*
                有小bug , 會變成點擊原先停點上的怪物 , 而不是指向防禦按鈕
                
                20240510 : 加入 Cursor.Position = new System.Drawing.Point(x, y); 才確保會移動到正確位置上。
             */
            int xOffset = Coordinate.windowBoxLineOffset + Coordinate.windowTop[0];
            int yOffset = Coordinate.windowHOffset + 1 + Coordinate.windowTop[1];
            int x, y;
            x = 779 + xOffset;
            y = 68 + yOffset;
            MouseSimulator.LeftMousePress(x, y);
            Thread.Sleep(50);
            MouseSimulator.MoveMouseTo(407, 260);
        }
        public static int getEnemyCoor()
        {
            int result = 0;
            if (BattleCheck_Player() == true || BattleCheck_Pet() == true)
            {
                int xOffset = Coordinate.windowBoxLineOffset + Coordinate.windowTop[0];
                int yOffset = Coordinate.windowHOffset + 1 + Coordinate.windowTop[1];

                Bitmap[] enemyGetBmp = new Bitmap[10];
                System.Drawing.Color EnemyExistColor;

                for (int i = 0; i < 10; i++)
                    enemyGetBmp[i] = BitmapFunction.CaptureScreen(Coordinate.checkEnemy[i, 0] + xOffset, Coordinate.checkEnemy[i, 1] + yOffset, 1, 1);

                if (SystemSetting.isWin10 == true)
                    EnemyExistColor = System.Drawing.Color.FromArgb(189, 190, 189);
                else
                    EnemyExistColor = System.Drawing.Color.FromArgb(255, 255, 255);

                for (int i = 0; i < 10; i++)
                {
                    double EnemyExistRatio = BitmapFunction.CalculateColorRatio(enemyGetBmp[i], EnemyExistColor);
                    if (EnemyExistRatio > 0)
                    {
                        result = i + 1;
                        return result;
                    }
                    if (DebugFunction.IsDebugMsg == true)
                        MessageBox.Show($"檢查第'{i}'位置時 , 比對值為'{EnemyExistRatio}'");
                }

                if (DebugFunction.IsDebugMsg == true)
                    MessageBox.Show($"無法取得怪物序列");

                return 0;
            }

            if (DebugFunction.IsDebugMsg == true)
                MessageBox.Show($"非戰鬥狀態");
            return 0;
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
    public class DebugFunction
    {
        public static bool IsDebugMsg = false;
        public static bool IsDebugDownloadImg = false;
        public static string feedBackKeyString(Key inputKey)
        {
            string result = string.Empty;
            switch (inputKey)
            {
                case Key.F5:
                    result = "F5";
                    break;
                case Key.F6:
                    result = "F6";
                    break;
                case Key.F7:
                    result = "F7";
                    break;
                case Key.F8:
                    result = "F8";
                    break;
                case Key.F9:
                    result = "F9";
                    break;
                case Key.F10:
                    result = "F10";
                    break;
                case Key.F12:
                    result = "F12";
                    break;
                default:
                    result = "";
                    break;
            }
            return result;
        }
        public static void captureAllEnemyDotScreen()
        {
            //if (GameFunction.BattleCheck_Player() == true || GameFunction.BattleCheck_Pet() == true)
            {
                int xOffset = Coordinate.windowBoxLineOffset + Coordinate.windowTop[0];
                int yOffset = Coordinate.windowHOffset + 1 + Coordinate.windowTop[1];

                Bitmap[] enemyGetBmp = new Bitmap[10];
                int x, y;

                for (int i = 0; i < 10; i++)
                {
                    enemyGetBmp[i] = BitmapFunction.CaptureScreen(Coordinate.checkEnemy[i, 0] + xOffset, Coordinate.checkEnemy[i, 1] + yOffset, 1, 1);
                    x = Coordinate.checkEnemy[i, 0] + xOffset;
                    y = Coordinate.checkEnemy[i, 1] + yOffset;
                    enemyGetBmp[i].Save("Enemy_" + i + "_" + "x" + x + "_" + "y" + y + "_" + ".bmp");
                }
            }
        }
        public static void captureTargetScreen(int in_x, int in_y, int width, int height)
        {
            int xOffset = Coordinate.windowBoxLineOffset + Coordinate.windowTop[0];
            int yOffset = Coordinate.windowHOffset + 1 + Coordinate.windowTop[1];
            int x, y;
            x = xOffset + in_x;
            y = yOffset + in_y;
            Bitmap GetBmp;
            GetBmp = BitmapFunction.CaptureScreen(x, y, width, height);
            GetBmp.Save("Bitmap_" + "_" + "x" + x + "_" + "y" + y + "_" + ".bmp");
        }
    }
    public class MouseSimulator
    {
        public struct POINT
        {
            public int X;
            public int Y;
        }
        // 匯入 GetCursorPos 函數
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        // 匯入User32.dll中的函數
        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        // 模擬滑鼠左鍵按下及釋放
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        // 模擬滑鼠右鍵按下及釋放
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        // WPF UI 執行緒安全的滑鼠左鍵按下方法
        public static void LeftMousePress(int x, int y)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 移動滑鼠到指定座標
                SetCursorPos(x, y);
                // 模擬滑鼠左鍵按下
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50); // 延遲一段時間
                mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, 0, 0, 0, UIntPtr.Zero);
            });
        }

        // WPF UI 執行緒安全的滑鼠右鍵按下方法
        public static void RightMousePress(int x, int y)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 移動滑鼠到指定座標
                SetCursorPos(x, y);

                // 模擬滑鼠右鍵按下
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50); // 延遲一段時間

                // 模擬滑鼠右鍵釋放
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            });
        }
        // WPF UI 執行緒安全的移動滑鼠位置
        public static void MoveMouseTo(int x, int y)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 移動滑鼠到指定座標
                SetCursorPos(x, y);
            });
        }
    }
    public class KeyboardSimulator
    {
        // 匯入 user32.dll 中的 keybd_event 函數
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        // 定義按鍵事件的標誌
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_KEYUP = 0x0002;

        // 模擬按下按鍵
        public static void KeyDown(Key key)
        {
            byte keyCode = (byte)KeyInterop.VirtualKeyFromKey(key);
            Application.Current.Dispatcher.Invoke(() =>
            {
                keybd_event(keyCode, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
            });
        }

        // 模擬釋放按鍵
        public static void KeyUp(Key key)
        {
            byte keyCode = (byte)KeyInterop.VirtualKeyFromKey(key);
            Application.Current.Dispatcher.Invoke(() =>
            {
                keybd_event(keyCode, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            });
        }

        // 模擬按下並釋放按鍵
        public static void KeyPress(Key key)
        {
            byte keyCode = (byte)KeyInterop.VirtualKeyFromKey(key);
            Application.Current.Dispatcher.Invoke(() =>
            {
                KeyDown(key);
                Thread.Sleep(50); // 延遲一段時間
                KeyUp(key);
            });
        }
    }
    public class SystemSetting
    {
        // Windows 版本判斷
        public static bool isWin10 = false;

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

        // 加載對應的語系資源字典
        public static void LoadResourceDictionary(string culture)
        {
            // 移除舊的資源字典
            ResourceDictionary oldDict = null;
            foreach (ResourceDictionary dict in Application.Current.Resources.MergedDictionaries)
            {
                if (dict.Source != null && (dict.Source.OriginalString.Contains("en-US.xaml") || dict.Source.OriginalString.Contains("zh-TW.xaml")))
                {
                    oldDict = dict;
                    break;
                }
            }

            if (oldDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);
            }

            // 加載新的資源字典
            var newDict = new ResourceDictionary();
            newDict.Source = new Uri($"/AutoClickTool_WPF;component/Language/{culture}", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries.Add(newDict);
        }
    }
}
