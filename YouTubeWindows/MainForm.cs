using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YouTubeWindows
{
    public partial class MainForm : Form
    {
        private string startupArgs = "";
        private string runtimeVersion = "Unknown";
        private string runtimePath = null;
        private CoreWebView2Environment coreWebView2Environment;
        public WebView2 splashScreenWebView;
        public WebView2 screenWebView;
        public Panel splashScreenWebViewPanel = new Panel();
        public Panel screenWebViewPanel = new Panel();
        private bool foundRuntime = false;
        private bool allowEndscreen = false;
        private bool _fullscreen = false;

        public bool fullscreen
        {
            get
            {
                return _fullscreen;
            }
            set
            {
                _fullscreen = value;
                if (_fullscreen)
                {
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = FormWindowState.Normal;
                }
            }
        }

        private void tryRuntime(string path)
        {
            try
            {
                var availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString(path);
                if (availableBrowserVersionString == null)
                {
                    throw new Exception("缺少 WebView2 Runtime");
                }
                else
                {
                    foundRuntime = true;
                    runtimePath = path;
                    runtimeVersion = availableBrowserVersionString;
                }
            }
            catch { }
        }

        public MainForm(string[] args)
        {
            string[] tryRuntimePaths = {
                // Fixed Version 固定版本
                AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Runtime",
                // Evergreen 长青版
                null
            };
            foreach (string tryRuntimePath in tryRuntimePaths)
            {
                if (foundRuntime == false)
                {
                    tryRuntime(tryRuntimePath);
                }
            }

            if (foundRuntime)
            {
#if DEBUG
                var availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString(runtimePath);
                MessageBox.Show("当前 WebView2 Runtime:\n" + (runtimePath == null ? "Evergreen Runtime" : "Fixed Version Runtime: " + runtimePath) + "\nVersion: " + availableBrowserVersionString, "YouTube");
#endif
            }
            else
            {
                MessageBox.Show("缺少 WebView2 Runtime，无法运行。\n可以通过以下任意一种方式安装：\n\n1. 安装任意非稳定通道 Microsoft Edge (Chromium) 浏览器。\n2. 安装 WebView2 Runtime Evergreen 版本。\n3. 将 WebView2 Runtime Fixed Version 版本放入 YouTube For Windows 的 Runtime 文件夹下。", "YouTube");
                Close();
                Application.Exit();
                return;
            }

            InitializeComponent();

            this.Icon = Resource.icon;

            screenWebViewPanel.Dock = DockStyle.Fill;
            screenWebViewPanel.BackColor = Color.Transparent;
            splashScreenWebViewPanel.Dock = DockStyle.Fill;
            splashScreenWebViewPanel.BackColor = Color.Transparent;

            Controls.Add(splashScreenWebViewPanel); // 放置闪屏承载层（顶部）
            Controls.Add(screenWebViewPanel); // 放置App承载层（底部）

            StringBuilder startupArgsBuilder = new StringBuilder();

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "--allow-endscreen":
                        {
                            allowEndscreen = true;
                        }
                        break;
                    default:
                        {
                            startupArgsBuilder.Append(arg + " ");
                        }
                        break;
                }

            }

            startupArgs = startupArgsBuilder.ToString();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var userDataDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "User Data";
            var ua = "GoogleTV/CloudMoe-Version (DISKTOP; Windows NT " + Environment.OSVersion.Version.ToString() + "; Wired) Cobalt/" + runtimeVersion + " (unlike Gecko) html5_enable_androidtv_cobalt_widevine html5_enable_cobalt_experimental_vp9_decoder";
            // var ua = "GoogleTV/10.0 (Windows NT 10.0; Cobalt; Wired) html5_enable_androidtv_cobalt_widevine html5_enable_cobalt_experimental_vp9_decoder";
            //var ua = "Mozilla/5.0 (WINDOWS 10.0), GAME_XboxSeriesX/10.0.18363.7196 (Microsoft, Xbox Series X, Wired)";
            //var ua = "Mozilla/5.0 (SMART-TV; Linux; Tizen 5.5) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/3.0 Chrome/69.0.3497.106 TV Safari/537.36";
            //var ua = "Mozilla/5.0 (PlayStation 4 7.51) AppleWebKit/605.1.15 (KHTML, like Gecko)";
            //var ua = "Mozilla/5.0 (Nintendo Switch; WebApplet) AppleWebKit/606.4 (KHTML, like Gecko) NF/6.0.1.16.10 NintendoBrowser/5.1.0.20923";
            //var options = new CoreWebView2EnvironmentOptions("--disable-web-security --enable-features=msPlayReadyWin10 --user-agent=\"Mozilla/5.0 (Windows NT 10.0; Win64; x64; Xbox; Xbox Series X) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.82 Safari/537.36 Edge/20.02\"");
            //var options = new CoreWebView2EnvironmentOptions("--enable-features=msMediaFoundationClearPlaybackWin10,msPlayReadyWin10 --user-agent=\"Mozilla/5.0 (Windows NT 10.0; Win64; x64; Xbox; Xbox Series X) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.82 Safari/537.36 Edge/20.02\"");
            var options = new CoreWebView2EnvironmentOptions(startupArgs + "--allow-failed-policy-fetch-for-test --allow-running-insecure-content --disable-web-security --user-agent=\"" + ua + "\""); // Mozilla/5.0 (WINDOWS 10.0) Cobalt/19.lts.4.196747-gold (unlike Gecko) v8/6.5.254.43 gles Starboard/10, GAME_XboxOne/10.0.18363.7196 (Microsoft, XboxOne X, Wired)
            coreWebView2Environment = CoreWebView2Environment.CreateAsync(runtimePath, userDataDir, options).Result;

            splashScreenWebView = new WebView2();
            screenWebView = new WebView2();

            screenWebView.Enabled = false;

            screenWebViewPanel.Visible = false;
            splashScreenWebViewPanel.Visible = false;

            screenWebViewPanel.Controls.Add(screenWebView);
            splashScreenWebViewPanel.Controls.Add(splashScreenWebView);

            InitializeSplashScreenAsync();
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private async Task NativeBridgeRegister(WebView2 webView2)
        {
            webView2.CoreWebView2.AddHostObjectToScript("NativeBridge", new Bridge(this));
            // 简化 NativeBridge
            await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.NativeBridge = window?.chrome?.webview?.hostObjects?.NativeBridge;");
            // 替换 Close
            await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.close = window?.chrome?.webview?.hostObjects?.NativeBridge?.Close;");
            // 全屏和重载监听
            await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.addEventListener('keydown', (event) => { if (event.keyCode === 122) { event.returnValue = false; NativeBridge.ToggleFullscreen(); } if(event.keyCode == 116) { event.returnValue = false; NativeBridge.ReloadApp(); } }, true);");
        }

        private async void InitializeSplashScreenAsync()
        {
            splashScreenWebView.Dock = DockStyle.Fill;
            await splashScreenWebView.EnsureCoreWebView2Async(coreWebView2Environment);
            await splashScreenWebView.ExecuteScriptAsync("document.body.style.backgroundColor = '#181818'");
            await NativeBridgeRegister(splashScreenWebView);
            splashScreenWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            splashScreenWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            splashScreenWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            splashScreenWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            InitializeMainAppAsync();
        }

        private async void InitializeMainAppAsync()
        {
            await screenWebView.EnsureCoreWebView2Async(coreWebView2Environment);
            await NativeBridgeRegister(screenWebView);
            screenWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            screenWebView.CoreWebView2.AddWebResourceRequestedFilter("https://www.gstatic.com/ytlr/txt/licenses_googletv.txt", CoreWebView2WebResourceContext.All);
            screenWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            screenWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            screenWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            screenWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            screenWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#if DEBUG
            screenWebView.CoreWebView2.OpenDevToolsWindow();
#endif
            ReloadApp();
        }

        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            Console.WriteLine(e.Request.Uri);
            if (e.Request.Uri == "https://www.gstatic.com/ytlr/txt/licenses_googletv.txt")
            {
                var stream = GenerateStreamFromString(Resource.Staff.Replace("\n", "\n\u200B"));
                e.Response = coreWebView2Environment.CreateWebResourceResponse(stream, 200, "OK", "Content-Type: text/html");
                new Thread(() =>
                {
                    Thread.Sleep(3000); // 流资源 3000ms 后释放
                    var action = new Action(() =>
                    {
                        stream.Close();
                    });

                    if (InvokeRequired)
                    {
                        Invoke(action);
                    }
                    else
                    {
                        action();
                    }
                }).Start();
            }
        }

        public void ReloadApp()
        {
            screenWebView.Enabled = false;
            splashScreenWebView.CoreWebView2.NavigateToString(Resource.youtube_splash_screen);
            screenWebView.CoreWebView2.Navigate("https://www.youtube.com/tv");
            screenWebViewPanel.Visible = false;
            splashScreenWebViewPanel.Visible = true;
        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (screenWebView.Source.ToString().StartsWith("https://www.youtube.com"))
            {
                screenWebView.Dock = DockStyle.None;
                screenWebView.Width = 15360;
                screenWebView.Height = 8640;
                // 破解分辨率（先伪装8K屏幕，然后还原）
                screenWebView.ExecuteScriptAsync("{ let YTInitCheckerId = setInterval(() => { if(yt?.config_?.WEB_PLAYER_CONTEXT_CONFIGS?.WEB_PLAYER_CONTEXT_CONFIG_ID_LIVING_ROOM_WATCH?.videoContainerOverride || ytcfg?.data_?.WEB_PLAYER_CONTEXT_CONFIGS?.WEB_PLAYER_CONTEXT_CONFIG_ID_LIVING_ROOM_WATCH?.videoContainerOverride) { clearInterval(YTInitCheckerId); NativeBridge.ActiveScreen(); } }, 1000); }");
                // 后台播放
                screenWebView.ExecuteScriptAsync("for (event_name of ['visibilitychange', 'webkitvisibilitychange', 'blur']) { window.addEventListener(event_name, function(event) { event.stopImmediatePropagation(); }, true); }");
                // 注入动画
                screenWebView.ExecuteScriptAsync("document.body.style.opacity = 0; document.body.style.transition = 'opacity 333ms';");
                // 隐藏片尾视频内链接
                if (!allowEndscreen)
                {
                    screenWebView.ExecuteScriptAsync("{ const style = document.createElement('style'); style.innerHTML = 'ytlr-endscreen-renderer { display: none !important; }'; document.head.appendChild(style); }");
                }
            }
            else
            {
                screenWebView.Dock = DockStyle.Fill;
            }
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            screenWebView.Focus();
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class BridgeAnotherClass
    {
        // Sample property.
        public string Prop { get; set; } = "Example";
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class Bridge
    {
        private MainForm ctxMainForm;

        public Bridge(MainForm mainForm)
        {
            ctxMainForm = mainForm;
        }

        public string Func(string param)
        {
            Console.WriteLine(param);
            return "Example: " + param;
        }

        public void Close()
        {
            ctxMainForm.Close();
        }

        public void ReloadApp()
        {
            ctxMainForm.ReloadApp();
        }

        public void ToggleFullscreen()
        {
            ctxMainForm.fullscreen = !ctxMainForm.fullscreen;
        }

        public void ActiveScreen()
        {
            new Thread(() =>
            {
                var action1 = new Action(() =>
                {
                    ctxMainForm.screenWebView.Dock = DockStyle.Fill;
                });

                if (ctxMainForm.InvokeRequired)
                {
                    ctxMainForm.Invoke(action1);
                }
                else
                {
                    action1();
                }

                Thread.Sleep(3000);

                var action2 = new Action(() =>
                {
                    ctxMainForm.splashScreenWebView.ExecuteScriptAsync("document.getElementById('background').style.opacity = 0;");
                    ctxMainForm.screenWebViewPanel.Visible = true;
                });

                if (ctxMainForm.InvokeRequired)
                {
                    ctxMainForm.Invoke(action2);
                }
                else
                {
                    action2();
                }

                Thread.Sleep(500);

                var action3 = new Action(() =>
                {
                    ctxMainForm.splashScreenWebViewPanel.Visible = false;
                    ctxMainForm.splashScreenWebView.CoreWebView2.Navigate("about:blank");
                    ctxMainForm.splashScreenWebView.ExecuteScriptAsync("document.body.style.backgroundColor = '#181818'");
                    ctxMainForm.screenWebView.Enabled = true;
                    ctxMainForm.screenWebView.ExecuteScriptAsync("document.body.style.opacity = 1;");
                    if (ctxMainForm.Focused)
                    {
                        ctxMainForm.screenWebView.Focus();
                    }
                });

                if (ctxMainForm.InvokeRequired)
                {
                    ctxMainForm.Invoke(action3);
                }
                else
                {
                    action3();
                }
            }).Start();
        }

        public BridgeAnotherClass AnotherObject { get; set; } = new BridgeAnotherClass();

        // Sample indexed property.
        [System.Runtime.CompilerServices.IndexerName("Items")]
        public string this[int index]
        {
            get { return m_dictionary[index]; }
            set { m_dictionary[index] = value; }
        }
        private Dictionary<int, string> m_dictionary = new Dictionary<int, string>();
    }
}
