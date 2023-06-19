﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XDM.Core
{
    public static class IconResource
    {
        private static Dictionary<string, HashSet<string>> imageTypes = new()
        {
            ["Compressed"] = new HashSet<string> { ".zip", ".gz", ".tar", ".xz", ".7z", ".rar", ".bz2" },
            ["Music"] = new HashSet<string> { ".mp3", ".aac", ".ac3", ".wma", ".m4a", ".ogg", ".mka" },
            ["Video"] = new HashSet<string> { ".mp4", ".mkv", ".ts", ".webm", ".avi", ".divx", ".mov", ".m4v" },
            ["Document"] = new HashSet<string> { ".docx", ".doc", ".pdf", ".txt", ".xlsx", ".xls", ".html" },
            ["ApplicationContext.Core"] = new HashSet<string> { ".exe", ".bin", ".appx", ".ApplicationContext.Core", ".msi", ".rpm", ".deb" }
        };

        private static string GetFileType(string ext)
        {
            foreach (var key in imageTypes.Keys)
            {
                var extList = imageTypes[key];
                if (extList.Contains(ext))
                {
                    return key;
                }
            }
            return "Other";
        }

        public static string GetFontIconForFileType(string file)
        {
            var ext = Path.GetExtension(file)?.ToLowerInvariant() ?? string.Empty;
            var fileType = GetFileType(ext);
            return fileType switch
            {
                "Compressed" => RemixIcon.GetFontIcon(RemixIcon.ArchiveIcon),
                "Music" => RemixIcon.GetFontIcon(RemixIcon.MusicIcon),
                "Video" => RemixIcon.GetFontIcon(RemixIcon.VideoIcon),
                "Document" => RemixIcon.GetFontIcon(RemixIcon.DocumentIcon),
                "ApplicationContext.Core" => RemixIcon.GetFontIcon(RemixIcon.AppIcon),
                _ => RemixIcon.GetFontIcon(RemixIcon.OtherFileIcon),
            };
        }

        public static string GetSVGNameForFileType(string file)
        {
            var ext = Path.GetExtension(file)?.ToLowerInvariant() ?? string.Empty;
            var fileType = GetFileType(ext);
            return fileType switch
            {
                "Compressed" => "file-zip-line",
                "Music" => "file-music-line",
                "Video" => "movie-line",
                "Document" => "file-text-line",
                "ApplicationContext.Core" => "function-line",
                _ => "file-line",
            };
        }
    }
}
