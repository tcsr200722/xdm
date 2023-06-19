﻿using XDM.Core.MediaParser.Hls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using TraceLog;
using XDM.Core.Clients.Http;
using XDM.Core;
using XDM.Core.MediaProcessor;
using XDM.Core.Util;
using XDM.Core.IO;

namespace XDM.Core.Downloader.Adaptive.Hls
{
    public class MultiSourceHLSDownloader : MultiSourceDownloaderBase
    {
        private CountdownLatch? initLatch;
        public override string Type => "Hls";
        public override Uri PrimaryUrl
        {
            get
            {
                var state = _state as MultiSourceHLSDownloadState;
                if (state == null)
                {
                    return null;
                }
                if (state.NonMuxedVideoPlaylistUrl != null) return state.NonMuxedVideoPlaylistUrl;
                if (state.MuxedPlaylistUrl != null) return state.MuxedPlaylistUrl;
                return null;
            }
        }
        public MultiSourceHLSDownloader(MultiSourceHLSDownloadInfo info, IHttpClient http = null,
            BaseMediaProcessor mediaProcessor = null,
            AuthenticationInfo? authentication = null, ProxyInfo? proxy = null) : base(info, http, mediaProcessor)
        {
            var state = new MultiSourceHLSDownloadState
            {
                Id = base.Id,
                Cookies = info.Cookies,
                Headers = info.Headers,
                Authentication = authentication,
                Proxy = proxy,
                TempDirectory = Path.Combine(Config.Instance.TempDir, Id)
            };

            if (state.Authentication == null)
            {
                state.Authentication = Helpers.GetAuthenticationInfoFromConfig(new Uri(info.VideoUri ?? info.AudioUri));
            }

            Log.Debug("Video playlist url: " + info.VideoUri +
                " Audio playlist url: " + info.AudioUri);
            if (info.VideoUri != null && info.AudioUri != null)
            {
                state.NonMuxedVideoPlaylistUrl = new Uri(info.VideoUri);
                state.NonMuxedAudioPlaylistUrl = new Uri(info.AudioUri);
                state.Demuxed = true;
            }
            else
            {
                state.MuxedPlaylistUrl = new Uri(info.VideoUri);
                state.Demuxed = false;
            }

            this._state = state;
            this.TargetFileName = FileHelper.SanitizeFileName(info.File);
        }

        public MultiSourceHLSDownloader(string id, IHttpClient http = null, BaseMediaProcessor mediaProcessor = null) : base(id, http, mediaProcessor)
        {
        }

        public override void Stop()
        {
            this.initLatch?.Break();
            base.Stop();
        }

        private Dictionary<string, HlsPlaylist> ProbeTarget()
        {
            bool GetHlsManifest(Uri uri, out HttpStatusCode statusCode, out string? text)
            {
                statusCode = HttpStatusCode.OK;
                text = null;

                try
                {
                    var request = _http.CreateGetRequest(uri, this._state.Headers, this._state.Cookies, this._state.Authentication);
                    using var response = _http.Send(request);
                    this._cancellationTokenSource.ThrowIfCancellationRequested();
                    statusCode = response.StatusCode;
                    response.EnsureSuccessStatusCode();
                    text = response.ReadAsString(this._cancellationTokenSource);
                    this._cancellationTokenSource.ThrowIfCancellationRequested();
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, ex.Message);
                    return false;
                }
            }

            try
            {
                var state = this._state as MultiSourceHLSDownloadState;
                var results = new string?[state.Demuxed ? 2 : 1];
                var status = new HttpStatusCode[state.Demuxed ? 2 : 1];
                var success = true;
                if (state.Demuxed)
                {
                    initLatch = new CountdownLatch(2);
                    var t1 = new Thread(() =>
                      {
                          var res = GetHlsManifest(state.NonMuxedVideoPlaylistUrl,
                              out HttpStatusCode statusCode,
                              out string? text);
                          results[0] = text;
                          status[0] = statusCode;
                          if (!res)
                          {
                              success = false;
                          }
                          initLatch.CountDown();
                      });
                    t1.Start();

                    var t2 = new Thread(() =>
                      {
                          var res = GetHlsManifest(state.NonMuxedAudioPlaylistUrl,
                              out HttpStatusCode statusCode,
                              out string? text);
                          results[1] = text;
                          status[1] = statusCode;
                          if (!res)
                          {
                              success = false;
                          }
                          initLatch.CountDown();
                      });
                    t2.Start();
                }
                else
                {
                    initLatch = new CountdownLatch(1);

                    new Thread(() =>
                    {
                        var res = GetHlsManifest(state.MuxedPlaylistUrl,
                            out HttpStatusCode statusCode,
                            out string? text);
                        results[0] = text;
                        status[0] = statusCode;
                        if (!res)
                        {
                            success = false;
                        }
                        initLatch.CountDown();
                    }).Start();
                }

                initLatch.Wait();
                this._cancellationTokenSource.ThrowIfCancellationRequested();
                //var results = await Task.WhenAll(tasks);
                HttpStatusCode? FindErrorStatus()
                {
                    if (status.Length == 2)
                    {
                        if (status[0] != HttpStatusCode.OK) return status[0];
                        if (status[1] != HttpStatusCode.OK) return status[1];
                        return null;
                    }
                    else
                    {
                        if (status[0] != HttpStatusCode.OK) return status[0];
                        return null;
                    }
                }

                if (!success)
                {
                    var statusCode = FindErrorStatus();
                    if (statusCode.HasValue)
                    {
                        throw new Exception($"Invalid response code: {statusCode.Value}",
                            new HttpException(statusCode.Value.ToString(), null, statusCode.Value));
                    }
                    throw new Exception("Unable to download HLS manifest");
                }

                var playlists = new Dictionary<string, HlsPlaylist>();
                if (state.Demuxed)
                {
                    playlists["video"] = HlsParser.ParseMediaSegments(results[0]!.Split('\n'), state.NonMuxedVideoPlaylistUrl.ToString());
                    playlists["audio"] = HlsParser.ParseMediaSegments(results[1]!.Split('\n'), state.NonMuxedAudioPlaylistUrl.ToString());

                    this._state.VideoContainerFormat = GuessContainerFormatFromPlaylist(playlists["video"]);
                    this._state.AudioContainerFormat = GuessContainerFormatFromPlaylist(playlists["audio"]);
                    
                    var ext = FileExtensionHelper.GuessContainerFormatFromSegmentExtension(
                            this._state.VideoContainerFormat, this._state.AudioContainerFormat);
                    TargetFileName = Path.GetFileNameWithoutExtension(TargetFileName ?? "video")
                            + ext;

                    Log.Debug($"Guessed Demuxed formats - VideoContainerFormat: {this._state.VideoContainerFormat} AudioContainerFormat: {this._state.AudioContainerFormat}");
                    Log.Debug($"Guessed - ext: {ext} TargetFileName: {TargetFileName}");
                }
                else
                {
                    playlists["muxed"] = HlsParser.ParseMediaSegments(results[0]!.Split('\n'),
                        state.MuxedPlaylistUrl.ToString());
                    _state.Duration = playlists["muxed"].TotalDuration;
                    this._state.VideoContainerFormat = GuessContainerFormatFromPlaylist(playlists["muxed"]);
                    var ext = FileExtensionHelper.GuessContainerFormatFromSegmentExtension(
                            this._state.VideoContainerFormat.ToLowerInvariant());
                    TargetFileName = Path.GetFileNameWithoutExtension(TargetFileName ?? "video")
                                + ext; 
                    
                    Log.Debug($"Guessed Muxed format - VideoContainerFormat: {this._state.VideoContainerFormat}");
                    Log.Debug($"Guessed - ext: {ext} TargetFileName: {TargetFileName}");
                }

                if (string.IsNullOrEmpty(this.TargetDir))
                {
                    this.TargetDir = FileHelper.GetDownloadFolderByFileName(this.TargetFileName);
                }

                return playlists;
            }
            catch { throw; }
        }

        protected override void Init(string tempDir)
        {
            var playlists = ProbeTarget();
            _state.FileSize = -1;
            var i = 0;

            if (this._state.Demuxed)
            {
                var video = playlists["video"];
                var audio = playlists["audio"];
                _chunks = new List<MultiSourceChunk>(video.MediaSegments.Count + audio.MediaSegments.Count);

                this._state.AudioChunkCount = audio.MediaSegments.Count;
                this._state.VideoChunkCount = video.MediaSegments.Count;
                this._state.Duration = Math.Max(playlists["video"].TotalDuration, playlists["audio"].TotalDuration);
                this._state.AudioContainerFormat = Path.GetExtension(FileHelper.GetFileName(audio.MediaSegments.Last().Url));
                this._state.VideoContainerFormat = Path.GetExtension(FileHelper.GetFileName(video.MediaSegments.Last().Url));

                //even for byte range based playlists, we will create separate chunks
                for (; i < Math.Min(this._state.AudioChunkCount, this._state.VideoChunkCount); i++)
                {
                    var chunk1 = CreateChunk(video.MediaSegments[i], video.HasByteRange, 0);
                    _chunks.Add(chunk1);
                    _chunkStreamMap.StreamMap[chunk1.Id] = Path.Combine(tempDir, "1_" + chunk1.Id + FileHelper.GetFileName(chunk1.Uri));

                    var chunk2 = CreateChunk(audio.MediaSegments[i], audio.HasByteRange, 1);
                    _chunks.Add(chunk2);
                    _chunkStreamMap.StreamMap[chunk2.Id] = Path.Combine(tempDir, "2_" + chunk2.Id + FileHelper.GetFileName(chunk2.Uri));
                }
                for (; i < this._state.VideoChunkCount; i++)
                {
                    var chunk = CreateChunk(video.MediaSegments[i], video.HasByteRange, 0);
                    _chunks.Add(chunk);
                    _chunkStreamMap.StreamMap[chunk.Id] = Path.Combine(tempDir, "1_" + chunk.Id + FileHelper.GetFileName(chunk.Uri));
                }
                for (; i < this._state.AudioChunkCount; i++)
                {
                    var chunk = CreateChunk(audio.MediaSegments[i], audio.HasByteRange, 1);
                    _chunks.Add(chunk);
                    _chunkStreamMap.StreamMap[chunk.Id] = Path.Combine(tempDir, "2_" + chunk.Id + FileHelper.GetFileName(chunk.Uri));
                }
            }
            else
            {
                var playlist = playlists["muxed"];
                _chunks = new List<MultiSourceChunk>(playlist.MediaSegments.Count);
                this._state.VideoChunkCount = playlist.MediaSegments.Count;
                this._state.Duration = playlist.TotalDuration;

                for (; i < this._state.VideoChunkCount; i++)
                {
                    var chunk = CreateChunk(playlist.MediaSegments[i], playlist.HasByteRange, 0);
                    _chunks.Add(chunk);
                    _chunkStreamMap.StreamMap[chunk.Id] = Path.Combine(tempDir, "1_" + chunk.Id + FileHelper.GetFileName(chunk.Uri));
                }
            }
        }

        private MultiSourceChunk CreateChunk(HlsMediaSegment mediaSegment, bool hasByteRange, int streamIndex)
        {
            Log.Debug(streamIndex + "-Url: " + mediaSegment.Url);
            return new MultiSourceChunk
            {
                Uri = mediaSegment.Url,
                ChunkState = ChunkState.Ready,
                Id = Guid.NewGuid().ToString(),
                Offset = hasByteRange ? mediaSegment.ByteRange.Key : 0,
                Size = hasByteRange ? mediaSegment.ByteRange.Value : -1,
                Duration = mediaSegment.Duration,
                StreamIndex = streamIndex
            };
        }

        protected override void RestoreState()
        {
            var state = DownloadStateIO.LoadMultiSourceHLSDownloadState(Id!);
            this._state = state;

            try
            {
                Log.Debug("Restoring chunks from: " + Path.Combine(state.TempDirectory, "chunks.db"));

                if (!TransactedIO.ReadStream("chunks.db", state.TempDirectory, s =>
                {
                    _chunks = ChunkStateFromBytes(s);
                }))
                {
                    throw new FileNotFoundException(Path.Combine(state.TempDirectory, "chunks.db"));
                }

                var hlsDir = state.TempDirectory;

                var streamMap = _chunks.Select(c => new
                {
                    c.Id,
                    TempFilePath = Path.Combine(hlsDir, (c.StreamIndex == 0 ? "1_" : "2_") + c.Id + FileHelper.GetFileName(c.Uri))
                }).ToDictionary(e => e.Id, e => e.TempFilePath);
                _chunkStreamMap = new SimpleStreamMap { StreamMap = streamMap };

                var count = 0;
                totalDownloadedBytes = 0;
                _chunks.ForEach(c =>
                {
                    if (c.ChunkState == ChunkState.Finished) count++;
                    if (c.Downloaded > 0) totalDownloadedBytes += c.Downloaded;
                });
                ticksAtDownloadStartOrResume = Helpers.TickCount();
                this.lastProgress = (count * 100) / _chunks.Count;
                Log.Debug("Already downloaded: " + count + " Total: " + _chunks.Count);
            }
            catch
            {
                // ignored
                Log.Debug("Chunk restore failed");
            }
        }

        protected override void SaveState()
        {
            DownloadStateIO.Save((MultiSourceHLSDownloadState)this._state);
        }

        protected override void OnContentTypeReceived(Chunk chunk, string contentType)
        {
        }

        private static string GuessContainerFormatFromPlaylist(HlsPlaylist playlist)
        {
            var file = FileHelper.GetFileName(playlist.MediaSegments.Last().Url);
            return Path.GetExtension(file).ToLowerInvariant();
        }
    }

    public class MultiSourceHLSDownloadState : MultiSourceDownloadState
    {
        public Uri MuxedPlaylistUrl, NonMuxedAudioPlaylistUrl, NonMuxedVideoPlaylistUrl;
    }
}
