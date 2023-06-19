﻿using System;
using System.Collections.Generic;
using XDM.Core;
using XDM.Core.Clients.Http;

namespace XDM.Core.Downloader.Progressive
{
    public interface IPieceCallback
    {
        public bool IsFirstRequest(StreamType streamType);
        public bool IsFileChangedOnServer(StreamType streamType, long streamSize, DateTime? lastModified);
        public Piece GetPiece(string pieceId);
        public HeaderData?
            GetHeaderUrlAndCookies(string pieceId);
        public IHttpClient? GetSharedHttpClient(string pieceId);
        public void PieceConnected(string pieceId, ProbeResult? result);
        public string GetPieceFile(string pieceId);
        public void UpdateDownloadedBytesCount(string pieceId, long bytes);
        public bool ContinueAdjacentPiece(string pieceId, long maxByteRange);
        public void PieceDownloadFailed(string pieceId, ErrorCode error);
        public void PieceDownloadFinished(string pieceId);
        public void ThrottleIfNeeded();
        public bool IsTextRedirectionAllowed();
    }

    public struct HeaderData
    {
        public Dictionary<string, List<string>> Headers { get; set; }
        public string? Cookies { get; set; }
        public Uri Url { get; set; }
        public AuthenticationInfo? Authentication { get; set; }
        public ProxyInfo? Proxy { get; set; }
    }
}
