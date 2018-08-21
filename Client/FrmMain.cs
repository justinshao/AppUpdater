using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Justin.Updater.Client
{
    public partial class FrmMain : Form
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

        /// <summary>
        /// 进度条
        /// </summary>
        private FrmProgress _frmProgress = new FrmProgress();
        /// <summary>
        /// 更新检测任务
        /// </summary>
        private UpdateDetector _updateDetector;
        /// <summary>
        /// 主程序状态检测任务
        /// </summary>
        private MainAppStateDetector _mainAppStateDetector;
        /// <summary>
        /// 主程序相对路径
        /// </summary>
        private string _mainAppPath;
        /// <summary>
        /// 系统更新配置
        /// </summary>
        private Config _config;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            notifyIcon.Icon = Properties.Resources.logo1;

            BindUpdateEvents();
            UpdateHelper.Update("正常启动");
        }
        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnBindUpdateEvents();
            
            if(_config.DetectEnabled && _config.KeepUpdaterRunning)
            {
                UpdateHelper.KillRunningApps(_mainAppPath);
            }
        }

        private void BindUpdateEvents()
        {
            UpdateHelper.OnError += OnUpdateError;
            UpdateHelper.OnInvalidUpdateUrl += OnInvalidUpdateUrl;
            UpdateHelper.OnNotSpecifyUpdateUrl += OnNotSpecifyUpdateUrl;
            UpdateHelper.OnStatus += OnUpdateStatus;
            UpdateHelper.OnComplete += OnUpdateComplete;
            UpdateHelper.OnMainAppNotExists += OnMainAppNotExists;
            UpdateHelper.OnMainAppStarted += OnMainAppStarted;
            UpdateHelper.OnBeginUpdate += OnBeginUpdate;
            UpdateHelper.OnMainAppAlreadyRunning += OnMainAppAlreadyRunning;
            UpdateHelper.OnConfirmFileError += OnConfirmFileError;
        }
        private void UnBindUpdateEvents()
        {
            UpdateHelper.OnError -= OnUpdateError;
            UpdateHelper.OnInvalidUpdateUrl -= OnInvalidUpdateUrl;
            UpdateHelper.OnNotSpecifyUpdateUrl -= OnNotSpecifyUpdateUrl;
            UpdateHelper.OnStatus -= OnUpdateStatus;
            UpdateHelper.OnComplete -= OnUpdateComplete;
            UpdateHelper.OnMainAppNotExists -= OnMainAppNotExists;
            UpdateHelper.OnMainAppStarted -= OnMainAppStarted;
            UpdateHelper.OnBeginUpdate -= OnBeginUpdate;
            UpdateHelper.OnMainAppAlreadyRunning -= OnMainAppAlreadyRunning;
            UpdateHelper.OnConfirmFileError -= OnConfirmFileError;
        }

        #region 更新相关事件
        private void OnUpdateError(LocalRunInfo localRunInfo, Exception ex)
        {
            this.Invoke((Action)(() =>
            {
                LogHelper.LogError("更新出错", ex);
                _frmProgress.Hide();

                if (MessageBox.Show(string.Format("{0}\r\n是否继续运行？", ex.Message),
                    "错误", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (string.IsNullOrEmpty(localRunInfo.AppRunCmd))
                    {
                        InputStartApp(localRunInfo);
                    }
                    else
                    {
                        UpdateHelper.RunMainApp(localRunInfo);
                    }
                }
                else
                {
                    Exit(localRunInfo);
                }
            }));
        }
        private void OnInvalidUpdateUrl(LocalRunInfo localRunInfo)
        {
            this.Invoke((Action)(() =>
            {
                MessageBox.Show("更新地址有误");

                InputUpdateUrl(localRunInfo);
            }));
        }
        private void OnNotSpecifyUpdateUrl(LocalRunInfo localRunInfo)
        {
            this.Invoke((Action)(() =>
            {
                InputUpdateUrl(localRunInfo);
            }));
        }
        private void OnUpdateStatus(string msg)
        {
            this.Invoke((Action)(() => { this.lbStatus.Text = msg; }));
        }
        private void OnUpdateComplete(
            LocalRunInfo localRunInfo, 
            IEnumerable<UpdateRunFile> fileDiff, 
            List<string> failedUpdateFiles, 
            List<string> failedDeleteFiles, 
            bool initStart, bool runBat, string msg)
        {
            this.Invoke((Action)(() =>
            {
                _frmProgress.Hide();
                Show();

                OnUpdateStatus("正在保存配置信息...");
                UpdateHelper.SaveLocalRunInfo(localRunInfo);

                if (runBat)
                {
                    OnUpdateStatus("正在做初始化工作...");
                    UpdateHelper.RunBat();
                }

                if (!initStart && fileDiff.Count() > 0)
                { // 由于首次运行的时候更新的文件较多，不写更新日志
                    OnUpdateStatus("正在写更新日志...");
                    LogHelper.WriteUpdateLog(localRunInfo.Ver, fileDiff, failedUpdateFiles, failedDeleteFiles);
                }

                OnUpdateStatus("正在启动主程序...");
                if (string.IsNullOrEmpty(localRunInfo.AppRunCmd))
                {
                    InputStartApp(localRunInfo);
                }
                else
                {
                    UpdateHelper.RunMainApp(localRunInfo, msg);
                }
            }));
        }
        private void OnMainAppNotExists(LocalRunInfo localRunInfo)
        {
            this.Invoke((Action)(() =>
            {
                MessageBox.Show("启动程序不存在");

                InputStartApp(localRunInfo);
            }));
        }
        private void OnMainAppStarted(LocalRunInfo localRunInfo, string msg)
        {
            this.Invoke((Action)(() =>
            {
                if(string.IsNullOrEmpty(localRunInfo.ClientId))
                {
                    localRunInfo.ClientId = InputClientId();
                }

                UpdateHelper.SaveLocalRunInfo(localRunInfo);

                // 以下是更新检测相关逻辑
                _mainAppPath = localRunInfo.AppRunCmd.Split(new char[] { ' ' },
                            StringSplitOptions.RemoveEmptyEntries)[0];
                var urlInfo = UpdateUrlInfo.Parse(localRunInfo.UpdateUrl);
                this._config = UpdateHelper.GetSystemConfig(urlInfo.Host, urlInfo.SystemId);

                if (UpdateHelper.IsThisAppRunning() || !this._config.DetectEnabled)
                {// 保证更新程序只有一个在后台运行
                    Exit(localRunInfo);
                }
                else
                {
                    StartUpdateDetect(localRunInfo, this._config);
                    StartMainAppStateDetect(_mainAppPath, localRunInfo, this._config);
                }

                var appName = Path.GetFileName(_mainAppPath);
                notifyIcon.Text = this.Text = "更新检测-" + appName;

                LogHelper.LogInfoToServer($"程序启动成功（{msg}）", localRunInfo);
            }));
        }
        private void OnBeginUpdate(LocalRunInfo obj)
        {
            this.Invoke((Action)(() =>
            {
                Hide();
                _frmProgress.Show();
            }));
        }
        private bool OnConfirmFileError(string msg, Exception e)
        {
            return MessageBox.Show(string.Format("{0} 错误信息：\r\n\t{1}\r\n是否继续？", msg, e.Message), "错误", MessageBoxButtons.YesNo)
                == DialogResult.Yes;
        }
        private void OnMainAppAlreadyRunning(LocalRunInfo localRunInfo)
        {
            this.Invoke((Action)(() =>
            {
                MessageBox.Show("程序正在运行，请退出后再更新。", "提示", MessageBoxButtons.OK);

                Exit(localRunInfo);
            }));
        } 
        #endregion

        //private void InputAndStartApp(LocalRunInfo localRunInfo)
        //{
        //    if(string.IsNullOrEmpty(localRunInfo.AppRunCmd))
        //    {
        //        var appRunCmd = localRunInfo.AppRunCmd;
        //        if (InputStartApp(ref localRunInfo.AppRunCmd))
        //        {

        //        }
        //        else
        //        {
        //            Exit(localRunInfo);
        //        }
        //    }
        //}

        //private bool InputStartApp(ref string appRunCmd)
        //{
        //    FrmInput frm = new FrmInput("输入启动程序：", appRunCmd);
        //    if (frm.ShowDialog() == DialogResult.OK)
        //    {
        //        appRunCmd = frm.Value;
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        private void InputStartApp(LocalRunInfo localRunInfo)
        {
            FrmInput frm = new FrmInput("输入启动程序：", localRunInfo.AppRunCmd);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                localRunInfo.AppRunCmd = frm.Value;
                UpdateHelper.RunMainApp(localRunInfo);
            }
            else
            {
                Exit(localRunInfo);
            }
        }
        private string InputClientId()
        {
            FrmInput frm = new FrmInput("输入客户名称：", null, true);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                return frm.Value;
            }

            return null;
        }
        private void InputUpdateUrl(LocalRunInfo localRunInfo)
        {
            FrmInput frm = new FrmInput("输入更新地址：", localRunInfo.UpdateUrl);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                localRunInfo.UpdateUrl = frm.Value;
                UpdateHelper.SaveLocalRunInfo(localRunInfo);

                UpdateHelper.Update("正常启动");
            }
            else
            {
                Exit(localRunInfo);
            }
        }
        internal static void Exit(LocalRunInfo localRunInfo)
        {
            Environment.Exit(0);
        }

        #region 后台任务（更新检测，主程序状态检测）相关事件
        /// <summary>
        /// 检测到程序更新事件
        /// </summary>
        /// <param name="detector"></param>
        private void OnUpdateNotification(UpdateDetector detector)
        {
            this.Invoke((Action)(() =>
            {
                ShowUpdateNotification();
            }));
        }
        /// <summary>
        /// 更新检测任务启动完成事件
        /// </summary>
        /// <param name="detector"></param>
        private void OnUpdateDetectorStart(BackgroundTask detector)
        {
            this.Invoke((Action)(() =>
            {
                ShowAsNotification();
                UpdateNotificationMenu();
            }));
        }
        /// <summary>
        /// 更新检测任务停止事件
        /// </summary>
        /// <param name="detector"></param>
        private void OnUpdateDetectorStop(BackgroundTask detector)
        {
            this.Invoke((Action)(() =>
            {
                UpdateNotificationMenu();
            }));
        }
        /// <summary>
        /// 所有主程序关闭事件
        /// </summary>
        /// <param name="detector"></param>
        private void OnMainAppsClose(MainAppStateDetector detector, LocalRunInfo localRunInfo)
        {
            this.Invoke((Action)(() =>
            {
                if(!this._config.KeepUpdaterRunning)
                {
                    Exit(null);
                }
                else if(this._config.KeepAppRunning)
                {
                    if (!UpdateHelper.IsAppRunning(_mainAppPath))
                    {
                        UpdateHelper.RunMainApp(localRunInfo, "由更新程序启动");
                    }
                }
            }));
        }
        /// <summary>
        /// 运行服务端命令
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="localRunInfo"></param>
        private void OnServerCommandRequest(ClientCommand cmd, LocalRunInfo localRunInfo)
        {
            switch (cmd.Type)
            {
                case ClientCommandType.Start:
                    if(!UpdateHelper.IsAppRunning(_mainAppPath))
                    {
                        UpdateHelper.RunMainApp(localRunInfo, "由服务端启动");
                    }
                    break;
                case ClientCommandType.Stop:
                    if (UpdateHelper.IsAppRunning(_mainAppPath))
                    {
                        UpdateHelper.KillRunningApps(_mainAppPath);
                    }
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 更新请求事件
        /// </summary>
        private void OnUpdateRequest()
        {
            this.Invoke((Action)(() =>
            {
                if (UpdateHelper.IsAppRunning(_mainAppPath))
                {
                    if (MessageBox.Show("是否退出当前运行的所有程序？", "询问",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        UpdateApp("手动更新");
                    }
                    else
                    {
                        // 选择不退出主程序（放弃更新）
                        IgnoreUpdate();
                    }
                }
                else
                {
                    UpdateApp("手动更新");
                }
            }));
        }

        private void UpdateApp(string msg)
        {
            var appRunning = UpdateHelper.IsAppRunning(_mainAppPath);

            if (appRunning)
            {
                UpdateHelper.KillRunningApps(_mainAppPath);

                StopUpdateDetect();
                StopMainAppActiveDetect();
            }

            ShowAsNormal(appRunning ? "准备更新，正在关闭程序..." : null);
            UpdateHelper.Update(msg, appRunning ? 5000 : 0);
        }

        private void IgnoreUpdate()
        {
            this._updateDetector.DelayPrompt(_config.PromptInterval * 60 * 1000);
        }

        /// <summary>
        /// 更新忽略事件
        /// </summary>
        private void OnUpdateIgnore()
        {
            this.Invoke((Action)(() =>
            {
                this.IgnoreUpdate();
            }));
        }
        #endregion

        /// <summary>
        /// 显示更新通知
        /// </summary>
        private void ShowUpdateNotification()
        {
            //FrmUpdateNotify _frmUpdateNotify = new FrmUpdateNotify();
            //_frmUpdateNotify.UpdateRequest += OnUpdateRequest;
            //_frmUpdateNotify.Show();
            if(!_config.ForceUpdate)
            {
                FrmUpdateNotify.Show(OnUpdateRequest, OnUpdateIgnore);
            }
            else
            {
                UpdateApp("自动更新");
            }
        }
        /// <summary>
        /// 当前窗体正常显示
        /// </summary>
        private void ShowAsNormal(string msg)
        {
            if(!string.IsNullOrEmpty(msg))
            {
                lbStatus.Text = msg;
            }

            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon.Visible = false;
        }
        /// <summary>
        /// 当前窗体显示到状态栏
        /// </summary>
        private void ShowAsNotification()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            notifyIcon.Visible = true;
        }
        /// <summary>
        /// 停止主程序状态检测
        /// </summary>
        private void StopMainAppActiveDetect()
        {
            _mainAppStateDetector.Stop();
        }
        private void StopUpdateDetect()
        {
            _updateDetector.Stop();
        }
        /// <summary>
        /// 启动更新检测
        /// </summary>
        /// <param name="localRunInfo"></param>
        /// <param name="config"></param>
        private void StartUpdateDetect(LocalRunInfo localRunInfo, Config config)
        {
            if(_updateDetector!= null)
            {
                _updateDetector.Stop();
                _updateDetector.OnStart -= OnUpdateDetectorStart;
                _updateDetector.OnStop -= OnUpdateDetectorStop;
                _updateDetector.OnNotifyUpdate -= OnUpdateNotification;
            }

            UpdateUrlInfo info = UpdateUrlInfo.Parse(localRunInfo.UpdateUrl);
            _updateDetector = new UpdateDetector(config.DetectInterval * 1000, info);
            _updateDetector.OnStart += OnUpdateDetectorStart;
            _updateDetector.OnStop += OnUpdateDetectorStop;
            _updateDetector.OnNotifyUpdate += OnUpdateNotification;
            _updateDetector.Start();
        }
        /// <summary>
        /// 启动主程序状态检测
        /// </summary>
        /// <param name="mainAppPath"></param>
        private void StartMainAppStateDetect(string mainAppPath, LocalRunInfo localRunInfo, Config config)
        {
            if (_mainAppStateDetector != null)
            {
                _mainAppStateDetector.Stop();
                _mainAppStateDetector.OnMainAppsClose -= OnMainAppsClose;
                _mainAppStateDetector.OnCommandRequest -= OnServerCommandRequest;
            }

            _mainAppStateDetector = new MainAppStateDetector(mainAppPath, localRunInfo, config);
            _mainAppStateDetector.OnMainAppsClose += OnMainAppsClose;
            _mainAppStateDetector.OnCommandRequest += OnServerCommandRequest;
            _mainAppStateDetector.Start();
        }

        /// <summary>
        /// 更新状态栏右键菜单
        /// </summary>
        private void UpdateNotificationMenu()
        {
            //MenuItem[] mnuItms = new MenuItem[3];

            //mnuItms[0] = new MenuItem();
            //mnuItms[0].Text = _updateDetector.IsStarted ? "停止检测" : "开启检测";
            //mnuItms[0].Click += _updateDetector.IsStarted ? (EventHandler)((obj, e) => _updateDetector.Stop()) : ((obj, e) => _updateDetector.Start());

            //mnuItms[1] = new MenuItem("-");

            //mnuItms[2] = new MenuItem();
            //mnuItms[2].Text = "退出";
            //mnuItms[2].Click += (obj, e) => Exit(null);
            //mnuItms[2].DefaultItem = true;
            
            //notifyIcon.ContextMenu = new ContextMenu(mnuItms);
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x0112, 0xF012, 0);
        }
        
    }
}
