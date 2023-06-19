﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XDM.Core.UI;

namespace XDM.Core.UI
{
    public interface INewVideoDownloadDialog : IFileSelectable
    {
        void DisposeWindow();
        void Invoke(Action callback);
        void ShowWindow();
        void ShowMessageBox(string text);
        string FileSize { get; set; }
        public AuthenticationInfo? Authentication { get; set; }
        public ProxyInfo? Proxy { get; set; }
        public int SpeedLimit { get; set; }
        public bool EnableSpeedLimit { get; set; }
        public bool ShowMp3Checkbox { get; set; }
        public bool IsMp3CheckboxChecked { get; set; }

        event EventHandler DownloadClicked, DestroyEvent, QueueSchedulerClicked, Mp3CheckChanged;
        event EventHandler<DownloadLaterEventArgs> DownloadLaterClicked;
    }
}
