using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YouTubeWindows
{
    public struct WebView2RuntimeInfo
    {
        public string Version;
        public string Path;
    }

    public partial class MainForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        
        public string webview2StartupArgs = "";
        WebView2RuntimeInfo? webview2RuntimeInfo = null;
        private CoreWebView2Environment coreWebView2Environment;
        public WebView2 splashScreenWebView;
        public WebView2 screenWebView;
        public Panel splashScreenWebViewPanel = new Panel();
        public Panel screenWebViewPanel = new Panel();
        private int titleHeight
        {
            get
            {
                Rectangle screenRectangle = this.RectangleToScreen(this.ClientRectangle);
                return screenRectangle.Top - this.Top;
            }
        }
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

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (IsWindows10OrGreater(17763))
            {
                var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (IsWindows10OrGreater(18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, (int)attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
        }

        private WebView2RuntimeInfo? ReadRuntime(string path)
        {
            try
            {
                var availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString(path);
                if (availableBrowserVersionString != null)
                {
                    WebView2RuntimeInfo info = new WebView2RuntimeInfo()
                    {
                        Version = availableBrowserVersionString,
                        Path = path
                    };
                    return info;
                }
            }
            catch { }
            return null;
        }

        public MainForm(string[] args)
        {
            string[] runtimePaths = {
                // Fixed Version 固定版本
                AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "runtime",
                // Evergreen 长青版
                null
            };

            foreach (string runtimePath in runtimePaths)
            {
                webview2RuntimeInfo = ReadRuntime(runtimePath);
                if (webview2RuntimeInfo != null)
                {
                    break;
                }
            }

            if (webview2RuntimeInfo != null)
            {
#if DEBUG
                var availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString();
                MessageBox.Show("当前 WebView2 Runtime:\n" + (webview2RuntimeInfo.Value.Path == null ? "Evergreen Runtime" : "Fixed Version Runtime: " + webview2RuntimeInfo.Value.Path) + "\nVersion: " + availableBrowserVersionString, "YouTube");
#endif
            }
            else
            {
                MessageBox.Show("缺少 WebView2 Runtime，无法运行。\n可以通过以下任意一种方式安装：\n\n1. 安装任意非稳定通道 Microsoft Edge (Chromium) 浏览器。\n2. 安装 WebView2 Runtime Evergreen 版本。\n3. 将 WebView2 Runtime Fixed Version 版本放入 YouTube For Windows 的 runtime 文件夹下。", "YouTube");
                Close();
                Application.Exit();
                return;
            }

            StringBuilder webview2StartupArgsBuilder = new StringBuilder();

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
                            webview2StartupArgsBuilder.Append(arg + " ");
                        }
                        break;
                }

            }

            webview2StartupArgs = webview2StartupArgsBuilder.ToString();

            InitializeComponent();
            
            UseImmersiveDarkMode(this.Handle, true);

            this.Icon = Resource.icon;

            screenWebViewPanel.Dock = DockStyle.Fill;
            screenWebViewPanel.BackColor = Color.Transparent;
            splashScreenWebViewPanel.Dock = DockStyle.Fill;
            splashScreenWebViewPanel.BackColor = Color.Transparent;

            Controls.Add(splashScreenWebViewPanel); // 放置闪屏承载层（顶部）
            Controls.Add(screenWebViewPanel); // 放置App承载层（底部）
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var userDataDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "User Data";
            var ua = "GoogleTV/CloudMoe-Version (DISKTOP; Windows NT " + Environment.OSVersion.Version.ToString() + "; Wired) Cobalt/" + webview2RuntimeInfo.Value.Version + " (unlike Gecko) html5_enable_androidtv_cobalt_widevine html5_enable_cobalt_experimental_vp9_decoder html5_live_head_playable";
            var options = new CoreWebView2EnvironmentOptions(webview2StartupArgs + "--allow-failed-policy-fetch-for-test --allow-running-insecure-content --disable-web-security --user-agent=\"" + ua + "\""); // Mozilla/5.0 (WINDOWS 10.0) Cobalt/19.lts.4.196747-gold (unlike Gecko) v8/6.5.254.43 gles Starboard/10, GAME_XboxOne/10.0.18363.7196 (Microsoft, XboxOne X, Wired)
            coreWebView2Environment = CoreWebView2Environment.CreateAsync(webview2RuntimeInfo.Value.Path, userDataDir, options).Result;

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
            _ = splashScreenWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setEmitTouchEventsForMouse", "{\"enabled\": true}");
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
            _ = screenWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setEmitTouchEventsForMouse", "{\"enabled\": true}");
            _ = screenWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride", "{\"width\": 0, \"height\": 0, \"deviceScaleFactor\": 10 ,\"screenWidth\": 7680 ,\"screenHeight\": 4320, \"mobile\": false, \"dontSetVisibleSize\": false}");
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
                var stream = GenerateStreamFromString(
                    Resource.Staff
                    .Replace("\n", "\n\u200B")
                    .Replace("<--%WEBVIEW_VERSION%-->", webview2RuntimeInfo.Value.Version)
                    .Replace("<--%PROGRAM_VERSION%-->", Version.Parse(Application.ProductVersion).ToString(3)));
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
                // 破解分辨率（先伪装8K屏幕，然后还原）
                //screenWebView.Dock = DockStyle.None;
                //screenWebView.Width = 15360;
                //screenWebView.Height = 8640;
                //screenWebView.ExecuteScriptAsync("{ let YTInitCheckerId = setInterval(() => { if(!!document.getElementsByTagName(\"video\")[0]) { clearInterval(YTInitCheckerId); NativeBridge.ActiveScreen(); } }, 1000); }");
                // 新版用 DeviceMetricsOverride 替代
                screenWebView.ExecuteScriptAsync("{ setTimeout(() => { NativeBridge.ActiveScreen(); }, 0); }");
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

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Maximized)
            {
                var aspect = (double)16 / 9;
                var height = this.ClientSize.Width / aspect;
                var width = height * aspect;
                this.ClientSize = new Size((int)width, (int)height);
            }
        }

        private void MainForm_ResizeBegin(object sender, EventArgs e)
        {
            this.SuspendLayout();
        }

        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            this.ResumeLayout();
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
