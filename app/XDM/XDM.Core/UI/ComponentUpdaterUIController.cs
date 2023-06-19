﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using TraceLog;
using XDM.Core;
using XDM.Core.Downloader;
using XDM.Core.Downloader.Progressive.SingleHttp;
using XDM.Core.Updater;
using XDM.Core.Util;

namespace XDM.Core.UI
{
    public class ComponentUpdaterUIController
    {
        private UpdateMode updateMode;
        private IUpdaterUI updaterUI;
        private IList<UpdateInfo>? updates;
        private int count = 0;
        private SingleSourceHTTPDownloader? http;
        private readonly IList<string> files = new List<string>();
        private long size;
        private long downloaded;

        public ComponentUpdaterUIController(IUpdaterUI updaterUI, UpdateMode updateMode)
        {
            this.updaterUI = updaterUI;
            this.updateMode = updateMode;
            try
            {
                this.updaterUI.Cancelled += (s, e) =>
                {
                    if (this.http != null)
                    {
                        this.http.Stop();
                    }
                    this.http = null;
                };
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "ComponentUpdaterUI");
            }
        }

        public void StartUpdate()
        {
            new Thread(() =>
            {
                try
                {
                    updaterUI.Inderminate = true;
                    if (!UpdateChecker.GetAppUpdates(ApplicationContext.CoreService.AppVerion, out updates, out _, this.updateMode))
                    {
                        updaterUI.DownloadFailed(this, new DownloadFailedEventArgs(ErrorCode.Generic));
                    }
                    if (updates.Count == 0)
                    {
                        updaterUI.ShowNoUpdateMessage();
                        return;
                    }
                    foreach (var update in updates)
                    {
                        size += update.Size;
                    }
                    updaterUI.Inderminate = false;
                    StartUpdate(updates[0]);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, ex.Message);
                    updaterUI.DownloadFailed(this, new DownloadFailedEventArgs(ErrorCode.Generic));
                }
            }).Start();

        }

        private void StartUpdate(UpdateInfo update)
        {
            try
            {
                Log.Debug("Downloading " + update.Name);
                updaterUI.Label = "Downloading " + update.Name;
                http = new SingleSourceHTTPDownloader(new SingleSourceHTTPDownloadInfo
                {
                    Uri = update.Url,
                    Headers = new Dictionary<string, List<string>>
                    {
                        ["User-Agent"] = new List<string>{
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36" }
                    }
                });
                http.SetTargetDirectory(Path.GetTempPath());
                http.Started += updaterUI.DownloadStarted;
                //http.Probed += HandleProbeResult;
                http.Finished += Finished;
                http.ProgressChanged += ProgressChanged;
                http.Cancelled += updaterUI.DownloadCancelled;
                http.Failed += updaterUI.DownloadFailed;
                http.Start();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "StartUpdate");
                updaterUI.DownloadFailed(this, new DownloadFailedEventArgs(ErrorCode.Generic));
            }
        }

        private void ProgressChanged(object? sender, ProgressResultEventArgs e)
        {
            try
            {
                var totalProgress = (int)(((downloaded + e.Downloaded) * 100) / size);
                this.updaterUI.DownloadProgressChanged(this, new ProgressResultEventArgs { Progress = totalProgress });
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "ProgressChanged");
            }
        }

        private void Finished(object? sender, EventArgs e)
        {
            try
            {
                Log.Debug("Finished " + updates[count].Name);
                downloaded += updates[count].Size;
                count++;
                files.Add(http!.TargetFile!);

#if NET5_0_OR_GREATER
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    PlatformHelper.SetExecutable(http!.TargetFile!);
                }
#endif
                if (count == updates.Count)
                {
                    foreach (var file in files)
                    {
                        var name = Path.GetFileName(file);
                        var bakup = Path.Combine(Config.AppDir, name + ".bak");
                        var target = Path.Combine(Config.AppDir, name);
                        File.Move(file, bakup);
                        File.Delete(target);
                        File.Move(bakup, target);
                    }

                    File.WriteAllText(Path.Combine(Config.AppDir, "ytdlp-update.json"),
                        JsonConvert.SerializeObject(new UpdateHistory
                        {
                            //FFmpegUpdateDate = DateTime.Now,
                            YoutubeDLUpdateDate = DateTime.Now
                        }));

                    updaterUI.DownloadFinished(sender, e);
                    return;
                }
                StartUpdate(updates[count]);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Finished");
            }
        }
    }
}

