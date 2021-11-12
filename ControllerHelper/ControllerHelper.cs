﻿using ControllerService;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace ControllerHelper
{
    public partial class ControllerHelper : Form
    {
        #region imports
        [DllImport("User32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);
        #endregion

        private PipeClient PipeClient;
        private Timer MonitorTimer;
        private IntPtr CurrentProcess;

        private MouseHook m_Hook;

        public ControllerHelper()
        {
            InitializeComponent();
            this.Hide();
        }

        private void ControllerHelper_Load(object sender, EventArgs e)
        {
            // start the pipe client
            PipeClient = new PipeClient("ControllerService", this);
            PipeClient.Start();

            // start mouse hook
            m_Hook = new MouseHook(PipeClient);
            m_Hook.Start();

            // monitors processes
            MonitorTimer = new Timer(1000) { Enabled = true, AutoReset = true };
            MonitorTimer.Elapsed += MonitorHelper;
        }

        private void ControllerHelper_Closed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            PipeClient.Stop();
        }

        public void Kill()
        {
            Application.Exit();
        }

        private void MonitorHelper(object sender, ElapsedEventArgs e)
        {
            IntPtr hWnd = GetForegroundWindow();
            IntPtr processId;

            if (GetWindowThreadProcessId(hWnd, out processId) == 0)
                return;

            if (processId != CurrentProcess)
            {
                PipeMessage message = new PipeMessage { Code = PipeCode.CODE_PROCESS, args = new string[] { processId.ToString() } };
                PipeClient.SendMessage(message);

                CurrentProcess = processId;
            }
        }

        public void SendToast(string title, string content)
        {
            string url = "file:///" + AppDomain.CurrentDomain.BaseDirectory + "Toast.png";
            var uri = new Uri(url);

            new ToastContentBuilder()
                .AddText(title)
                .AddText(content)
                .AddAppLogoOverride(uri, ToastGenericAppLogoCrop.Circle)
                .SetToastDuration(ToastDuration.Short)
                .Show();
        }
    }
}