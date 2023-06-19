﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace XDM.Core.UI
{
    public interface IDownloadCompleteDialog
    {
        public event EventHandler<DownloadCompleteDialogEventArgs>? FileOpenClicked;
        public event EventHandler<DownloadCompleteDialogEventArgs>? FolderOpenClicked;
        public event EventHandler? DontShowAgainClickd;

        public string FileNameText { get; set; }
        public string FolderText { get; set; }
        public void ShowDownloadCompleteDialog();
    }

    public class DownloadCompleteDialogEventArgs : EventArgs
    {
        public string? Path { get; set; }
        public string? FileName { get; set; }
    }
}
