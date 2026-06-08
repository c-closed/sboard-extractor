using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace SboardExtractor
{
    static class NativeMethods
    {
        public const int KEYEVENTF_KEYUP = 0x0002;
        public const byte VK_TAB = 0x09;
        public const byte VK_RETURN = 0x0D;
        public const byte VK_CONTROL = 0x11;
        public const byte VK_V = 0x56;
        public const byte VK_MENU = 0x12;
        public const byte VK_DOWN = 0x28;
        public const byte VK_SHIFT = 0x10;
        public const int SW_RESTORE = 9;
        public const int SW_SHOW = 5;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLengthW(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowW(string lpClassName, string lpWindowName);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr ShellExecuteW(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);
        [DllImport("shell32.dll")]
        public static extern bool IsUserAnAdmin();
        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();
        [DllImport("user32.dll")]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        public const uint GW_HWNDNEXT = 2;
        public const uint GW_CHILD = 5;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);
        public const uint WM_SETTEXT = 0x000C;
        public const uint BM_CLICK = 0x00F5;
        public const uint CB_SETCURSEL = 0x014E;
        public const uint CB_GETCOUNT = 0x0146;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern short VkKeyScanW(char ch);

        public const uint WM_GETTEXT = 0x000D;
        public const uint WM_GETTEXTLENGTH = 0x000E;
        public const uint WM_SETFOCUS = 0x0007;

        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        [DllImport("user32.dll")]
        public static extern IntPtr GetMenu(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);
        [DllImport("user32.dll")]
        public static extern int GetMenuItemCount(IntPtr hMenu);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMenuItemInfo(IntPtr hMenu, int uItem, bool fByPosition,
            ref MENUITEMINFO lpmii);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetMenuString(IntPtr hMenu, int uItem, StringBuilder lpString,
            int nMaxCount, int uFlag);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetMenuItemID(IntPtr hMenu, int nPos);

        public const uint WM_COMMAND = 0x0111;
        public const uint WM_MENUCOMMAND = 0x01E6;

        public const int MF_BYPOSITION = 0x400;
        public const int MIIM_STRING = 0x40;
        public const int MIIM_ID = 0x2;
        public const int MIIM_SUBMENU = 0x4;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MENUITEMINFO
        {
            public int cbSize;
            public uint fMask;
            public uint fType;
            public uint fState;
            public uint wID;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public IntPtr dwItemData;
            public string dwTypeData;
            public int cch;
            public IntPtr hbmpItem;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }

    class UIElementInfo
    {
        public int Depth { get; set; }
        public string ClassName { get; set; }
        public string Text { get; set; }
        public string ControlType { get; set; }
        public bool Visible { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    class SboardExtractor
    {
        private static List<UIElementInfo> _elements = new List<UIElementInfo>();
        private static string _outputPath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            "sboard_data.csv");
        private const string LoginWindowTitle = "Sboard";
        private const string SessionPrefix = "Sboard [";
        private const string AppVersion = "1.0.0.0";
        private const string UpdateXmlUrl = "https://extractor-api.sboard-auto-login.workers.dev/api/update.xml";
        private const byte VK_UP = 0x26;
        private const byte VK_LEFT = 0x25;
        private const byte VK_RIGHT = 0x27;
        private const byte VK_SPACE = 0x20;
        private const byte VK_HOME = 0x24;
        private const byte VK_END = 0x23;

        [STAThread]
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "--console" || args[i] == "-c" || args[i] == "--discover-login" || args[i] == "--discover-menu" || args[i] == "--test-menu-id" || args[i] == "--test-all-menu" || args[i] == "--extract-details" || args[i] == "-e")
                { ConsoleMain(args); return; }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }

        static void ConsoleMain(string[] args)
        {
            Console.WriteLine("=== Sboard Data Extractor v" + AppVersion + " ===");
            Console.WriteLine();

            bool discoverMode = false;
            bool extractMode = false;
            bool discoverLogin = false;
            bool discoverMenu = false;
            bool debugXlsx = false;
            string xlsxPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a == "--discover" || a == "-d") discoverMode = true;
                if (a == "--extract-details" || a == "-e") extractMode = true;
                if (a == "--discover-login") discoverLogin = true;
                if (a == "--discover-menu") discoverMenu = true;
                if (a == "--xlsx" && i + 1 < args.Length) { xlsxPath = args[++i]; }
            }

            int testMenuId = -1;
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "--test-menu-id" && i + 1 < args.Length)
                { int.TryParse(args[++i], out testMenuId); }

            bool testAll = false;
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "--test-all-menu") testAll = true;

            if (discoverLogin) { DiscoverLoginWindow(); return; }
            if (discoverMenu) { DiscoverMenu(); return; }
            if (testMenuId > 0) { TestMenuId(testMenuId); return; }
            if (testAll) { TestAllMenuIds(); return; }

            if (!NativeMethods.IsUserAnAdmin())
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string argLine = args.Length > 0 ? string.Join(" ", args) : "";
                NativeMethods.ShellExecuteW(IntPtr.Zero, "runas", exePath, argLine, null, 1);
                Environment.Exit(0);
            }

            string userId = "220807";
            string password = "0906";
            string sboardExe = @"C:\Program Files (x86)\sprog\sboard.exe";

            string inpFile = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                "1팀수행사업.inp");

            try
            {
                Run(userId, password, sboardExe, inpFile, discoverMode, extractMode, xlsxPath, null);
            }
            catch (Exception runEx)
            {
                Console.WriteLine("Run() unhandled: " + runEx.GetType().Name);
                try { Console.WriteLine(runEx.ToString()); } catch { Console.WriteLine("(cannot print exception details)"); }
            }
        }

        static void Run(string userId, string password, string exePath, string inpFile,
            bool discoverMode, bool extractMode, string xlsxPath, Action<string> progress)
        {
            Action<string> Progress = msg => { if (progress != null) progress(msg); else Console.WriteLine(msg); };

            IntPtr sessionHwnd = IntPtr.Zero;

            try
            {

            if (extractMode)
            {
                sessionHwnd = FindWindowByTitle(SessionPrefix, false);
                if (sessionHwnd == IntPtr.Zero) sessionHwnd = FindWindowByTitle("Sboard", false);
                if (sessionHwnd == IntPtr.Zero) { Progress("세션 없음"); return; }
                ExtractContractDetails(sessionHwnd);
                return;
            }

            AdminCheck();

            Progress("Sboard 실행중...");
            var sessionWindows = FindAllWindowsByTitle(SessionPrefix);
            var loginWindows = FindAllWindowsByTitle(LoginWindowTitle);
            bool foundSession = sessionWindows.Exists(w => w.Item2.StartsWith(SessionPrefix));
            bool foundLogin = loginWindows.Exists(w => w.Item2.Trim() == LoginWindowTitle);
            if (foundSession || foundLogin) { KillSboard(); Thread.Sleep(1000); }

            LaunchSboard(exePath);
            IntPtr hwnd = WaitForWindow(LoginWindowTitle, 15);
            if (hwnd == IntPtr.Zero) { Progress("로그인 창 대기 실패"); return; }

            Progress("로그인중...");
            InputCredentials(hwnd, userId, password);

            Progress("세션 대기중...");
            sessionHwnd = WaitForSession(15);
            if (sessionHwnd == IntPtr.Zero) { Progress("세션 연결 실패"); return; }

            Progress("메뉴 진입중...");
            NativeMethods.PostMessageW(sessionHwnd, NativeMethods.WM_COMMAND, (IntPtr)14, IntPtr.Zero);
            Thread.Sleep(1500);

            if (discoverMode) { DiscoverControls(sessionHwnd); return; }
            if (extractMode) { ExtractContractDetails(sessionHwnd); return; }

            Progress("엑셀 파일 찾는중...");
            if (string.IsNullOrEmpty(xlsxPath))
            {
                string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
                Progress("  경로=" + exeDir);
                var xlsxFiles = Directory.GetFiles(exeDir, "*.xlsx");
                Progress("  " + xlsxFiles.Length + "개 파일 발견");
                Array.Sort(xlsxFiles, (a, b) => string.Compare(b, a, StringComparison.Ordinal));
                foreach (string f in xlsxFiles)
                {
                    string fn = Path.GetFileName(f);
                    if (!fn.StartsWith("~$")) { xlsxPath = f; break; }
                }
            }
            Progress("  대상=" + (xlsxPath ?? "(없음)"));

            Progress("입력 파일 확인중...");
            if (string.IsNullOrEmpty(xlsxPath) && !File.Exists(inpFile))
            { Progress("입력 파일 없음"); return; }

            var items = new List<SearchItem>();
            Progress("업무 목록 로딩중...");
            if (!string.IsNullOrEmpty(xlsxPath))
            {
                Progress("  xlsx 읽는중...");
                items = ReadXlsxBizList(xlsxPath);
                Progress("  " + items.Count + "개 로드완료");
            }
            else
            {
                string[] lines = File.ReadAllLines(inpFile, Encoding.UTF8);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("//")) continue;
                    string[] parts = trimmed.Split(new[] { ',' }, 2);
                    if (parts.Length < 2) continue;
                    int di;
                    if (int.TryParse(parts[0].Trim(), out di))
                        items.Add(new SearchItem { BizNum = parts[1].Trim(), DeptIndex = di });
                }
                Progress("  " + items.Count + "개 로드완료 (INP)");
            }

            Progress("총 " + items.Count + "건 처리 시작...");
            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                string tag = string.Format("{0:D2}/{1:D2} ", idx + 1, items.Count);
                DoSearch(sessionHwnd, item.DeptIndex, item.BizNum);
                Thread.Sleep(500);
                Progress(tag + "[" + item.BizNum + "] 추출중...");

                Dictionary<string, string> fields = null;
                try
                {
                    fields = ReadContractDetails(sessionHwnd);
                }
                catch (Exception ex)
                {
                    Progress(tag + "ReadContractDetails 오류: " + ex.GetType().Name + " - " + ex.Message);
                    fields = new Dictionary<string, string>();
                }
                if (fields != null && fields.Count > 0)
                {
                    if (!string.IsNullOrEmpty(xlsxPath))
                    {
                        try
                        {
                            WriteXlsxRow(xlsxPath, item.BizNum, fields);
                        }
                        catch (Exception ex)
                        {
                            Progress(tag + "WriteXlsxRow 오류: " + ex.GetType().Name + " - " + ex.Message);
                        }
                    }
                    string v; string projName = fields.TryGetValue("사업명", out v) ? v : "";
                    Progress(tag + "[" + item.BizNum + "] " + projName + " 추출완료");
                }
                else
                {
                    Progress(tag + "[" + item.BizNum + "] 데이터없음");
                }
                Thread.Sleep(500);
            }

            Progress("--- 모두 완료 ---");

            }
            catch (Exception ex)
            {
                Progress("치명 오류: " + ex.GetType().Name + " - " + ex.Message);
            }
        }

        class SearchItem
        {
            public string BizNum;
            public int DeptIndex;
        }

        static void AdminCheck()
        {
            if (!NativeMethods.IsUserAnAdmin())
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                NativeMethods.ShellExecuteW(IntPtr.Zero, "runas", exePath, "", null, 1);
                Environment.Exit(0);
            }
        }

        // ========== Update Check ==========

        static bool CheckForUpdate(out string latestVersion, out string downloadUrl)
        {
            latestVersion = null; downloadUrl = null;
            try
            {
                var client = new System.Net.WebClient();
                client.Encoding = Encoding.UTF8;
                string xml = client.DownloadString(UpdateXmlUrl);
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                var versionNode = doc.SelectSingleNode("/item/version");
                var urlNode = doc.SelectSingleNode("/item/url");
                if (versionNode == null || urlNode == null) return false;
                latestVersion = versionNode.InnerText.Trim();
                downloadUrl = urlNode.InnerText.Trim();
                var current = new Version(AppVersion);
                var latest = new Version(latestVersion);
                return latest > current;
            }
            catch { return false; }
        }

        static void SelfUpdate(string downloadUrl)
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = Path.GetDirectoryName(exePath);
            string zipPath = Path.Combine(Path.GetTempPath(), "SboardExtractor_Update.zip");
            string extractDir = Path.Combine(Path.GetTempPath(), "SboardExtractor_Update_" + Guid.NewGuid().ToString("N"));

            try
            {
                using (var client = new System.Net.WebClient())
                    client.DownloadFile(downloadUrl, zipPath);

                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

                string updaterPs1 = Path.Combine(Path.GetTempPath(), "SboardExtractor_Updater.ps1");
                string psContent = ""
                    + "$id = (Get-Process -Id " + Process.GetCurrentProcess().Id + " -ErrorAction SilentlyContinue).Id`r`n"
                    + "if ($id) { Wait-Process -Id $id -Timeout 30 -ErrorAction SilentlyContinue }`r`n"
                    + "Start-Sleep -Seconds 1`r`n"
                    + "Copy-Item -Path '" + extractDir.Replace("'", "''") + "\\*' -Destination '" + exeDir.Replace("'", "''") + "' -Recurse -Force`r`n"
                    + "Start-Process -FilePath '" + exePath.Replace("'", "''") + "'";
                File.WriteAllText(updaterPs1, psContent);

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + updaterPs1 + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch { }
        }

        // ========== Form Classes ==========

        static void SetIcon(Form form)
        {
            try
            {
                string iconPath = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                    "icon.ico");
                if (File.Exists(iconPath))
                    form.Icon = new Icon(iconPath);
            }
            catch { }
        }

        class LoginForm : Form
        {
            private TextBox txtId;
            private TextBox txtPw;
            private Button btnLogin;

            public LoginForm()
            {
                Text = "Sboard Data Extractor";
                Size = new Size(320, 210);
                FormBorderStyle = FormBorderStyle.FixedSingle;
                MaximizeBox = false;
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.White;
                SetIcon(this);

                var lblId = new Label
                {
                    Text = "ID", Location = new Point(30, 28), Size = new Size(30, 22),
                    Font = new Font("맑은 고딕", 9), ForeColor = Color.FromArgb(80, 80, 80), TextAlign = ContentAlignment.MiddleLeft
                };
                txtId = new TextBox
                {
                    Location = new Point(70, 26), Size = new Size(220, 25),
                    Font = new Font("맑은 고딕", 10), BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(245, 245, 245)
                };

                var lblPw = new Label
                {
                    Text = "PW", Location = new Point(30, 66), Size = new Size(30, 22),
                    Font = new Font("맑은 고딕", 9), ForeColor = Color.FromArgb(80, 80, 80), TextAlign = ContentAlignment.MiddleLeft
                };
                txtPw = new TextBox
                {
                    Location = new Point(70, 64), Size = new Size(220, 25),
                    Font = new Font("맑은 고딕", 10), UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(245, 245, 245)
                };

                btnLogin = new Button
                {
                    Text = "로 그 인", Location = new Point(30, 108), Size = new Size(260, 36),
                    FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 },
                    BackColor = Color.FromArgb(52, 120, 246), ForeColor = Color.White,
                    Font = new Font("맑은 고딕", 10, FontStyle.Bold), Cursor = Cursors.Hand
                };
                btnLogin.Click += BtnLogin_Click;
                btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(42, 100, 220);
                btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = Color.FromArgb(52, 120, 246);
                AcceptButton = btnLogin;

                Controls.Add(lblId); Controls.Add(txtId);
                Controls.Add(lblPw); Controls.Add(txtPw);
                Controls.Add(btnLogin);
            }

            void BtnLogin_Click(object sender, EventArgs e)
            {
                btnLogin.Enabled = false;
                btnLogin.Text = "접속중...";
                var progress = new ProgressForm(txtId.Text, txtPw.Text);
                progress.FormClosed += (s2, e2) => { Close(); };
                progress.Show();
                Hide();
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    string latestVersion, downloadUrl;
                    if (CheckForUpdate(out latestVersion, out downloadUrl))
                    {
                        BeginInvoke((Action)(delegate
                        {
                            var result = MessageBox.Show(
                                "새 버전 " + latestVersion + "이(가) 있습니다.\n\n지금 업데이트하시겠습니까?",
                                "업데이트 확인",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);
                            if (result == DialogResult.Yes)
                            {
                                SelfUpdate(downloadUrl);
                                Application.Exit();
                            }
                        }));
                    }
                });
            }
        }

        class ProgressForm : Form
        {
            private ListBox lstLog;
            private Label lblItem;
            private Thread workThread;

            public ProgressForm(string userId, string password)
            {
                Text = "데이터 추출 진행";
                Size = new Size(620, 420);
                FormBorderStyle = FormBorderStyle.FixedSingle;
                ControlBox = false;
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.White;
                SetIcon(this);

                lstLog = new ListBox
                {
                    Location = new Point(12, 12),
                    Size = new Size(580, 330),
                    Font = new Font("Consolas", 9),
                    HorizontalScrollbar = true,
                    SelectionMode = SelectionMode.None,
                    IntegralHeight = false,
                    BackColor = Color.FromArgb(250, 250, 250),
                    BorderStyle = BorderStyle.FixedSingle
                };

                lblItem = new Label
                {
                    Text = "준비중...",
                    Location = new Point(12, 355),
                    Size = new Size(580, 22),
                    Font = new Font("맑은 고딕", 9),
                    ForeColor = Color.FromArgb(80, 80, 80),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                Controls.Add(lstLog);
                Controls.Add(lblItem);

                Shown += (s, args) =>
                {
                    workThread = new Thread(() => DoWork(userId, password));
                    workThread.Start();
                };
            }

            void AddLog(string msg)
            {
                if (InvokeRequired)
                { try { Invoke((Action)(() => AddLog(msg))); } catch { } return; }
                lstLog.Items.Add("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg);
                lstLog.TopIndex = lstLog.Items.Count - 1;
            }

            void DoWork(string uid, string pw)
            {
                AddLog("작업 시작");
                try
                {
                    string sboardExe = @"C:\Program Files (x86)\sprog\sboard.exe";
                    string inpFile = Path.Combine(
                        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                        "1팀수행사업.inp");

                    Run(uid, pw, sboardExe, inpFile, false, false, null, msg =>
                    {
                        try { Invoke((Action)(() => UpdateUI(msg))); } catch { }
                    });
                }
                catch (Exception ex)
                {
                    AddLog("오류: " + ex.Message);
                }
                finally
                {
                    AddLog("작업 완료");
                }
            }

            void UpdateUI(string msg)
            {
                if (msg.StartsWith("---"))
                {
                    lblItem.Text = "";
                    AddLog(msg.Replace("---", "").Trim());
                }
                else
                {
                    AddLog(msg);
                    lblItem.Text = msg;
                }
            }
        }

        // ========== End Form Classes ==========

        static List<SearchItem> ReadXlsxBizList(string xlsxPath)
        {
            var result = new List<SearchItem>();
            string connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + xlsxPath + ";Extended Properties=\"Excel 12.0;HDR=YES;\"";
            try
            {
                using (var conn = new System.Data.OleDb.OleDbConnection(connStr))
                {
                    conn.Open();
                    var dt = conn.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, null);
                    var sheetNames = new List<string>();
                    foreach (System.Data.DataRow r in dt.Rows)
                        sheetNames.Add(r["TABLE_NAME"].ToString());

                    string sheetName = "";
                    int maxRows = 0;
                    foreach (string name in sheetNames)
                    {
                        if (!name.EndsWith("$")) continue;
                        try
                        {
                            using (var cntCmd = conn.CreateCommand())
                            {
                                cntCmd.CommandText = "SELECT COUNT(*) FROM [" + name + "]";
                                int cnt = (int)cntCmd.ExecuteScalar();
                                if (cnt > maxRows) { maxRows = cnt; sheetName = name; }
                            }
                        }
                        catch { }
                    }
                    if (string.IsNullOrEmpty(sheetName)) return result;

                    string sql = "SELECT * FROM [" + sheetName + "]";
                    using (var da = new System.Data.OleDb.OleDbDataAdapter(sql, conn))
                    {
                        var data = new System.Data.DataTable();
                        da.Fill(data);
                        foreach (System.Data.DataRow row in data.Rows)
                        {
                            string bizNum = "";
                            if (row[1] != null && row[1] != DBNull.Value)
                                bizNum = row[1].ToString().Trim();
                            if (string.IsNullOrEmpty(bizNum)) continue;
                            int dept = 0;
                            if (row[2] != null && row[2] != DBNull.Value)
                                int.TryParse(row[2].ToString().Trim(), out dept);
                            result.Add(new SearchItem { BizNum = bizNum, DeptIndex = dept });
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("  xlsx 읽기 오류: " + ex.Message); }
            return result;
        }

        static void WriteXlsxRow(string xlsxPath, string bizNum, Dictionary<string, string> fields)
        {
            dynamic excel = null;
            dynamic workbook = null;
            dynamic sheets = null;
            dynamic sheet = null;
            try
            {
                Type excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null) return;
                excel = Activator.CreateInstance(excelType);
                excel.Visible = false;
                excel.DisplayAlerts = false;
                workbook = excel.Workbooks.Open(xlsxPath);
                sheets = workbook.Sheets;
                int maxCount = 0;
                try
                {
                    foreach (dynamic s in sheets)
                    {
                        int cnt = s.UsedRange.Rows.Count;
                        if (cnt > maxCount) { maxCount = cnt; sheet = s; }
                    }
                }
                catch (Exception) { }
                if (sheet == null) { workbook.Close(false); return; }

                int foundRow = 0;
                int lastRow = sheet.UsedRange.Rows.Count;
                for (int r = 2; r <= lastRow; r++)
                {
                    object val = sheet.Cells[r, 2].Value2;
                    if (val != null && val.ToString().Trim() == bizNum)
                    { foundRow = r; break; }
                }

                if (foundRow > 0)
                {
                    string v;
                    if (fields.TryGetValue("사업명", out v))
                    { try { sheet.Cells[foundRow, 4] = v; } catch (Exception) { } }
                    if (fields.TryGetValue("공급가액", out v))
                    { try { sheet.Cells[foundRow, 5] = ParseCommaNumber(v); } catch (Exception) { } }
                    if (fields.TryGetValue("총수금액", out v))
                    { try { sheet.Cells[foundRow, 6] = ParseCommaNumber(v); } catch (Exception) { } }
                    if (fields.TryGetValue("잔액", out v))
                    { try { sheet.Cells[foundRow, 7] = ParseCommaNumber(v); } catch (Exception) { } }
                    if (fields.TryGetValue("총외주액", out v))
                    { try { sheet.Cells[foundRow, 8] = ParseCommaNumber(v); } catch (Exception) { } }
                    if (fields.TryGetValue("외주잔액", out v))
                    { try { sheet.Cells[foundRow, 9] = ParseCommaNumber(v); } catch (Exception) { } }
                }

                try { workbook.Save(); } catch (Exception) { }
                try { workbook.Close(false); } catch (Exception) { }
            }
            catch (Exception ex) { Console.WriteLine("  Excel 쓰기 오류: " + ex.Message); }
            finally
            {
                try { if (sheets != null) Marshal.ReleaseComObject(sheets); } catch (Exception) { }
                try { if (sheet != null) Marshal.ReleaseComObject(sheet); } catch (Exception) { }
                try { if (workbook != null) Marshal.ReleaseComObject(workbook); } catch (Exception) { }
                try { if (excel != null) { excel.Quit(); Marshal.ReleaseComObject(excel); } } catch (Exception) { }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        static object ParseCommaNumber(string value)
        {
            if (string.IsNullOrEmpty(value)) return DBNull.Value;
            double result;
            if (double.TryParse(value.Replace(",", ""), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out result))
                return result;
            return value;
        }

        static void DoSearch(IntPtr sessionHwnd, int deptIndex, string bizNum)
        {
            IntPtr mdiClient = FindChildByClass(sessionHwnd, "MDIClient");
            if (mdiClient == IntPtr.Zero) return;
            IntPtr sproj = FindChildByClass(mdiClient, "Tfrm_sproj");
            if (sproj == IntPtr.Zero) return;
            IntPtr tabSheet = FindActiveTabSheet(sproj);
            if (tabSheet == IntPtr.Zero) return;

            IntPtr companyGroup = GetChildByZOrder(tabSheet, 3);
            IntPtr statusGroup = GetChildByZOrder(tabSheet, 4);
            IntPtr bizEdit = GetChildByZOrder(tabSheet, 10);
            IntPtr searchBtn = GetChildByZOrder(tabSheet, 8);
            IntPtr deptCombo = GetChildByZOrder(tabSheet, 11);
            IntPtr clientEdit = GetChildByZOrder(tabSheet, 5);

            if (deptCombo != IntPtr.Zero)
            {
                int idx = deptIndex - 1;
                if (idx < 0) idx = 0;
                NativeMethods.SendMessageW(deptCombo, NativeMethods.CB_SETCURSEL, (IntPtr)idx, IntPtr.Zero);
            }
            if (clientEdit != IntPtr.Zero)
                NativeMethods.SendMessageW(clientEdit, NativeMethods.WM_SETTEXT, IntPtr.Zero, new StringBuilder(""));
            if (bizEdit != IntPtr.Zero)
                NativeMethods.SendMessageW(bizEdit, NativeMethods.WM_SETTEXT, IntPtr.Zero, new StringBuilder(bizNum));
            if (companyGroup != IntPtr.Zero)
            {
                IntPtr btn = NativeMethods.GetTopWindow(companyGroup);
                if (btn != IntPtr.Zero)
                    NativeMethods.SendMessageW(btn, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            }
            if (statusGroup != IntPtr.Zero)
            {
                IntPtr btn = NativeMethods.GetTopWindow(statusGroup);
                if (btn != IntPtr.Zero)
                    NativeMethods.SendMessageW(btn, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            }
            if (searchBtn != IntPtr.Zero)
            {
                NativeMethods.SendMessageW(searchBtn, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(1500);
            }
            IntPtr grid = GetChildByZOrder(tabSheet, 13);
            if (grid != IntPtr.Zero)
            {
                NativeMethods.SendMessageW(grid, 0x0100, (IntPtr)VK_HOME, IntPtr.Zero);
                Thread.Sleep(100);
                NativeMethods.SendMessageW(grid, 0x0101, (IntPtr)VK_HOME, IntPtr.Zero);
                Thread.Sleep(200);
            }
        }

        static IntPtr FindChildByClass(IntPtr parent, string className)
        {
            IntPtr found = IntPtr.Zero;
            NativeMethods.EnumChildWindows(parent, (child, _) =>
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(child, sb, sb.Capacity);
                if (sb.ToString() == className) { found = child; return false; }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        static IntPtr FindActiveTabSheet(IntPtr parent)
        {
            IntPtr pageCtrl = FindChildByClass(parent, "TPageControl");
            if (pageCtrl == IntPtr.Zero) return IntPtr.Zero;
            IntPtr child = NativeMethods.GetTopWindow(pageCtrl);
            while (child != IntPtr.Zero)
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(child, sb, sb.Capacity);
                if (sb.ToString() == "TTabSheet" && NativeMethods.IsWindowVisible(child))
                    return child;
                child = NativeMethods.GetWindow(child, NativeMethods.GW_HWNDNEXT);
            }
            return IntPtr.Zero;
        }

        static IntPtr GetChildByZOrder(IntPtr parent, int index)
        {
            IntPtr child = NativeMethods.GetTopWindow(parent);
            int i = 0;
            while (child != IntPtr.Zero && i < index)
            {
                child = NativeMethods.GetWindow(child, NativeMethods.GW_HWNDNEXT);
                i++;
            }
            return child;
        }

        static void ExtractContractDetails(IntPtr sessionHwnd)
        {
            Console.WriteLine("\n=== 계약 상세 추출 ===\n");
            IntPtr mdiClient = FindChildByClass(sessionHwnd, "MDIClient");
            if (mdiClient == IntPtr.Zero) { Console.WriteLine("MDIClient 없음"); return; }
            IntPtr sproj = FindChildByClass(mdiClient, "Tfrm_sproj");
            if (sproj == IntPtr.Zero) { Console.WriteLine("Tfrm_sproj 없음"); return; }

            IntPtr outerPageCtrl = IntPtr.Zero;
            IntPtr dc = NativeMethods.GetTopWindow(sproj);
            while (dc != IntPtr.Zero)
            {
                var sbCls = new StringBuilder(256);
                NativeMethods.GetClassNameW(dc, sbCls, sbCls.Capacity);
                if (sbCls.ToString() == "TPageControl") { outerPageCtrl = dc; break; }
                dc = NativeMethods.GetWindow(dc, NativeMethods.GW_HWNDNEXT);
            }
            if (outerPageCtrl == IntPtr.Zero) { Console.WriteLine("외부 페이지 없음"); return; }

            int tabIndex = 0;
            IntPtr contractTab = IntPtr.Zero;
            IntPtr child = NativeMethods.GetTopWindow(outerPageCtrl);
            while (child != IntPtr.Zero)
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(child, sb, sb.Capacity);
                if (sb.ToString() == "TTabSheet")
                {
                    int len = NativeMethods.GetWindowTextLengthW(child);
                    if (len > 0)
                    {
                        var sb2 = new StringBuilder(len + 1);
                        NativeMethods.GetWindowTextW(child, sb2, sb2.Capacity);
                        if (sb2.ToString().Trim() == "계약상세")
                        { contractTab = child; break; }
                    }
                    tabIndex++;
                }
                child = NativeMethods.GetWindow(child, NativeMethods.GW_HWNDNEXT);
            }
            if (contractTab == IntPtr.Zero) { Console.WriteLine("계약상세 탭 없음"); return; }

            NativeMethods.SendMessageW(outerPageCtrl, 0x130B, (IntPtr)tabIndex, IntPtr.Zero);
            Thread.Sleep(500);

            Console.WriteLine("계약상세 탭 자식 목록:\n");
            IntPtr c = NativeMethods.GetTopWindow(contractTab);
            int ci = 0;
            while (c != IntPtr.Zero && ci < 50)
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(c, sb, sb.Capacity);
                string cls = sb.ToString();
                string text = "";
                int len = NativeMethods.GetWindowTextLengthW(c);
                if (len > 0)
                {
                    var sb2 = new StringBuilder(len + 1);
                    NativeMethods.GetWindowTextW(c, sb2, sb2.Capacity);
                    text = sb2.ToString().Trim();
                }
                else
                {
                    len = (int)NativeMethods.SendMessageW(c, NativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
                    if (len > 0)
                    {
                        var sb2 = new StringBuilder(len + 1);
                        NativeMethods.SendMessageW(c, NativeMethods.WM_GETTEXT, (IntPtr)sb2.Capacity, sb2);
                        text = sb2.ToString().Trim();
                    }
                }
                Console.WriteLine("  [" + ci + "] " + cls + " = '" + text + "'");
                c = NativeMethods.GetWindow(c, NativeMethods.GW_HWNDNEXT);
                ci++;
            }
            NativeMethods.SendMessageW(outerPageCtrl, 0x130B, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine("\n완료.");
        }

        static void DiscoverControls(IntPtr sessionHwnd)
        {
            Console.WriteLine("\n=== UI 탐색 모드 ===\n");
            IntPtr mdiClient = IntPtr.Zero;
            NativeMethods.EnumChildWindows(sessionHwnd, (child, _) =>
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(child, sb, sb.Capacity);
                if (sb.ToString() == "MDIClient") { mdiClient = child; return false; }
                return true;
            }, IntPtr.Zero);
            if (mdiClient == IntPtr.Zero) { Console.WriteLine("MDIClient 없음!"); return; }
            Console.WriteLine("MDIClient 발견. MDI 자식 목록:\n");

            var mdiChildren = new List<IntPtr>();
            NativeMethods.EnumChildWindows(mdiClient, (child, _) => { mdiChildren.Add(child); return true; }, IntPtr.Zero);

            foreach (var mdiChild in mdiChildren)
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(mdiChild, sb, sb.Capacity);
                string cls = sb.ToString();
                int len = NativeMethods.GetWindowTextLengthW(mdiChild);
                string text = "";
                if (len > 0)
                {
                    var sb2 = new StringBuilder(len + 1);
                    NativeMethods.GetWindowTextW(mdiChild, sb2, sb2.Capacity);
                    text = sb2.ToString();
                }
                Console.WriteLine("  Class=" + cls + " Text='" + text + "'");
                IntPtr child = NativeMethods.GetTopWindow(mdiChild);
                int idx = 0;
                while (child != IntPtr.Zero && idx < 200)
                {
                    PrintChildInfo(child, idx, 4);
                    child = NativeMethods.GetWindow(child, NativeMethods.GW_HWNDNEXT);
                    idx++;
                }
                Console.WriteLine();
            }

            Console.WriteLine("\n--- 상세 탭 시트 ---\n");
            foreach (var mdiChild in mdiChildren)
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(mdiChild, sb, sb.Capacity);
                if (sb.ToString() != "Tfrm_sproj") continue;
                IntPtr pageCtrl = FindChildByClass(mdiChild, "TPageControl");
                if (pageCtrl == IntPtr.Zero) continue;
                IntPtr tabSheet = FindActiveTabSheet(mdiChild);
                if (tabSheet == IntPtr.Zero) continue;
                IntPtr innerPageCtrl = FindChildByClass(tabSheet, "TPageControl");
                if (innerPageCtrl == IntPtr.Zero) continue;

                IntPtr innerChild = NativeMethods.GetTopWindow(innerPageCtrl);
                while (innerChild != IntPtr.Zero)
                {
                    var sb2 = new StringBuilder(256);
                    NativeMethods.GetClassNameW(innerChild, sb2, sb2.Capacity);
                    if (sb2.ToString() == "TTabSheet")
                    {
                        int len2 = NativeMethods.GetWindowTextLengthW(innerChild);
                        string title = "";
                        if (len2 > 0)
                        {
                            var sb3 = new StringBuilder(len2 + 1);
                            NativeMethods.GetWindowTextW(innerChild, sb3, sb3.Capacity);
                            title = sb3.ToString();
                        }
                        bool vis = NativeMethods.IsWindowVisible(innerChild);
                        Console.WriteLine("Tab: '" + title.Trim() + "' Visible=" + vis);
                        IntPtr c = NativeMethods.GetTopWindow(innerChild);
                        int ci = 0;
                        while (c != IntPtr.Zero && ci < 50)
                        {
                            PrintChildInfo(c, ci, 6);
                            c = NativeMethods.GetWindow(c, NativeMethods.GW_HWNDNEXT);
                            ci++;
                        }
                    }
                    innerChild = NativeMethods.GetWindow(innerChild, NativeMethods.GW_HWNDNEXT);
                }
            }
            Console.WriteLine("완료.");
        }

        static void PrintChildInfo(IntPtr hwnd, int index, int indent)
        {
            var sb = new StringBuilder(256);
            NativeMethods.GetClassNameW(hwnd, sb, sb.Capacity);
            string cls = sb.ToString();
            int len = NativeMethods.GetWindowTextLengthW(hwnd);
            string text = "";
            if (len > 0)
            {
                var sb2 = new StringBuilder(len + 1);
                NativeMethods.GetWindowTextW(hwnd, sb2, sb2.Capacity);
                text = sb2.ToString();
            }
            string label = "";
            if (cls == "TLabeledEdit")
            {
                IntPtr labelChild = NativeMethods.GetTopWindow(hwnd);
                while (labelChild != IntPtr.Zero)
                {
                    var sb3 = new StringBuilder(256);
                    NativeMethods.GetClassNameW(labelChild, sb3, sb3.Capacity);
                    if (sb3.ToString() == "TLabel" || sb3.ToString() == "TLabelText")
                    {
                        int llen = NativeMethods.GetWindowTextLengthW(labelChild);
                        if (llen > 0)
                        {
                            var sb4 = new StringBuilder(llen + 1);
                            NativeMethods.GetWindowTextW(labelChild, sb4, sb4.Capacity);
                            label = sb4.ToString();
                        }
                        break;
                    }
                    labelChild = NativeMethods.GetWindow(labelChild, NativeMethods.GW_HWNDNEXT);
                }
            }
            if (string.IsNullOrEmpty(label) && (cls == "TNumberEdit" || cls == "TMaskEdit" || cls == "TLabeledEdit"))
            {
                IntPtr labelChild = NativeMethods.GetTopWindow(hwnd);
                while (labelChild != IntPtr.Zero)
                {
                    var sb3 = new StringBuilder(256);
                    NativeMethods.GetClassNameW(labelChild, sb3, sb3.Capacity);
                    if (sb3.ToString() == "TLabel" || sb3.ToString() == "TLabelText")
                    {
                        int llen = NativeMethods.GetWindowTextLengthW(labelChild);
                        if (llen > 0)
                        {
                            var sb4 = new StringBuilder(llen + 1);
                            NativeMethods.GetWindowTextW(labelChild, sb4, sb4.Capacity);
                            label = sb4.ToString().Trim();
                        }
                        break;
                    }
                    labelChild = NativeMethods.GetWindow(labelChild, NativeMethods.GW_HWNDNEXT);
                }
            }
            string pad = new string(' ', indent);
            Console.Write(pad + "[" + index + "] Class=" + cls);
            if (!string.IsNullOrEmpty(label)) Console.Write(" Label='" + label + "'");
            if (!string.IsNullOrEmpty(text)) Console.Write(" Text='" + text.Trim() + "'");
            Console.WriteLine(" Visible=" + NativeMethods.IsWindowVisible(hwnd));
        }

        static void DiscoverLoginWindow()
        {
            Console.WriteLine("=== 로그인 창 탐색 ===");
            IntPtr loginHwnd = FindWindowByTitle(LoginWindowTitle, true);
            Console.WriteLine("FindWindowByTitle('Sboard'): " + loginHwnd);
            if (loginHwnd == IntPtr.Zero)
            {
                Console.WriteLine("없음. Sboard 실행중...");
                string exePath = @"C:\Program Files (x86)\sprog\sboard.exe";
                LaunchSboard(exePath);
                loginHwnd = WaitForWindow(LoginWindowTitle, 15);
                if (loginHwnd == IntPtr.Zero)
                { Console.WriteLine("실패: 15초 내 로그인 창 없음"); return; }
            }
            Console.WriteLine("로그인 창 핸들: " + loginHwnd);
            Console.WriteLine("자식 목록 (재귀):\n");
            _elements.Clear();
            WalkWindowTree(loginHwnd, 0, 20);
            Console.WriteLine(_elements.Count + "개 요소 발견:\n");
            foreach (var e in _elements)
            {
                string pad = new string(' ', e.Depth * 2);
                Console.WriteLine(pad + "[" + e.Depth + "] Class=" + e.ClassName
                    + " Text='" + e.Text + "' Visible=" + e.Visible
                    + " (" + e.X + "," + e.Y + " " + e.Width + "x" + e.Height + ")");
            }
            Console.WriteLine("\n");

            Console.WriteLine("=== 모든 Sboard 창 ===");
            var allSboard = FindAllWindowsByTitle("Sboard");
            foreach (var w in allSboard)
                Console.WriteLine("  핸들=" + w.Item1 + " 제목='" + w.Item2 + "'");

            Console.WriteLine("\n완료.");
        }

        static void DiscoverMenu()
        {
            Console.WriteLine("=== 메뉴 탐색 ===");
            IntPtr hwnd = FindWindowByTitle(SessionPrefix, false);
            if (hwnd == IntPtr.Zero)
            {
                Console.WriteLine("Sboard 세션 없음.");
                return;
            }
            Console.WriteLine("세션 창 핸들: " + hwnd);
            IntPtr hMenu = NativeMethods.GetMenu(hwnd);
            if (hMenu == IntPtr.Zero)
            {
                Console.WriteLine("메뉴 없음 (GetMenu=0).");
                return;
            }
            Console.WriteLine("메뉴 핸들: " + hMenu + "\n");
            PrintMenu(hMenu, 0);
            Console.WriteLine("\n완료.");
        }

        static void PrintMenu(IntPtr hMenu, int depth)
        {
            int count = NativeMethods.GetMenuItemCount(hMenu);
            for (int i = 0; i < count; i++)
            {
                var sb = new StringBuilder(256);
                int len = NativeMethods.GetMenuString(hMenu, i, sb, sb.Capacity, NativeMethods.MF_BYPOSITION);
                string text = (len > 0) ? sb.ToString().Trim() : "";
                uint id1 = NativeMethods.GetMenuItemID(hMenu, i);

                var mii = new NativeMethods.MENUITEMINFO();
                mii.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(mii);
                mii.fMask = NativeMethods.MIIM_ID;
                mii.dwTypeData = new string(' ', 256);
                mii.cch = 256;
                string id2 = "?";
                if (NativeMethods.GetMenuItemInfo(hMenu, i, true, ref mii))
                    id2 = "" + mii.wID;

                IntPtr sub = NativeMethods.GetSubMenu(hMenu, i);
                string pad = new string(' ', depth * 2);
                Console.WriteLine(pad + "[" + i + "] GetMenuItemID=" + id1
                    + " MIIM.wID=" + id2 + " Text='" + text + "'");
                if (sub != IntPtr.Zero)
                    PrintMenu(sub, depth + 1);
            }
        }

        static void TestMenuId(int id)
        {
            Console.WriteLine("메뉴 ID 테스트 " + id);
            IntPtr hwnd = FindWindowByTitle(SessionPrefix, false);
            if (hwnd == IntPtr.Zero)
            {
                hwnd = FindWindowByTitle("Sboard", false);
                if (hwnd == IntPtr.Zero)
                { Console.WriteLine("Sboard 창 없음."); return; }
            }
            Console.WriteLine("WM_COMMAND 전송 ID=" + id + " → " + hwnd);
            NativeMethods.PostMessageW(hwnd, NativeMethods.WM_COMMAND, (IntPtr)(uint)id, IntPtr.Zero);
            Console.WriteLine("전송 완료. 창이 열렸는지 확인하세요.");
        }

        static void TestAllMenuIds()
        {
            IntPtr hwnd = FindWindowByTitle(SessionPrefix, false);
            if (hwnd == IntPtr.Zero)
            {
                hwnd = FindWindowByTitle("Sboard", false);
                if (hwnd == IntPtr.Zero)
                { Console.WriteLine("Sboard 창 없음."); return; }
            }
            int[] ids = new int[] { 4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23 };
            Console.WriteLine(ids.Length + "개 메뉴 ID 테스트 (" + hwnd + ")");
            Console.WriteLine("Enter 누를 때마다 다음 ID 전송.\n");
            for (int i = 0; i < ids.Length; i++)
            {
                Console.Write("ID=" + ids[i] + " (" + (i+1) + "/" + ids.Length + "). Enter 입력중... ");
                Console.ReadLine();
                NativeMethods.PostMessageW(hwnd, NativeMethods.WM_COMMAND, (IntPtr)(uint)ids[i], IntPtr.Zero);
                Console.Write("전송. 용역현황 열렸으면 y 입력 (아니면 Enter): ");
                string answer = Console.ReadLine();
                if (answer != null && answer.Trim().ToLower() == "y")
                { Console.WriteLine("발견! 용역현황 ID=" + ids[i]); Console.ReadLine(); return; }
            }
            Console.WriteLine("\n전체 테스트 완료. 용역현황 없음?");
        }

        static void InputCredentials(IntPtr hwnd, string id, string pw)
        {
            List<IntPtr> edits = new List<IntPtr>();
            NativeMethods.EnumChildWindows(hwnd, (child, _) =>
            {
                StringBuilder sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(child, sb, sb.Capacity);
                string cls = sb.ToString();
                if (cls == "Edit" || cls == "TEdit" || cls == "TLabeledEdit")
                    edits.Add(child);
                return true;
            }, IntPtr.Zero);

            if (edits.Count >= 1)
                NativeMethods.SendMessageW(edits[0], NativeMethods.WM_SETTEXT, IntPtr.Zero, new StringBuilder(pw));
            if (edits.Count >= 2)
                NativeMethods.SendMessageW(edits[1], NativeMethods.WM_SETTEXT, IntPtr.Zero, new StringBuilder(id));

            Thread.Sleep(200);
            NativeMethods.SetForegroundWindow(hwnd);
            Thread.Sleep(100);
            PressKey(NativeMethods.VK_RETURN);
        }

        static void PressKey(byte vk)
        {
            NativeMethods.keybd_event(vk, 0, 0, IntPtr.Zero);
            Thread.Sleep(20);
            NativeMethods.keybd_event(vk, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
            Thread.Sleep(20);
        }

        static IntPtr WaitForWindow(string title, int timeoutSec)
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < timeoutSec)
            {
                IntPtr hwnd = FindWindowByTitle(title, true);
                if (hwnd != IntPtr.Zero) return hwnd;
                Thread.Sleep(200);
            }
            return IntPtr.Zero;
        }

        static IntPtr WaitForSession(int timeoutSec)
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < timeoutSec)
            {
                IntPtr hwnd = FindWindowByTitle(SessionPrefix, false);
                if (hwnd != IntPtr.Zero) return hwnd;
                IntPtr loginHwnd = FindWindowByTitle(LoginWindowTitle, true);
                if (loginHwnd == IntPtr.Zero) return hwnd;
                Thread.Sleep(200);
            }
            return IntPtr.Zero;
        }

        static IntPtr FindWindowByTitle(string titlePart, bool exactMatch)
        {
            IntPtr found = IntPtr.Zero;
            NativeMethods.EnumWindows((hwnd, _) =>
            {
                if (!NativeMethods.IsWindowVisible(hwnd)) return true;
                int len = NativeMethods.GetWindowTextLengthW(hwnd);
                if (len > 0)
                {
                    var sb = new StringBuilder(len + 1);
                    NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
                    string title = sb.ToString();
                    bool match = exactMatch ? title.Trim() == titlePart : title.StartsWith(titlePart);
                    if (match) found = hwnd;
                }
                return found == IntPtr.Zero;
            }, IntPtr.Zero);
            return found;
        }

        static List<Tuple<IntPtr, string>> FindAllWindowsByTitle(string titlePart)
        {
            var results = new List<Tuple<IntPtr, string>>();
            NativeMethods.EnumWindows((hwnd, _) =>
            {
                if (!NativeMethods.IsWindowVisible(hwnd)) return true;
                int len = NativeMethods.GetWindowTextLengthW(hwnd);
                if (len > 0)
                {
                    var sb = new StringBuilder(len + 1);
                    NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
                    string title = sb.ToString();
                    if (title.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) >= 0)
                        results.Add(Tuple.Create(hwnd, title));
                }
                return true;
            }, IntPtr.Zero);
            return results;
        }

        static IntPtr DetectErrorDialog(uint pid)
        {
            IntPtr found = IntPtr.Zero;
            NativeMethods.EnumWindows((hwnd, _) =>
            {
                uint wpid;
                NativeMethods.GetWindowThreadProcessId(hwnd, out wpid);
                if (wpid != pid) return true;
                if (!NativeMethods.IsWindowVisible(hwnd)) return true;
                int len = NativeMethods.GetWindowTextLengthW(hwnd);
                if (len > 0)
                {
                    var sb = new StringBuilder(len + 1);
                    NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
                    string title = sb.ToString();
                    if (title.Contains("Sboard") && !title.StartsWith(SessionPrefix))
                        found = hwnd;
                }
                return found == IntPtr.Zero;
            }, IntPtr.Zero);
            return found;
        }

        static void LaunchSboard(string exePath)
        {
            if (!File.Exists(exePath))
            {
                string envPath = Environment.GetEnvironmentVariable("SBOARD_EXE_PATH");
                if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                    exePath = envPath;
                else
                {
                    string[] fallbacks = {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sboard.exe"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sboard.exe"),
                        @"C:\Program Files\sprog\sboard.exe"
                    };
                    foreach (var f in fallbacks)
                        if (File.Exists(f)) { exePath = f; break; }
                }
            }
            var psi = new ProcessStartInfo(exePath)
            { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Normal };
            Process.Start(psi);
        }

        static void KillSboard()
        {
            foreach (var proc in Process.GetProcessesByName("sboard"))
            { try { proc.Kill(); } catch { } }
        }

        static void ExtractViaWin32(IntPtr hwnd)
        {
            int before = _elements.Count;
            WalkWindowTree(hwnd, 0);
            Console.WriteLine("  Win32: " + (_elements.Count - before) + "개 요소");
        }

        static void WalkWindowTree(IntPtr hwnd, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;
            var sbClass = new StringBuilder(256);
            NativeMethods.GetClassNameW(hwnd, sbClass, sbClass.Capacity);
            string className = sbClass.ToString();
            int len = NativeMethods.GetWindowTextLengthW(hwnd);
            string text = "";
            if (len > 0)
            {
                var sb = new StringBuilder(len + 1);
                NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
                text = sb.ToString().Trim();
            }
            else
            {
                len = (int)NativeMethods.SendMessageW(hwnd, NativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
                if (len > 0)
                {
                    var sb = new StringBuilder(len + 1);
                    NativeMethods.SendMessageW(hwnd, NativeMethods.WM_GETTEXT, (IntPtr)sb.Capacity, sb);
                    text = sb.ToString().Trim();
                }
            }
            bool visible = NativeMethods.IsWindowVisible(hwnd);
            NativeMethods.RECT rect;
            NativeMethods.GetWindowRect(hwnd, out rect);
            if (!string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(className))
            {
                _elements.Add(new UIElementInfo
                {
                    Depth = depth, ClassName = className, Text = text,
                    Visible = visible, X = rect.Left, Y = rect.Top,
                    Width = rect.Right - rect.Left, Height = rect.Bottom - rect.Top
                });
            }
            NativeMethods.EnumChildWindows(hwnd, (childHwnd, _) =>
            { WalkWindowTree(childHwnd, depth + 1, maxDepth); return true; }, IntPtr.Zero);
        }

        static void WalkWindowTree(IntPtr hwnd, int depth)
        {
            WalkWindowTree(hwnd, depth, 8);
        }

        static void ScreenshotAndOCR(IntPtr hwnd)
        {
            try
            {
                NativeMethods.RECT rect;
                NativeMethods.GetWindowRect(hwnd, out rect);
                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;
                if (w <= 0 || h <= 0) return;
                string screenshotPath = Path.GetFullPath("sboard_screenshot.png");
                string ocrOutput = Path.GetFullPath("sboard_ocr");
                using (var bitmap = new Bitmap(w, h))
                {
                    using (var g = Graphics.FromImage(bitmap))
                    { g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(w, h)); }
                    bitmap.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                string tesseractPath = @"C:\Program Files\Tesseract-OCR\tesseract.exe";
                if (!File.Exists(tesseractPath)) return;
                string args = "\"" + screenshotPath + "\" \"" + ocrOutput + "\" -l kor+eng --psm 6";
                var psi = new ProcessStartInfo(tesseractPath, args)
                { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                using (var ocr = Process.Start(psi)) { ocr.WaitForExit(30000); }
                string ocrTextFile = ocrOutput + ".txt";
                if (File.Exists(ocrTextFile))
                {
                    string ocrText = File.ReadAllText(ocrTextFile, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(ocrText))
                    {
                        foreach (var line in ocrText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string trimmed = line.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed))
                                _elements.Add(new UIElementInfo { Depth = 0, ClassName = "OCR", ControlType = "OCR_Text", Text = trimmed, Visible = true });
                        }
                    }
                }
            }
            catch { }
        }

        static void SaveToCsv()
        {
            using (var sw = new StreamWriter(_outputPath, false, Encoding.UTF8))
            {
                sw.WriteLine("Depth,Class,ControlType,Text,Visible,X,Y,Width,Height");
                foreach (var e in _elements)
                {
                    string text = EscapeCsv(e.Text);
                    sw.WriteLine(e.Depth + "," + EscapeCsv(e.ClassName) + "," +
                        EscapeCsv(e.ControlType) + "," + text + "," +
                        e.Visible + "," + e.X + "," + e.Y + "," +
                        e.Width + "," + e.Height);
                }
            }
        }

        static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        static Dictionary<string, string> ReadContractDetails(IntPtr sessionHwnd)
        {
            var result = new Dictionary<string, string>();
            IntPtr mdiClient = FindChildByClass(sessionHwnd, "MDIClient");
            if (mdiClient == IntPtr.Zero) return result;
            IntPtr sproj = FindChildByClass(mdiClient, "Tfrm_sproj");
            if (sproj == IntPtr.Zero) return result;

            IntPtr outerPageCtrl = FindChildByClass(sproj, "TPageControl");
            if (outerPageCtrl == IntPtr.Zero) return result;
            IntPtr tabSheet = IntPtr.Zero;
            IntPtr ch = NativeMethods.GetTopWindow(outerPageCtrl);
            while (ch != IntPtr.Zero)
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(ch, sb, sb.Capacity);
                if (sb.ToString() == "TTabSheet" && NativeMethods.IsWindowVisible(ch))
                { tabSheet = ch; break; }
                ch = NativeMethods.GetWindow(ch, NativeMethods.GW_HWNDNEXT);
            }
            if (tabSheet == IntPtr.Zero) return result;

            IntPtr innerPageCtrl = FindChildByClass(tabSheet, "TPageControl");
            if (innerPageCtrl == IntPtr.Zero) return result;

            int tabIndex = 0;
            IntPtr contractTab = IntPtr.Zero;
            IntPtr child = NativeMethods.GetTopWindow(innerPageCtrl);
            while (child != IntPtr.Zero)
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(child, sb, sb.Capacity);
                if (sb.ToString() == "TTabSheet")
                {
                    int len = NativeMethods.GetWindowTextLengthW(child);
                    if (len > 0)
                    {
                        var sb2 = new StringBuilder(len + 1);
                        NativeMethods.GetWindowTextW(child, sb2, sb2.Capacity);
                        if (sb2.ToString().Trim() == "계약상세")
                        { contractTab = child; break; }
                    }
                    tabIndex++;
                }
                child = NativeMethods.GetWindow(child, NativeMethods.GW_HWNDNEXT);
            }
            if (contractTab == IntPtr.Zero) return result;

            NativeMethods.SendMessageW(innerPageCtrl, 0x130B, (IntPtr)tabIndex, IntPtr.Zero);
            Thread.Sleep(500);

            result["사업명"] = GetControlText(contractTab, 30);
            result["공급가액"] = GetControlText(contractTab, 28);
            result["총수금액"] = GetControlText(contractTab, 24);
            result["잔액"] = GetControlText(contractTab, 23);
            result["총외주액"] = GetControlText(contractTab, 10);
            result["외주잔액"] = GetControlText(contractTab, 9);

            NativeMethods.SendMessageW(innerPageCtrl, 0x130B, IntPtr.Zero, IntPtr.Zero);
            Thread.Sleep(300);
            return result;
        }

        static string GetControlText(IntPtr parent, int zorderIndex)
        {
            IntPtr c = NativeMethods.GetTopWindow(parent);
            int i = 0;
            while (c != IntPtr.Zero && i < zorderIndex)
            {
                c = NativeMethods.GetWindow(c, NativeMethods.GW_HWNDNEXT);
                i++;
            }
            if (c == IntPtr.Zero) return "";
            int len = NativeMethods.GetWindowTextLengthW(c);
            if (len > 0)
            {
                var sb = new StringBuilder(len + 1);
                NativeMethods.GetWindowTextW(c, sb, sb.Capacity);
                return sb.ToString().Trim();
            }
            len = (int)NativeMethods.SendMessageW(c, NativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
            if (len > 0)
            {
                var sb = new StringBuilder(len + 1);
                NativeMethods.SendMessageW(c, NativeMethods.WM_GETTEXT, (IntPtr)sb.Capacity, sb);
                return sb.ToString().Trim();
            }
            return "";
        }


    }
}
