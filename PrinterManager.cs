using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Net.Sockets;

namespace PrinterManager
{
    public class MainForm : Form
    {
        // UI Controls
        private TabControl tabControl;
        
        // Tab 1: Client
        private TextBox txtServerIP;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private CheckBox chkSaveConfig;
        private Button btnConnect;
        private Button btnDiagnoseConnection;
        private ListBox lstRemoteShares;
        private Button btnInstallPrinter;
        private CheckBox chkAutoSetDefault;
        private Button btnOpenShare;
        
        private ListBox lstLocalPrinters;
        private Button btnSetDefault;
        private Button btnDeletePrinter;
        private Button btnRefreshLocal;

        // Tab 2: Server (Sharing)
        private ListBox lstMyPrinters;
        private Button btnSharePrinter;
        private Button btnConfigNoPass;
        private Button btnRefreshMyPrinters;
        private TextBox txtShareName;

        // Tab 3: Repair
        private Button btnFixSpooler_Host;
        private Button btnFixNetwork_Host;
        private Button btnDisableFirewall_Host;
        private Button btnFixPolicy_Client;
        private Button btnFixSpooler_Client;
        private TextBox txtLog;

        private ToolStripStatusLabel statusLabel;
        
        private const string CONFIG_FILE = "printer_config.txt";

        public MainForm()
        {
            InitializeComponent();
            LoadConfig();
            RefreshAllPrinters();
            CheckAdmin();
        }

        private void CheckAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("警告：当前程序未以管理员身份运行。\n部分高级功能（如一键修复、设置共享）可能无法正常工作。\n请右键程序选择“以管理员身份运行”。", "权限提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "全能打印机共享管理工具 (Win7/10/11兼容版) - 作者：R";
            this.Size = new Size(650, 700); // Increased height for better layout
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            TabPage tabClient = new TabPage("连接打印机/文件");
            CreateClientTab(tabClient);
            tabControl.TabPages.Add(tabClient);

            TabPage tabServer = new TabPage("本机共享设置");
            CreateServerTab(tabServer);
            tabControl.TabPages.Add(tabServer);

            TabPage tabRepair = new TabPage("一键修复工具");
            CreateRepairTab(tabRepair);
            tabControl.TabPages.Add(tabRepair);

            StatusStrip statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel() { Text = "就绪" };
            statusStrip.Items.Add(statusLabel);
            statusStrip.Dock = DockStyle.Bottom;

            this.Controls.Add(tabControl);
            this.Controls.Add(statusStrip);
        }

        private void CreateClientTab(TabPage tab)
        {
            GroupBox grpConnection = new GroupBox();
            grpConnection.Text = "服务器连接";
            grpConnection.Location = new Point(10, 10);
            grpConnection.Size = new Size(600, 100);
            
            Label lblIP = new Label() { Text = "服务器IP:", Location = new Point(15, 25), AutoSize = true };
            txtServerIP = new TextBox() { Location = new Point(80, 22), Width = 150, Text = "10.154.1.206" };
            
            Label lblUser = new Label() { Text = "用户名:", Location = new Point(250, 25), AutoSize = true };
            txtUsername = new TextBox() { Location = new Point(300, 22), Width = 100 };
            
            Label lblPass = new Label() { Text = "密码:", Location = new Point(410, 25), AutoSize = true };
            txtPassword = new TextBox() { Location = new Point(450, 22), Width = 100, PasswordChar = '*' };
            
            chkSaveConfig = new CheckBox() { Text = "保存配置", Location = new Point(80, 55), AutoSize = true, Checked = true };
            
            btnConnect = new Button() { Text = "连接并列出资源", Location = new Point(300, 55), Width = 250, Height = 30 };
            btnConnect.Click += BtnConnect_Click;
            btnConnect.Width = 120;
            btnDiagnoseConnection = new Button() { Text = "连接诊断", Location = new Point(430, 55), Width = 120, Height = 30 };
            btnDiagnoseConnection.Click += BtnDiagnoseConnection_Click;

            grpConnection.Controls.AddRange(new Control[] { lblIP, txtServerIP, lblUser, txtUsername, lblPass, txtPassword, chkSaveConfig, btnConnect, btnDiagnoseConnection });

            GroupBox grpRemote = new GroupBox();
            grpRemote.Text = "远程共享资源 (打印机/文件夹)";
            grpRemote.Location = new Point(10, 120);
            grpRemote.Size = new Size(600, 180);
            
            lstRemoteShares = new ListBox() { Location = new Point(15, 25), Size = new Size(400, 140) };
            
            chkAutoSetDefault = new CheckBox() { Text = "安装后自动设为默认", Location = new Point(430, 25), AutoSize = true, Checked = true };
            
            btnInstallPrinter = new Button() { Text = "一键安装选中打印机", Location = new Point(430, 50), Size = new Size(150, 40) };
            btnInstallPrinter.Click += BtnInstallPrinter_Click;

            btnOpenShare = new Button() { Text = "打开选中文件夹", Location = new Point(430, 100), Size = new Size(150, 40) };
            btnOpenShare.Click += BtnOpenShare_Click;
            
            grpRemote.Controls.AddRange(new Control[] { lstRemoteShares, chkAutoSetDefault, btnInstallPrinter, btnOpenShare });

            GroupBox grpLocal = new GroupBox();
            grpLocal.Text = "本机已安装打印机";
            grpLocal.Location = new Point(10, 310);
            grpLocal.Size = new Size(600, 180);
            
            lstLocalPrinters = new ListBox() { Location = new Point(15, 25), Size = new Size(400, 140) };
            
            btnSetDefault = new Button() { Text = "设为默认", Location = new Point(430, 25), Size = new Size(150, 40) };
            btnSetDefault.Click += BtnSetDefault_Click;
            
            btnDeletePrinter = new Button() { Text = "删除打印机", Location = new Point(430, 75), Size = new Size(150, 40) };
            btnDeletePrinter.Click += BtnDeletePrinter_Click;
            
            btnRefreshLocal = new Button() { Text = "刷新列表", Location = new Point(430, 125), Size = new Size(150, 30) };
            btnRefreshLocal.Click += (s, e) => RefreshLocalPrinters();
            
            grpLocal.Controls.AddRange(new Control[] { lstLocalPrinters, btnSetDefault, btnDeletePrinter, btnRefreshLocal });

            tab.Controls.AddRange(new Control[] { grpConnection, grpRemote, grpLocal });
        }

        private void CreateServerTab(TabPage tab)
        {
            GroupBox grpMyPrinters = new GroupBox();
            grpMyPrinters.Text = "本机打印机列表";
            grpMyPrinters.Location = new Point(10, 10);
            grpMyPrinters.Size = new Size(600, 280);

            lstMyPrinters = new ListBox() { Location = new Point(15, 25), Size = new Size(400, 240) };
            
            Label lblShareName = new Label() { Text = "共享名称:", Location = new Point(430, 25), AutoSize = true };
            txtShareName = new TextBox() { Location = new Point(430, 45), Width = 150 };

            btnSharePrinter = new Button() { Text = "设置选中为共享", Location = new Point(430, 80), Size = new Size(150, 40) };
            btnSharePrinter.Click += BtnSharePrinter_Click;

            btnRefreshMyPrinters = new Button() { Text = "刷新列表", Location = new Point(430, 130), Size = new Size(150, 30) };
            btnRefreshMyPrinters.Click += (s, e) => RefreshLocalPrinters();

            grpMyPrinters.Controls.AddRange(new Control[] { lstMyPrinters, lblShareName, txtShareName, btnSharePrinter, btnRefreshMyPrinters });

            GroupBox grpConfig = new GroupBox();
            grpConfig.Text = "高级共享配置 (一键修改组策略/注册表)";
            grpConfig.Location = new Point(10, 300);
            grpConfig.Size = new Size(600, 170);

            Label lblDesc = new Label() { Text = "说明：开启免密共享将执行以下操作\n1. 启用 Guest 账户并置空密码\n2. 修改组策略允许空密码登录\n3. 允许匿名访问共享", Location = new Point(15, 25), AutoSize = true };
            
            btnConfigNoPass = new Button() { Text = "一键开启免密共享 (推荐)", Location = new Point(15, 90), Size = new Size(200, 40) };
            btnConfigNoPass.Click += BtnConfigNoPass_Click;

            grpConfig.Controls.AddRange(new Control[] { lblDesc, btnConfigNoPass });

            tab.Controls.AddRange(new Control[] { grpMyPrinters, grpConfig });
        }

        private void CreateRepairTab(TabPage tab)
        {
            // === Host Repair Group ===
            GroupBox grpHost = new GroupBox();
            grpHost.Text = "主机端修复 (本机作为共享主机)";
            grpHost.Location = new Point(10, 10);
            grpHost.Size = new Size(600, 150);

            btnFixSpooler_Host = new Button() { Text = "重启打印服务 (Spooler)", Location = new Point(15, 30), Size = new Size(250, 40) };
            btnFixSpooler_Host.Click += (s, e) => RunRepair("spooler");

            btnFixNetwork_Host = new Button() { Text = "开启网络发现与共享服务", Location = new Point(280, 30), Size = new Size(250, 40) };
            btnFixNetwork_Host.Click += (s, e) => RunRepair("network");

            btnDisableFirewall_Host = new Button() { Text = "一键关闭本机防火墙", Location = new Point(15, 85), Size = new Size(250, 40), ForeColor = Color.Red };
            btnDisableFirewall_Host.Click += (s, e) => RunRepair("disable_firewall");

            Button btnUnlockUser_Host = new Button() { Text = "解除用户锁定 (解锁账号)", Location = new Point(280, 85), Size = new Size(250, 40) };
            btnUnlockUser_Host.Click += (s, e) => RunRepair("unlock_user");

            Label lblHostDesc = new Label() { Text = "如果其他电脑无法发现或连接此电脑，请尝试上述修复。", Location = new Point(15, 130), AutoSize = true, ForeColor = Color.Gray };
            grpHost.Controls.AddRange(new Control[] { btnFixSpooler_Host, btnFixNetwork_Host, btnDisableFirewall_Host, btnUnlockUser_Host, lblHostDesc });


            // === Client Repair Group ===
            GroupBox grpClient = new GroupBox();
            grpClient.Text = "客户端修复 (本机去连接别人)";
            grpClient.Location = new Point(10, 170);
            grpClient.Size = new Size(600, 150);

            btnFixPolicy_Client = new Button() { Text = "一键修复连接策略 (含Win10/11/24H2)", Location = new Point(15, 30), Size = new Size(400, 40) };
            btnFixPolicy_Client.Click += (s, e) => RunRepair("guest_policy");

            btnFixSpooler_Client = new Button() { Text = "重启打印服务 (Spooler)", Location = new Point(15, 85), Size = new Size(250, 40) };
            btnFixSpooler_Client.Click += (s, e) => RunRepair("spooler");

            Label lblClientDesc = new Label() { Text = "解决“无法访问”、“扩展错误”、“组织策略阻止”等问题。", Location = new Point(15, 130), AutoSize = true, ForeColor = Color.Gray };
            grpClient.Controls.AddRange(new Control[] { btnFixPolicy_Client, btnFixSpooler_Client, lblClientDesc });


            // Log Area
            Label lblLog = new Label() { Text = "操作日志:", Location = new Point(10, 330), AutoSize = true };
            txtLog = new TextBox() { Location = new Point(10, 350), Size = new Size(600, 200), Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

            tab.Controls.AddRange(new Control[] { grpHost, grpClient, lblLog, txtLog });
        }

        private void LoadConfig()
        {
            if (File.Exists(CONFIG_FILE))
            {
                try
                {
                    string[] lines = File.ReadAllLines(CONFIG_FILE);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("IP=")) txtServerIP.Text = line.Substring(3);
                        if (line.StartsWith("User=")) txtUsername.Text = line.Substring(5);
                    }
                }
                catch { }
            }
        }

        private void SaveConfig()
        {
            if (chkSaveConfig.Checked)
            {
                try
                {
                    File.WriteAllLines(CONFIG_FILE, new string[] { 
                        "IP=" + txtServerIP.Text.Trim(),
                        "User=" + txtUsername.Text.Trim()
                    });
                }
                catch { }
            }
        }

        private void UpdateStatus(string msg)
        {
            statusLabel.Text = msg;
            Application.DoEvents();
        }

        private void Log(string msg)
        {
            if (txtLog != null)
                txtLog.AppendText(string.Format("[{0}] {1}\r\n", DateTime.Now.ToLongTimeString(), msg));
            UpdateStatus(msg);
        }

        private void RunCommand(string command, string args, Action<string> outputCallback = null)
        {
            CommandResult result = RunCommandDetailed(command, args);
            if (outputCallback != null)
            {
                outputCallback(result.StdOut);
            }
        }

        private class CommandResult
        {
            public int ExitCode;
            public string StdOut;
            public string StdErr;
        }

        private CommandResult RunCommandDetailed(string command, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = command;
            psi.Arguments = args;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.StandardOutputEncoding = Encoding.GetEncoding("gb2312");
            psi.StandardErrorEncoding = Encoding.GetEncoding("gb2312");

            try
            {
                using (Process proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    return new CommandResult
                    {
                        ExitCode = proc.ExitCode,
                        StdOut = output ?? string.Empty,
                        StdErr = error ?? string.Empty
                    };
                }
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    ExitCode = -1,
                    StdOut = string.Empty,
                    StdErr = ex.Message
                };
            }
        }

        private string EscapeArg(string value)
        {
            if (value == null) return string.Empty;
            return value.Replace("\"", "\\\"");
        }

        private List<string> BuildUserCandidates(string ip, string user)
        {
            List<string> userCandidates = new List<string>();
            if (string.IsNullOrEmpty(user))
            {
                return userCandidates;
            }

            if (user.Contains("\\") || user.Contains("@"))
            {
                userCandidates.Add(user);
            }
            else
            {
                userCandidates.Add(user);
                userCandidates.Add(".\\" + user);
                userCandidates.Add(ip + "\\" + user);
            }

            return userCandidates;
        }

        private bool IsRunningAsAdmin()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private bool TestTcpPort(string host, int port, int timeoutMs, out string error)
        {
            error = string.Empty;
            TcpClient client = new TcpClient();
            try
            {
                IAsyncResult ar = client.BeginConnect(host, port, null, null);
                bool ok = ar.AsyncWaitHandle.WaitOne(timeoutMs);
                if (!ok)
                {
                    error = "timeout";
                    return false;
                }

                client.EndConnect(ar);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                try { client.Close(); } catch { }
            }
        }

        private string GetCombinedOutput(CommandResult result)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(result.StdOut)) sb.AppendLine(result.StdOut.Trim());
            if (!string.IsNullOrEmpty(result.StdErr)) sb.AppendLine(result.StdErr.Trim());
            return sb.ToString().Trim();
        }

        private void AppendCommandResult(StringBuilder sb, string title, CommandResult result, int maxLines)
        {
            sb.AppendLine(string.Format("{0}: ExitCode={1}", title, result.ExitCode));
            string combined = GetCombinedOutput(result);
            if (string.IsNullOrEmpty(combined))
            {
                sb.AppendLine("  (无输出)");
                return;
            }

            string[] lines = combined.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int count = 0;
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                sb.AppendLine("  " + line);
                count++;
                if (count >= maxLines)
                {
                    if (lines.Length > maxLines) sb.AppendLine("  ...");
                    break;
                }
            }
        }

        private bool ContainsAny(string text, params string[] patterns)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (string pattern in patterns)
            {
                if (text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool TryConnectServer(string ip, string user, string pass, out string usedUser, out string errorSummary)
        {
            usedUser = string.Empty;
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(user))
            {
                CommandResult anonymousResult = RunCommandDetailed("net", string.Format("use \\\\{0} /persistent:no", ip));
                if (anonymousResult.ExitCode == 0)
                {
                    errorSummary = string.Empty;
                    return true;
                }

                errors.Add(string.Format("anonymous (code {0}): {1}", anonymousResult.ExitCode, anonymousResult.StdErr.Trim()));
                errorSummary = string.Join("\r\n", errors.ToArray());
                return false;
            }

            HashSet<string> testedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string candidateUser in BuildUserCandidates(ip, user))
            {
                if (!testedUsers.Add(candidateUser)) continue;

                string args = string.Format(
                    "use \\\\{0} /user:\"{1}\" \"{2}\" /persistent:no",
                    ip,
                    EscapeArg(candidateUser),
                    EscapeArg(pass));

                CommandResult result = RunCommandDetailed("net", args);
                if (result.ExitCode == 0)
                {
                    usedUser = candidateUser;
                    errorSummary = string.Empty;
                    return true;
                }

                errors.Add(string.Format("{0} (code {1}): {2}", candidateUser, result.ExitCode, result.StdErr.Trim()));
            }

            errorSummary = string.Join("\r\n", errors.ToArray());
            return false;
        }

        private void BtnDiagnoseConnection_Click(object sender, EventArgs e)
        {
            string ip = txtServerIP.Text.Trim();
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("请输入服务器IP");
                return;
            }

            SaveConfig();
            UpdateStatus("正在执行连接诊断...");

            StringBuilder report = new StringBuilder();
            report.AppendLine("=== SMB 连接诊断 ===");
            report.AppendLine("时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("目标: " + ip);
            report.AppendLine("用户名: " + (string.IsNullOrEmpty(user) ? "(未填写)" : user));
            report.AppendLine("管理员权限: " + (IsRunningAsAdmin() ? "是" : "否"));
            report.AppendLine();

            string portError;
            bool port445Open = TestTcpPort(ip, 445, 2500, out portError);
            report.AppendLine("TCP 445: " + (port445Open ? "可达" : "不可达"));
            if (!port445Open && !string.IsNullOrEmpty(portError))
            {
                report.AppendLine("  " + portError);
            }
            report.AppendLine();

            RunCommandDetailed("net", string.Format("use \\\\{0} /delete /y", ip));

            List<CommandResult> authResults = new List<CommandResult>();
            List<string> authTitles = new List<string>();
            bool authSuccess = false;
            string usedUser = string.Empty;

            if (string.IsNullOrEmpty(user))
            {
                CommandResult anonymousResult = RunCommandDetailed("net", string.Format("use \\\\{0} /persistent:no", ip));
                authResults.Add(anonymousResult);
                authTitles.Add("net use (anonymous)");
                authSuccess = anonymousResult.ExitCode == 0;
            }
            else
            {
                HashSet<string> testedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string candidateUser in BuildUserCandidates(ip, user))
                {
                    if (!testedUsers.Add(candidateUser)) continue;

                    string args = string.Format(
                        "use \\\\{0} /user:\"{1}\" \"{2}\" /persistent:no",
                        ip,
                        EscapeArg(candidateUser),
                        EscapeArg(pass));

                    CommandResult result = RunCommandDetailed("net", args);
                    authResults.Add(result);
                    authTitles.Add("net use /user:" + candidateUser);

                    if (result.ExitCode == 0)
                    {
                        authSuccess = true;
                        usedUser = candidateUser;
                        break;
                    }
                }
            }

            report.AppendLine("认证测试:");
            for (int i = 0; i < authResults.Count; i++)
            {
                AppendCommandResult(report, authTitles[i], authResults[i], 4);
            }
            if (authSuccess && !string.IsNullOrEmpty(usedUser))
            {
                report.AppendLine("  成功账号格式: " + usedUser);
            }
            report.AppendLine();

            CommandResult viewResult = RunCommandDetailed("net", string.Format("view \\\\{0}", ip));
            report.AppendLine("共享枚举测试:");
            AppendCommandResult(report, "net view \\\\" + ip, viewResult, 8);
            report.AppendLine();

            RunCommandDetailed("net", string.Format("use \\\\{0} /delete /y", ip));

            List<string> hints = new List<string>();
            string allText = report.ToString();

            if (!port445Open)
            {
                hints.Add("445端口不通：先检查两台机器网络互通、防火墙规则和“文件和打印机共享”。");
            }
            if (!authSuccess)
            {
                hints.Add("认证未通过：Win11 建议用“主机名\\\\用户名”或“.\\\\用户名”，并确认密码正确。");
            }
            if (ContainsAny(allText, "1219"))
            {
                hints.Add("检测到 1219（凭据冲突）：先执行 net use * /delete /y，再重新连接。");
            }
            if (ContainsAny(allText, "system error 5", "系统错误 5", "access is denied", "拒绝访问"))
            {
                hints.Add("检测到错误 5（拒绝访问）：检查共享权限/NTFS 权限和本地安全策略。");
            }
            if (ContainsAny(allText, "system error 53", "系统错误 53"))
            {
                hints.Add("检测到错误 53（网络路径找不到）：检查IP、SMB服务、网络发现状态。");
            }
            if (ContainsAny(allText, "system error 67", "系统错误 67"))
            {
                hints.Add("检测到错误 67（网络名找不到）：共享名不存在或服务端共享发布异常。");
            }
            if (ContainsAny(allText, "system error 86", "系统错误 86", "1326"))
            {
                hints.Add("检测到凭据错误（86/1326）：请重新确认账号格式和密码。");
            }
            if (hints.Count == 0)
            {
                hints.Add("未识别到典型错误码；若仍失败，先运行“修复连接策略”并重启两端 Spooler 后再试。");
            }

            report.AppendLine("建议:");
            for (int i = 0; i < hints.Count; i++)
            {
                report.AppendLine(string.Format("{0}. {1}", i + 1, hints[i]));
            }

            if (txtLog != null)
            {
                txtLog.AppendText("\r\n================ 连接诊断 ================\r\n");
                txtLog.AppendText(report.ToString());
                txtLog.AppendText("\r\n");
            }

            ShowTextDialog("连接诊断结果", report.ToString());
            UpdateStatus("连接诊断完成");
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            string ip = txtServerIP.Text.Trim();
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("请输入服务器IP");
                return;
            }

            SaveConfig();
            UpdateStatus("正在连接服务器...");

            RunCommand("net", string.Format("use \\\\{0} /delete /y", ip));

            string usedUser;
            string connectError;
            if (!TryConnectServer(ip, user, pass, out usedUser, out connectError))
            {
                Log("SMB authentication failed, continue with share enumeration.");
                if (!string.IsNullOrEmpty(connectError))
                {
                    Log(connectError);
                }

                MessageBox.Show(
                    "连接服务器认证失败，程序将继续尝试读取共享列表。\r\n\r\n" +
                    "如果是 Win11 到 Win11，建议用户名改成：主机名\\用户名 或 .\\用户名。\r\n" +
                    "也可先执行“修复连接策略”后重试。",
                    "认证失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else if (!string.IsNullOrEmpty(usedUser))
            {
                Log("Connected with user: " + usedUser);
            }

            UpdateStatus("正在获取资源列表 (API)...");
            lstRemoteShares.Items.Clear();

            List<ShareInfo> shares = NetNetwork.GetShares(ip);
            
            if (shares.Count > 0)
            {
                foreach (var share in shares)
                {
                    if (share.NetName.EndsWith("$") && !share.NetName.Equals("IPC$", StringComparison.OrdinalIgnoreCase)) continue;
                    if (share.NetName.Equals("IPC$", StringComparison.OrdinalIgnoreCase)) continue;

                    string typeStr = "Folder";
                    if (share.ShareType == 1) typeStr = "Print"; 
                    
                    string display = string.Format("{0} [{1}]", share.NetName, typeStr);
                    lstRemoteShares.Items.Add(display);
                }
                UpdateStatus(string.Format("找到 {0} 个共享资源", lstRemoteShares.Items.Count));
            }
            else
            {
                UpdateStatus("尝试使用 CMD 获取列表...");
                RunCommand("net", string.Format("view \\\\{0}", ip), (output) => {
                    string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;
                        if (trimmed.StartsWith("Share", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("共享名", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("---", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("The command completed", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("命令成功", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string[] parts = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            string typeStr = "Folder";
                            if (parts[1].IndexOf("print", StringComparison.OrdinalIgnoreCase) >= 0 || parts[1].Contains("打印"))
                            {
                                typeStr = "Print";
                            }
                            lstRemoteShares.Items.Add(string.Format("{0} [{1}]", parts[0], typeStr));
                        }
                    }
                });
            }

            if (lstRemoteShares.Items.Count == 0)
            {
                if (MessageBox.Show("未找到任何共享资源。\n\n这可能是因为：\n1. 服务器未开启共享。\n2. 防火墙阻止了列表获取。\n3. Windows 版本兼容性问题。\n\n是否尝试直接打开远程文件夹？", "未找到资源", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", string.Format("\\\\{0}", ip));
                }
            }
        }

        private void BtnInstallPrinter_Click(object sender, EventArgs e)
        {
            if (lstRemoteShares.SelectedItem == null)
            {
                MessageBox.Show("请先选择一台远程打印机");
                return;
            }

            string selection = lstRemoteShares.SelectedItem.ToString();
            if (selection.IndexOf("[Print]", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (MessageBox.Show("当前选中的共享看起来不是打印机，继续安装可能失败。是否继续？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
            }

            string shareName = selection.Split('[')[0].Trim();
            string serverIP = txtServerIP.Text.Trim();
            string fullPath = string.Format("\\\\{0}\\{1}", serverIP, shareName);

            UpdateStatus(string.Format("正在安装 {0} ...", shareName));

            // Install
            ProcessStartInfo psi = new ProcessStartInfo("rundll32", string.Format("printui.dll,PrintUIEntry /in /n \"{0}\" /q", fullPath));
            Process p = Process.Start(psi);
            p.WaitForExit();

            if (p.ExitCode == 0)
            {
                string msg = "安装成功！";
                
                // Set Default
                if (chkAutoSetDefault.Checked)
                {
                    Process.Start("rundll32", string.Format("printui.dll,PrintUIEntry /y /n \"{0}\"", fullPath));
                    msg += "\n已自动设为默认打印机。";
                }
                
                MessageBox.Show(msg);
                RefreshAllPrinters();
            }
            else
            {
                MessageBox.Show("安装失败。错误代码: " + p.ExitCode);
            }
            UpdateStatus("就绪");
        }

        private void BtnOpenShare_Click(object sender, EventArgs e)
        {
            string serverIP = txtServerIP.Text.Trim();
            string path = string.Format("\\\\{0}", serverIP);

            if (lstRemoteShares.SelectedItem != null)
            {
                string selection = lstRemoteShares.SelectedItem.ToString();
                string shareName = selection.Split('[')[0].Trim();
                path = string.Format("\\\\{0}\\{1}", serverIP, shareName);
            }
            else
            {
                if (string.IsNullOrEmpty(serverIP))
                {
                    MessageBox.Show("请先连接服务器或输入IP");
                    return;
                }
            }

            try
            {
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开失败: " + ex.Message);
            }
        }

        private void RefreshAllPrinters()
        {
            RefreshLocalPrinters();
        }

        private void RefreshLocalPrinters()
        {
            lstLocalPrinters.Items.Clear();
            if (lstMyPrinters != null) lstMyPrinters.Items.Clear();

            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                lstLocalPrinters.Items.Add(printer);
                if (lstMyPrinters != null) lstMyPrinters.Items.Add(printer);
            }
        }

        private void BtnSetDefault_Click(object sender, EventArgs e)
        {
            if (lstLocalPrinters.SelectedItem == null) return;
            string printerName = lstLocalPrinters.SelectedItem.ToString();
            Process.Start("rundll32", string.Format("printui.dll,PrintUIEntry /y /n \"{0}\"", printerName));
            MessageBox.Show(string.Format("已将 {0} 设为默认", printerName));
        }

        private void BtnDeletePrinter_Click(object sender, EventArgs e)
        {
            if (lstLocalPrinters.SelectedItem == null) return;
            string printerName = lstLocalPrinters.SelectedItem.ToString();
            if (MessageBox.Show(string.Format("确定要删除打印机 {0} 吗？", printerName), "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Process p = Process.Start("rundll32", string.Format("printui.dll,PrintUIEntry /dl /n \"{0}\" /q", printerName));
                p.WaitForExit();
                RefreshLocalPrinters();
            }
        }

        private void BtnSharePrinter_Click(object sender, EventArgs e)
        {
            if (lstMyPrinters.SelectedItem == null)
            {
                MessageBox.Show("请选择要共享的本机打印机");
                return;
            }
            string printerName = lstMyPrinters.SelectedItem.ToString();
            string shareName = txtShareName.Text.Trim();
            if (string.IsNullOrEmpty(shareName)) shareName = printerName.Replace(" ", "_");
            
            string args = string.Format("printui.dll,PrintUIEntry /Xs /n \"{0}\" Sharename \"{1}\" Attributes +Shared", printerName, shareName);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("rundll32", args);
                Process p = Process.Start(psi);
                p.WaitForExit();
                MessageBox.Show("已尝试设置共享。如果成功，其他电脑应该能看到。");
            }
            catch (Exception ex) { MessageBox.Show("操作失败: " + ex.Message); }
        }

        private void BtnConfigNoPass_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("此操作将修改注册表和用户策略以开启免密共享。\n\n确定要继续吗？", "安全警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                RunCommand("net", "user guest /active:yes");
                RunCommand("net", "user guest \"\""); 
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "RestrictAnonymous", 0);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 0);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "EveryoneIncludesAnonymous", 1);
                
                // Fix User Rights (Clear Deny Network Logon)
                FixUserRights();

                MessageBox.Show("配置完成！\n已尝试移除 '拒绝从网络访问' 策略。\n\n注意：如果仍无法连接，请重启电脑。");
            }
            catch (Exception ex) { MessageBox.Show("配置失败 (需要管理员权限): " + ex.Message); }
        }

        private void FixUserRights()
        {
            try
            {
                string cfgPath = Path.Combine(Path.GetTempPath(), "sec_fix.inf");
                string dbPath = Path.Combine(Path.GetTempPath(), "sec_fix.sdb");
                
                // Create a minimal INF to clear SeDenyNetworkLogonRight
                // This removes everyone (including Guest) from the Deny list
                string content = "[Unicode]\r\nUnicode=yes\r\n[Version]\r\nsignature=\"$CHICAGO$\"\r\nRevision=1\r\n[Privilege Rights]\r\nSeDenyNetworkLogonRight =\r\n";
                File.WriteAllText(cfgPath, content, Encoding.Unicode);
                
                RunCommand("secedit", string.Format("/configure /db \"{0}\" /cfg \"{1}\" /areas USER_RIGHTS", dbPath, cfgPath));
                Log("已清空 '拒绝从网络访问这台计算机' 策略 (允许Guest)");
                
                // Clean up
                if (File.Exists(cfgPath)) File.Delete(cfgPath);
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
            catch (Exception ex) { Log("组策略修复失败: " + ex.Message); }
        }

        private void RunRepair(string type)
        {
            try
            {
                Log("开始修复: " + type);
                if (type == "spooler")
                {
                    RunCommand("net", "stop spooler");
                    RunCommand("net", "start spooler");
                    Log("Print Spooler 服务已重启");
                }
                else if (type == "network")
                {
                    RunCommand("net", "start LanmanServer");
                    RunCommand("net", "start LanmanWorkstation");
                    RunCommand("net", "start fdPHost");
                    RunCommand("net", "start FDResPub");
                    RunCommand("netsh", "advfirewall firewall set rule group=\"File and Printer Sharing\" new enable=Yes");
                    RunCommand("netsh", "advfirewall firewall set rule group=\"文件和打印机共享\" new enable=Yes");
                    
                    // Switch Network to Private (Category = 1)
                    try
                    {
                        string profilesPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles";
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(profilesPath, true))
                        {
                            if (key != null)
                            {
                                foreach (string subKeyName in key.GetSubKeyNames())
                                {
                                    using (RegistryKey subKey = key.OpenSubKey(subKeyName, true))
                                    {
                                        if (subKey != null) subKey.SetValue("Category", 1, RegistryValueKind.DWord);
                                    }
                                }
                                Log("已尝试将所有网络配置设为【专用网络】");
                            }
                        }
                    }
                    catch { Log("切换网络配置文件失败 (可能权限不足)"); }
                    
                    Log("网络发现与共享服务已启动，防火墙规则已允许");
                }
                else if (type == "guest_policy")
                {
                    string lmParams = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters";
                    string polParams = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\LanmanWorkstation";
                    string serverParams = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters";
                    
                    // 1. AllowInsecureGuestAuth (Win10)
                    Registry.SetValue(lmParams, "AllowInsecureGuestAuth", 1, RegistryValueKind.DWord);
                    try { Registry.SetValue(polParams, "AllowInsecureGuestAuth", 1, RegistryValueKind.DWord); } catch { }
                    Log("已启用 AllowInsecureGuestAuth");

                    // 2. Win11 24H2 Fixes
                    Registry.SetValue(lmParams, "RequireSecuritySignature", 0, RegistryValueKind.DWord);
                    Registry.SetValue(lmParams, "EnableSecuritySignature", 0, RegistryValueKind.DWord);
                    Registry.SetValue(lmParams, "EnablePlainTextPassword", 1, RegistryValueKind.DWord);
                    Registry.SetValue(serverParams, "RequireSecuritySignature", 0, RegistryValueKind.DWord);
                    Registry.SetValue(serverParams, "EnableSecuritySignature", 0, RegistryValueKind.DWord);
                    Log("已应用 SMB 签名兼容策略（客户端+服务端）");
                    
                    // 3. User Suggested Fixes (PrintNightmare & RPC)
                    string printPolicy = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows NT\Printers\PointAndPrint";
                    try
                    {
                        // 0x11b Fixes
                        Registry.SetValue(printPolicy, "RestrictDriverInstallationToAdministrators", 0, RegistryValueKind.DWord);
                        Registry.SetValue(printPolicy, "NoWarningNoElevationOnInstall", 0, RegistryValueKind.DWord);
                        Registry.SetValue(printPolicy, "UpdatePromptSettings", 0, RegistryValueKind.DWord);
                        
                        // RpcAuthnLevelPrivacyEnabled (Client & Server side fix for 0x11b/0x709)
                        Registry.SetValue(@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Print", "RpcAuthnLevelPrivacyEnabled", 0, RegistryValueKind.DWord);
                        
                        Log("已应用 0x11b/0x709 修复策略 (RestrictDriver + RPC + Elevation)");
                    }
                    catch { Log("尝试设置打印机安全策略失败 (需要管理员权限)"); }

                    Log("已应用 Win11/24H2 兼容性策略 (SMB签名/明文密码/打印机驱动策略)");
                }
                else if (type == "disable_firewall")
                {
                    if (MessageBox.Show("警告：关闭防火墙会降低电脑安全性。\n仅建议在无法连接时临时测试使用。\n\n确定要关闭所有配置文件的防火墙吗？", "安全警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        RunCommand("netsh", "advfirewall set allprofiles state off");
                        Log("已尝试关闭本机防火墙 (所有配置文件)");
                    }
                }
                else if (type == "unlock_user")
                {
                    string targetUser = ShowInputBox("解除锁定", "请输入要解锁的用户名 (如 Guest, GUSER):", "GUSER");
                    if (!string.IsNullOrEmpty(targetUser))
                    {
                        RunCommand("net", string.Format("user \"{0}\" /active:yes", targetUser));
                        Log("已尝试解锁用户: " + targetUser);
                        MessageBox.Show("已执行解锁命令。\n如果问题依旧，请检查密码策略或手动在 lusrmgr.msc 中解锁。");
                    }
                }
                MessageBox.Show("操作已执行，请查看日志。");
            }
            catch (Exception ex) { Log("错误: " + ex.Message); MessageBox.Show("操作失败: " + ex.Message); }
        }

        private void ShowTextDialog(string title, string content)
        {
            Form form = new Form();
            TextBox txtContent = new TextBox();
            FlowLayoutPanel panelButtons = new FlowLayoutPanel();
            Button btnClose = new Button();
            Button btnCopy = new Button();

            form.Text = title;
            form.Size = new Size(760, 560);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;

            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.Both;
            txtContent.ReadOnly = true;
            txtContent.WordWrap = false;
            txtContent.Dock = DockStyle.Fill;
            txtContent.Font = new Font("Consolas", 9F);
            txtContent.Text = content;

            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Height = 42;
            panelButtons.FlowDirection = FlowDirection.RightToLeft;
            panelButtons.Padding = new Padding(8);

            btnClose.Text = "关闭";
            btnClose.Width = 80;
            btnClose.Click += (s, e) => form.Close();

            btnCopy.Text = "复制结果";
            btnCopy.Width = 90;
            btnCopy.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(content ?? string.Empty);
                    MessageBox.Show("诊断结果已复制到剪贴板。");
                }
                catch
                {
                    MessageBox.Show("复制失败。");
                }
            };

            panelButtons.Controls.Add(btnClose);
            panelButtons.Controls.Add(btnCopy);

            form.Controls.Add(txtContent);
            form.Controls.Add(panelButtons);
            form.ShowDialog();
        }

        private string ShowInputBox(string title, string promptText, string defaultValue)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = defaultValue;

            buttonOk.Text = "确定";
            buttonCancel.Text = "取消";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            return dialogResult == DialogResult.OK ? textBox.Text : "";
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class NetNetwork
    {
        [DllImport("Netapi32.dll", SetLastError = true)]
        static extern int NetShareEnum(
            [MarshalAs(UnmanagedType.LPWStr)] string servername,
            int level,
            out IntPtr bufPtr,
            int prefmaxlen,
            out int entriesread,
            out int totalentries,
            ref int resume_handle 
        );

        [DllImport("Netapi32.dll", SetLastError = true)]
        static extern int NetApiBufferFree(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHARE_INFO_1
        {
            public string shi1_netname;
            public uint shi1_type;
            public string shi1_remark;
        }

        public static List<ShareInfo> GetShares(string server)
        {
            List<ShareInfo> shares = new List<ShareInfo>();
            IntPtr bufPtr = IntPtr.Zero;
            int entriesread = 0;
            int totalentries = 0;
            int resume_handle = 0;
            int nStatus = 0;
            server = server.Trim();
            if (!server.StartsWith("\\\\")) server = "\\\\" + server;

            try
            {
                nStatus = NetShareEnum(server, 1, out bufPtr, -1, out entriesread, out totalentries, ref resume_handle);

                if (nStatus == 0 || nStatus == 234)
                {
                    IntPtr currentPtr = bufPtr;
                    int structSize = Marshal.SizeOf(typeof(SHARE_INFO_1));

                    for (int i = 0; i < entriesread; i++)
                    {
                        SHARE_INFO_1 shi1 = (SHARE_INFO_1)Marshal.PtrToStructure(currentPtr, typeof(SHARE_INFO_1));
                        shares.Add(new ShareInfo { NetName = shi1.shi1_netname, ShareType = shi1.shi1_type, Remark = shi1.shi1_remark });
                        currentPtr = (IntPtr)((long)currentPtr + structSize);
                    }
                }
            }
            catch { }
            finally
            {
                if (bufPtr != IntPtr.Zero) NetApiBufferFree(bufPtr);
            }
            return shares;
        }
    }

    public class ShareInfo
    {
        public string NetName;
        public uint ShareType;
        public string Remark;
    }
}
