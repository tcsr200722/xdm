﻿using System;
using System.Collections.Generic;
using System.Text;

namespace XDM.Core.UI
{
    public static class SpeedLimiterUIController
    {
        public static void Run(ISpeedLimiterWindow window)
        {
            window.OkClicked += Window_OkClicked;
            var speedLimitEnabled = Config.Instance.EnableSpeedLimit ? Config.Instance.DefaltDownloadSpeed > 0 : false;
            var defaultSpeedLimit = Config.Instance.DefaltDownloadSpeed;
            window.EnableSpeedLimit = speedLimitEnabled;
            window.SpeedLimit = defaultSpeedLimit;
            window.ShowWindow();
        }

        private static void Window_OkClicked(object? sender, EventArgs e)
        {
            var window = sender as ISpeedLimiterWindow;
            if (window == null) return;
            window.OkClicked -= Window_OkClicked;
            var speedLimitEnabled = window.EnableSpeedLimit;
            var defaultSpeedLimit = window.SpeedLimit;
            lock (Config.Instance)
            {
                Config.Instance.EnableSpeedLimit = speedLimitEnabled ? defaultSpeedLimit > 0 : false;
                Config.Instance.DefaltDownloadSpeed = defaultSpeedLimit > 0 ? defaultSpeedLimit : 0;
                Config.SaveConfig();
            }
            ApplicationContext.BroadcastConfigChange();
        }
    }
}
