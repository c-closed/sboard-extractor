using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Automation;

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

        public const uint WM_GETTEXT = 0x000D;
        public const uint WM_GETTEXTLENGTH = 0x000E;

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
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                ?? ".",
            "sboard_data.csv");
        private const string LoginWindowTitle = "Sboard";
        private const string SessionPrefix = "Sboard [";

        private const byte VK_UP = 0x26;
        private const byte VK_LEFT = 0x25;
        private const byte VK_RIGHT = 0x27;
        private const byte VK_SPACE = 0x20;
        private const byte VK_HOME = 0x24;
        private const byte VK_END = 0x23;

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("=== Sboard Data Extractor ===");
            Console.WriteLine();

            bool discoverMode = false;
            bool extractMode = false;
            string xlsxPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a == "--discover" || a == "-d") discoverMode = true;
                if (a == "--extract-details" || a == "-e") extractMode = true;
                if (a == "--xlsx" && i + 1 < args.Length) { xlsxPath = args[++i]; }
            }

            if (!NativeMethods.IsUserAnAdmin())
            {
                Console.Error.WriteLine("Error: Need administrator privileges.");
                Console.Error.WriteLine("Please right-click and run as Administrator.");
                Console.Error.WriteLine("Or run from an elevated command prompt.");
                Environment.Exit(1);
                return;
            }

            string userId = "220807";
            string password = "0906";

            string sboardExe = @"C:\Program Files (x86)\sprog\sboard.exe";
            if (args.Length > 0 && !args[0].StartsWith("-")) sboardExe = args[0];
            if (args.Length > 1 && !args[1].StartsWith("-")) _outputPath = args[1];

            string inpFile = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    ?? AppDomain.CurrentDomain.BaseDirectory,
                "1팀수행사업.inp");

            Run(userId, password, sboardExe, inpFile, discoverMode, extractMode, xlsxPath);
        }

        static void Run(string userId, string password, string exePath, string inpFile, bool discoverMode, bool extractMode, string xlsxPath)
        {
            IntPtr sessionHwnd = IntPtr.Zero;

            if (extractMode)
            {
                // Don't kill, attach to existing session
                sessionHwnd = FindWindowByTitle(SessionPrefix, false);
                if (sessionHwnd == IntPtr.Zero)
                {
                    sessionHwnd = FindWindowByTitle("Sboard", false);
                }
                if (sessionHwnd == IntPtr.Zero)
                {
                    Console.Error.WriteLine("No Sboard session found");
                    return;
                }
                ExtractContractDetails(sessionHwnd);
                return;
            }

            var sessionWindows = FindAllWindowsByTitle(SessionPrefix);
            var loginWindows = FindAllWindowsByTitle(LoginWindowTitle);

            bool foundSession = sessionWindows.Exists(w => w.Item2.StartsWith(SessionPrefix));
            bool foundLogin = loginWindows.Exists(w => w.Item2.Trim() == LoginWindowTitle);

            if (foundSession || foundLogin)
            {
                Console.Write("Sboard is running. Killing...");
                KillSboard();
                Thread.Sleep(1000);
                Console.WriteLine(" OK");
            }

            Console.Write("Launching Sboard...");
            LaunchSboard(exePath);
            Console.WriteLine(" OK");

            Console.Write("Waiting for login window...");
            IntPtr hwnd = WaitForWindow(LoginWindowTitle, 15);
            if (hwnd == IntPtr.Zero) { Console.Error.WriteLine(" FAILED"); return; }
            Console.WriteLine(" OK");
            Thread.Sleep(500);

            Console.Write("Logging in...");
            InputCredentials(hwnd, userId, password);
            Console.WriteLine(" OK");

            Console.Write("Waiting for session...");
            sessionHwnd = WaitForSession(15);
            if (sessionHwnd == IntPtr.Zero)
            {
                uint pid = 0;
                NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
                if (DetectErrorDialog(pid) != IntPtr.Zero)
                    Console.Error.WriteLine(" FAILED (wrong credentials)");
                else
                    Console.Error.WriteLine(" FAILED (timeout)");
                sessionHwnd = FindWindowByTitle("Sboard", false);
                if (sessionHwnd == IntPtr.Zero) return;
            }
            else
            {
                Console.WriteLine(" OK (logged in)");
            }

            // === MENU ===
            Console.Write("Opening menu (Alt+3)...");
            NativeMethods.SetForegroundWindow(sessionHwnd);
            Thread.Sleep(500);
            PressKey(NativeMethods.VK_MENU);
            Thread.Sleep(50);
            PressKey(0x33);
            Thread.Sleep(50);
            NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
            Console.WriteLine(" OK");
            Thread.Sleep(1000);

            Console.Write("Selecting menu item...");
            for (int i = 0; i < 9; i++)
                PressKey(NativeMethods.VK_DOWN);
            PressKey(NativeMethods.VK_RETURN);
            Console.WriteLine(" OK");
            Thread.Sleep(1000);

            if (discoverMode)
            {
                DiscoverControls(sessionHwnd);
                return;
            }

            if (extractMode)
            {
                ExtractContractDetails(sessionHwnd);
                return;
            }

            // === BATCH SEARCH ===
            // Auto-detect xlsx in exe directory
            if (string.IsNullOrEmpty(xlsxPath))
            {
                string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    ?? AppDomain.CurrentDomain.BaseDirectory;
                var xlsxFiles = Directory.GetFiles(exeDir, "*.xlsx");
                if (xlsxFiles.Length > 0)
                    xlsxPath = xlsxFiles[0];
            }

            if (string.IsNullOrEmpty(xlsxPath) && !File.Exists(inpFile))
            {
                Console.Error.WriteLine("File not found: " + inpFile);
                Console.WriteLine("Create '" + inpFile + "' with format: deptIndex, bizNumber (e.g. 1, 21020A)");
                Console.WriteLine("Or use --xlsx <file.xlsx> to read from Excel");
                return;
            }

            // Start fresh: delete existing CSV so it's overwritten
            if (File.Exists(_outputPath))
                File.Delete(_outputPath);

            // Build item list from xlsx or inp
            var items = new List<SearchItem>();
            if (!string.IsNullOrEmpty(xlsxPath))
            {
                items = ReadXlsxBizList(xlsxPath);
                Console.WriteLine("\nProcessing " + items.Count + " items from " + Path.GetFileName(xlsxPath));
            }
            else
            {
                string[] lines = File.ReadAllLines(inpFile, Encoding.UTF8);
                Console.WriteLine("\nProcessing " + lines.Length + " items from " + Path.GetFileName(inpFile));
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
            }

            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                Console.Write("\n[" + (idx + 1) + "/" + items.Count + "] Dept=" + item.DeptIndex + " Biz=" + item.BizNum + " ...");

                DoSearch(sessionHwnd, item.DeptIndex, item.BizNum);
                Console.Write(" search OK");

                Thread.Sleep(1000);
                var fields = ReadContractDetails(sessionHwnd);
                if (fields.Count > 0)
                {
                    string v;
                    string projName = fields.TryGetValue("사업명", out v) ? v : "";
                    string supply = fields.TryGetValue("공급가액", out v) ? v : "";
                    Console.Write(" [" + projName + " " + supply + "]");
                    SaveSearchResult(item.BizNum, item.DeptIndex, fields);
                    if (!string.IsNullOrEmpty(xlsxPath))
                        WriteXlsxRow(xlsxPath, item.BizNum, fields);
                    Console.WriteLine(" data saved");
                }
                else
                {
                    Console.WriteLine(" (no data)");
                }
                Thread.Sleep(500);
            }

            Console.WriteLine("\nDone.");
        }

        class SearchItem
        {
            public string BizNum;
            public int DeptIndex;
        }

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

                    // Collect sheet names
                    var sheetNames = new System.Collections.Generic.List<string>();
                    foreach (System.Data.DataRow r in dt.Rows)
                        sheetNames.Add(r["TABLE_NAME"].ToString());

                    // Pick the sheet with most rows (data sheet, not summary)
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
                    if (string.IsNullOrEmpty(sheetName)) { Console.WriteLine("  No data sheet found"); return result; }

                    // Read via column indices (0=진행상태, 1=사업번호, 2=부서인덱스)
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
            catch (Exception ex) { Console.WriteLine("  Error reading xlsx: " + ex.Message); }
            return result;
        }

        static void WriteXlsxRow(string xlsxPath, string bizNum, Dictionary<string, string> fields)
        {
            try
            {
                Type excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null) { Console.Write(" Excel not installed"); return; }
                dynamic excel = Activator.CreateInstance(excelType);
                try
                {
                    excel.Visible = false;
                    dynamic workbook = excel.Workbooks.Open(xlsxPath);

                    // Find the data sheet by enumerating names
                    dynamic sheets = workbook.Sheets;
                    dynamic sheet = null;
                    string sheetName = "";
                    int maxCount = 0;
                    foreach (dynamic s in sheets)
                    {
                        string sn = s.Name;
                        int cnt = s.UsedRange.Rows.Count;
                        if (cnt > maxCount) { maxCount = cnt; sheet = s; sheetName = sn; }
                    }

                    if (sheet == null) { Console.Write(" No sheet found"); workbook.Close(false); return; }

                    // Find row with matching 사업번호 (column B = 2)
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
                        if (fields.TryGetValue("사업명", out v)) sheet.Cells[foundRow, 4] = v;
                        if (fields.TryGetValue("공급가액", out v)) sheet.Cells[foundRow, 5] = ParseCommaNumber(v);
                        if (fields.TryGetValue("총수금액", out v)) sheet.Cells[foundRow, 6] = ParseCommaNumber(v);
                        if (fields.TryGetValue("잔액", out v)) sheet.Cells[foundRow, 7] = ParseCommaNumber(v);
                        if (fields.TryGetValue("총외주액", out v)) sheet.Cells[foundRow, 8] = ParseCommaNumber(v);
                        if (fields.TryGetValue("외주잔액", out v)) sheet.Cells[foundRow, 9] = ParseCommaNumber(v);
                    }

                    workbook.Save();
                    workbook.Close(false);
                }
                finally
                {
                    excel.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excel);
                }
            }
            catch (Exception ex) { Console.Write(" Error writing xlsx: " + ex.Message); }
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
            if (mdiClient == IntPtr.Zero) { Console.Error.Write(" (no MDIClient)"); return; }

            IntPtr sproj = FindChildByClass(mdiClient, "Tfrm_sproj");
            if (sproj == IntPtr.Zero) { Console.Error.Write(" (no Tfrm_sproj)"); return; }

            IntPtr tabSheet = FindActiveTabSheet(sproj);
            if (tabSheet == IntPtr.Zero) { Console.Error.Write(" (no tab sheet)"); return; }

            // Z-order indices within the tab sheet:
            // [3] TRadioGroup '회사', [4] TRadioGroup '진행상태', [5] TLabeledEdit (사업번호)
            // [8] TBitBtn '검색', [11] TComboBox (부서)
            IntPtr companyGroup = GetChildByZOrder(tabSheet, 3);
            IntPtr statusGroup  = GetChildByZOrder(tabSheet, 4);
            IntPtr bizEdit      = GetChildByZOrder(tabSheet, 10);  // 사업번호
            IntPtr searchBtn    = GetChildByZOrder(tabSheet, 8);
            IntPtr deptCombo    = GetChildByZOrder(tabSheet, 11);  // 부서 콤보
            IntPtr clientEdit   = GetChildByZOrder(tabSheet, 5);   // 발주처명

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
                IntPtr btn = NativeMethods.GetTopWindow(companyGroup); // first = 전체
                if (btn != IntPtr.Zero)
                    NativeMethods.SendMessageW(btn, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            }

            if (statusGroup != IntPtr.Zero)
            {
                IntPtr btn = NativeMethods.GetTopWindow(statusGroup); // first = 전체
                if (btn != IntPtr.Zero)
                    NativeMethods.SendMessageW(btn, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            }

            if (searchBtn != IntPtr.Zero)
            {
                NativeMethods.SendMessageW(searchBtn, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(1500);
            }

            // Select first row in results grid to ensure 계약상세 tab updates
            IntPtr grid = GetChildByZOrder(tabSheet, 13);
            if (grid != IntPtr.Zero)
            {
                // Force grid to select first row via direct window messages
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
            // Find TPageControl, then find visible TTabSheet
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
            Console.WriteLine("\n=== Extract Contract Details ===\n");

            IntPtr mdiClient = FindChildByClass(sessionHwnd, "MDIClient");
            if (mdiClient == IntPtr.Zero) { Console.WriteLine("No MDIClient"); return; }

            IntPtr sproj = FindChildByClass(mdiClient, "Tfrm_sproj");
            if (sproj == IntPtr.Zero) { Console.WriteLine("No Tfrm_sproj"); return; }

            IntPtr outerPageCtrl = IntPtr.Zero;
            IntPtr dc = NativeMethods.GetTopWindow(sproj);
            while (dc != IntPtr.Zero)
            {
                var sbCls = new StringBuilder(256);
                NativeMethods.GetClassNameW(dc, sbCls, sbCls.Capacity);
                if (sbCls.ToString() == "TPageControl") { outerPageCtrl = dc; break; }
                dc = NativeMethods.GetWindow(dc, NativeMethods.GW_HWNDNEXT);
            }
            if (outerPageCtrl == IntPtr.Zero) { Console.WriteLine("No outer page control"); return; }

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

            if (contractTab == IntPtr.Zero) { Console.WriteLine("No 계약상세 tab"); return; }

            NativeMethods.SendMessageW(outerPageCtrl, 0x130B, (IntPtr)tabIndex, IntPtr.Zero);
            Thread.Sleep(500);

            Console.WriteLine("Children of 계약상세 tab:\n");
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
            Console.WriteLine("\nDone.");
        }

        static void DiscoverControls(IntPtr sessionHwnd)
        {
            Console.WriteLine("\n=== UI Discovery Mode ===\n");

            IntPtr mdiClient = IntPtr.Zero;
            NativeMethods.EnumChildWindows(sessionHwnd, (child, _) =>
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(child, sb, sb.Capacity);
                if (sb.ToString() == "MDIClient") { mdiClient = child; return false; }
                return true;
            }, IntPtr.Zero);

            if (mdiClient == IntPtr.Zero) { Console.WriteLine("MDIClient not found!"); return; }
            Console.WriteLine("Found MDIClient. MDI children:\n");

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

            // Also dump the detail tab sheets (계약상세) with labels
            Console.WriteLine("\n--- Detail tab sheets ---\n");
            foreach (var mdiChild in mdiChildren)
            {
                var sb = new StringBuilder(256);
                NativeMethods.GetClassNameW(mdiChild, sb, sb.Capacity);
                if (sb.ToString() != "Tfrm_sproj") continue;

                IntPtr pageCtrl = FindChildByClass(mdiChild, "TPageControl");
                if (pageCtrl == IntPtr.Zero) continue;

                // Find the INNER TPageControl (the one under the tab sheet)
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
                        int len = NativeMethods.GetWindowTextLengthW(innerChild);
                        string title = "";
                        if (len > 0)
                        {
                            var sb3 = new StringBuilder(len + 1);
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

            Console.WriteLine("Done.");
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

            // Get label text (for TLabeledEdit, find its TLabel child)
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
            // For TNumberEdit and TMaskEdit, check for previous sibling label
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

        static void InputCredentials(IntPtr hwnd, string id, string pw)
        {
            NativeMethods.SetForegroundWindow(hwnd);
            Thread.Sleep(150);

            // Tab x2 to ID field
            PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);
            PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);

            // Paste ID from clipboard
            NativeMethods.SetForegroundWindow(hwnd);
            Thread.Sleep(150);
            SetClipboard(id);
            PasteWithControlV();

            // Tab to PW field
            PressKey(NativeMethods.VK_TAB); Thread.Sleep(50);

            // Paste PW from clipboard
            NativeMethods.SetForegroundWindow(hwnd);
            Thread.Sleep(150);
            SetClipboard(pw);
            PasteWithControlV();

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

        static void SetClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            for (int retry = 0; retry < 10; retry++)
            {
                try
                {
                    using (var sta = new ManualResetEvent(false))
                    {
                        Exception ex = null;
                        Thread t = new Thread(() =>
                        {
                            try { System.Windows.Forms.Clipboard.SetText(text); }
                            catch (Exception e) { ex = e; }
                            finally { sta.Set(); }
                        });
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        sta.WaitOne(5000);
                        if (ex == null) return;
                        if (retry < 9) Thread.Sleep(200);
                        else throw ex;
                    }
                }
                catch (ExternalException)
                {
                    if (retry < 9) Thread.Sleep(200);
                    else throw;
                }
            }
        }

        static void PasteWithControlV()
        {
            NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, IntPtr.Zero);
            Thread.Sleep(10);
            NativeMethods.keybd_event(NativeMethods.VK_V, 0, 0, IntPtr.Zero);
            Thread.Sleep(10);
            NativeMethods.keybd_event(NativeMethods.VK_V, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
            Thread.Sleep(10);
            NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, IntPtr.Zero);
            Thread.Sleep(10);
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
                if (loginHwnd == IntPtr.Zero) return hwnd; // login window disappeared -> probably logged in but session not yet found
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
                    {
                        if (File.Exists(f)) { exePath = f; break; }
                    }
                }
            }

            var psi = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            Process.Start(psi);
        }

        static void KillSboard()
        {
            foreach (var proc in Process.GetProcessesByName("sboard"))
            {
                try { proc.Kill(); } catch { }
            }
        }

        static void ExtractViaWin32(IntPtr hwnd)
        {
            Console.Write("  Win32 API...");
            int before = _elements.Count;
            WalkWindowTree(hwnd, 0);
            Console.WriteLine(" " + (_elements.Count - before) + " elements");
        }

        static void WalkWindowTree(IntPtr hwnd, int depth)
        {
            if (depth > 8) return;

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
                    Depth = depth,
                    ClassName = className,
                    Text = text,
                    Visible = visible,
                    X = rect.Left, Y = rect.Top,
                    Width = rect.Right - rect.Left,
                    Height = rect.Bottom - rect.Top
                });
            }

            NativeMethods.EnumChildWindows(hwnd, (childHwnd, _) =>
            {
                WalkWindowTree(childHwnd, depth + 1);
                return true;
            }, IntPtr.Zero);
        }

        static void ExtractViaUIA(Process proc)
        {
            Console.Write("  UI Automation...");
            int before = _elements.Count;
            try
            {
                var cond = new PropertyCondition(AutomationElement.ProcessIdProperty, proc.Id);
                var window = AutomationElement.RootElement.FindFirst(TreeScope.Children, cond);
                if (window == null) { Console.WriteLine(" (no window)"); return; }
                WalkUIA(window, 0);
                Console.WriteLine(" " + (_elements.Count - before) + " elements");
            }
            catch (Exception ex)
            {
                Console.WriteLine(" (error: " + ex.Message + ")");
            }
        }

        static void WalkUIA(AutomationElement el, int depth)
        {
            if (depth > 8) return;

            string name = el.Current.Name;
            string help = el.Current.HelpText;
            string ct = el.Current.ControlType.ProgrammaticName;
            string cls = el.Current.ClassName;
            var rect = el.Current.BoundingRectangle;

            string value = "";
            object valPattern = null;
            if (el.TryGetCurrentPattern(ValuePattern.Pattern, out valPattern))
            {
                try { value = ((ValuePattern)valPattern).Current.Value; } catch { }
            }

            string text = !string.IsNullOrEmpty(name) ? name : "";
            if (!string.IsNullOrEmpty(value)) text = value;

            if (!string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(help))
            {
                _elements.Add(new UIElementInfo
                {
                    Depth = depth,
                    ClassName = cls,
                    ControlType = ct,
                    Text = text,
                    Visible = !el.Current.IsOffscreen,
                    X = (int)rect.X, Y = (int)rect.Y,
                    Width = (int)rect.Width, Height = (int)rect.Height
                });
            }

            var child = TreeWalker.RawViewWalker.GetFirstChild(el);
            int count = 0;
            while (child != null && count < 2000)
            {
                WalkUIA(child, depth + 1);
                child = TreeWalker.RawViewWalker.GetNextSibling(child);
                count++;
            }
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

                using (var bitmap = new System.Drawing.Bitmap(w, h))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(rect.Left, rect.Top, 0, 0,
                            new System.Drawing.Size(w, h));
                    }
                    bitmap.Save(screenshotPath, System.Drawing.Imaging.ImageFormat.Png);
                }

                string tesseractPath = @"C:\Program Files\Tesseract-OCR\tesseract.exe";
                if (!File.Exists(tesseractPath)) return;

                string args = "\"" + screenshotPath + "\" \"" + ocrOutput + "\" -l kor+eng --psm 6";
                var psi = new ProcessStartInfo(tesseractPath, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var ocr = Process.Start(psi))
                {
                    ocr.WaitForExit(30000);
                }

                string ocrTextFile = ocrOutput + ".txt";
                if (File.Exists(ocrTextFile))
                {
                    string ocrText = File.ReadAllText(ocrTextFile, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(ocrText))
                    {
                        string[] lines = ocrText.Split(new[] { '\r', '\n' },
                            StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            string trimmed = line.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed))
                            {
                                _elements.Add(new UIElementInfo
                                {
                                    Depth = 0,
                                    ClassName = "OCR",
                                    ControlType = "OCR_Text",
                                    Text = trimmed,
                                    Visible = true
                                });
                            }
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

        static void SavePartialCsv(string path, int startIndex)
        {
            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.WriteLine("Depth,Class,ControlType,Text,Visible,X,Y,Width,Height");
                for (int i = startIndex; i < _elements.Count; i++)
                {
                    var e = _elements[i];
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
            if (s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 ||
                s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        static string ReadPassword()
        {
            var pass = new StringBuilder();
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass.Length--;
                    Console.Write("\b \b");
                }
                else if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    pass.Append(key.KeyChar);
                    Console.Write('*');
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass.ToString();
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

        static void SaveSearchResult(string bizNum, int deptIndex, Dictionary<string, string> fields)
        {
            bool needsHeader = !File.Exists(_outputPath) || new FileInfo(_outputPath).Length == 0;
            using (var sw = new StreamWriter(_outputPath, true, Encoding.UTF8))
            {
                if (needsHeader)
                    sw.WriteLine("사업번호,부서인덱스,사업명,공급가액,총수금액,잔액,총외주액,외주잔액");
                string v = "";
                fields.TryGetValue("사업명", out v);
                string 사업명 = EscapeCsv(v);
                fields.TryGetValue("공급가액", out v);
                string 공급가액 = EscapeCsv(v);
                fields.TryGetValue("총수금액", out v);
                string 총수금액 = EscapeCsv(v);
                fields.TryGetValue("잔액", out v);
                string 잔액 = EscapeCsv(v);
                fields.TryGetValue("총외주액", out v);
                string 총외주액 = EscapeCsv(v);
                fields.TryGetValue("외주잔액", out v);
                string 외주잔액 = EscapeCsv(v);
                sw.WriteLine(EscapeCsv(bizNum) + "," + deptIndex + ","
                    + 사업명 + "," + 공급가액 + "," + 총수금액 + ","
                    + 잔액 + "," + 총외주액 + "," + 외주잔액);
            }
            Console.WriteLine("  Saved to " + _outputPath);
        }
    }
}
