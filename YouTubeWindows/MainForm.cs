using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        private CoreWebView2Environment coreWebView2Environment;
        public WebView2 splashScreenWebView;
        public WebView2 screenWebView;
        public Panel splashScreenWebViewPanel = new Panel();
        public Panel screenWebViewPanel = new Panel();
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

        public MainForm(string[] args)
        {
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
            var userDataDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/User Data/";
            var ua = "GoogleTV/10.0 (Windows NT 10.0; Cobalt; Wired) html5_enable_androidtv_cobalt_widevine html5_enable_cobalt_experimental_vp9_decoder";
            //var ua = "Mozilla/5.0 (WINDOWS 10.0), GAME_XboxSeriesX/10.0.18363.7196 (Microsoft, Xbox Series X, Wired)";
            //var ua = "Mozilla/5.0 (SMART-TV; Linux; Tizen 5.5) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/3.0 Chrome/69.0.3497.106 TV Safari/537.36";
            //var ua = "Mozilla/5.0 (PlayStation 4 7.51) AppleWebKit/605.1.15 (KHTML, like Gecko)";
            //var ua = "Mozilla/5.0 (Nintendo Switch; WebApplet) AppleWebKit/606.4 (KHTML, like Gecko) NF/6.0.1.16.10 NintendoBrowser/5.1.0.20923";
            //var options = new CoreWebView2EnvironmentOptions("--disable-web-security --enable-features=msPlayReadyWin10 --user-agent=\"Mozilla/5.0 (Windows NT 10.0; Win64; x64; Xbox; Xbox Series X) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.82 Safari/537.36 Edge/20.02\"");
            //var options = new CoreWebView2EnvironmentOptions("--enable-features=msMediaFoundationClearPlaybackWin10,msPlayReadyWin10 --user-agent=\"Mozilla/5.0 (Windows NT 10.0; Win64; x64; Xbox; Xbox Series X) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.82 Safari/537.36 Edge/20.02\"");
            var options = new CoreWebView2EnvironmentOptions(startupArgs + "--allow-failed-policy-fetch-for-test --allow-running-insecure-content --disable-web-security --user-agent=\"" + ua + "\""); // Mozilla/5.0 (WINDOWS 10.0) Cobalt/19.lts.4.196747-gold (unlike Gecko) v8/6.5.254.43 gles Starboard/10, GAME_XboxOne/10.0.18363.7196 (Microsoft, XboxOne X, Wired)
            coreWebView2Environment = CoreWebView2Environment.CreateAsync(null, userDataDir, options).Result;

            splashScreenWebView = new WebView2();
            screenWebView = new WebView2();

            screenWebView.Enabled = false;

            screenWebViewPanel.Visible = false;
            splashScreenWebViewPanel.Visible = false;

            screenWebViewPanel.Controls.Add(screenWebView);
            splashScreenWebViewPanel.Controls.Add(splashScreenWebView);

            InitializeSplashScreenAsync();
        }

        private async Task NativeBridgeRegister(WebView2 webView2)
        {
            webView2.CoreWebView2.AddHostObjectToScript("NativeBridge", new Bridge(this));
            // 简化 NativeBridge
            await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.NativeBridge = window?.chrome?.webview?.hostObjects?.NativeBridge;");
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
            screenWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            screenWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            screenWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            screenWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#if DEBUG
            screenWebView.CoreWebView2.OpenDevToolsWindow();
#endif
            ReloadApp();
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

                var actionw = new Action(() =>
                {
                    ctxMainForm.splashScreenWebView.ExecuteScriptAsync("document.getElementById('background').style.opacity = 0;");
                    ctxMainForm.screenWebViewPanel.Visible = true;
                });

                if (ctxMainForm.InvokeRequired)
                {
                    ctxMainForm.Invoke(actionw);
                }
                else
                {
                    actionw();
                }

                Thread.Sleep(500);

                var action3 = new Action(() =>
                {
                    ctxMainForm.splashScreenWebViewPanel.Visible = false;
                    ctxMainForm.splashScreenWebView.CoreWebView2.Navigate("about:blank");
                    ctxMainForm.splashScreenWebView.ExecuteScriptAsync("document.body.style.backgroundColor = '#181818'");
                    ctxMainForm.screenWebView.Enabled = true;
                    ctxMainForm.screenWebView.ExecuteScriptAsync("document.body.style.opacity = 1;");
                    ctxMainForm.screenWebView.Focus();
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
