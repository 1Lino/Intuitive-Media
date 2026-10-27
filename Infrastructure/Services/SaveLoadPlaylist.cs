using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IntuitiveMedia.Services;

public static class M3u8Playlist
{
    public record Entry(string Path, string? Title);

    public static void Save(string filePath, IEnumerable<ItemData> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");

        foreach (var item in items)
        {
            // -1 = duração desconhecida
            sb.AppendLine($"#EXTINF:-1,{item.Title}");
            sb.AppendLine(item.Path);
        }

        // UTF-8 sem BOM, como o formato .m3u8 espera
        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false));
    }

    public static List<Entry> Load(string filePath)
    {
        var result = new List<Entry>();
        var baseDir = System.IO.Path.GetDirectoryName(filePath) ?? "";
        string? pendingTitle = null;

        foreach (var raw in File.ReadLines(filePath, Encoding.UTF8))
        {
            var line = raw.Trim().TrimStart('\uFEFF');   // remove BOM, se houver
            if (line.Length == 0) continue;

            if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                var comma = line.IndexOf(',');
                pendingTitle = comma >= 0 ? line[(comma + 1)..].Trim() : null;
                continue;
            }

            if (line.StartsWith('#')) continue;          // outras diretivas e comentários

            result.Add(new Entry(ResolvePath(line, baseDir), pendingTitle));
            pendingTitle = null;
        }

        return result;
    }

    private static string ResolvePath(string entry, string baseDir)
    {
        // URL de rede: mantém como está
        if (entry.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            entry.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return entry;

        // URI file:///...
        if (Uri.TryCreate(entry, UriKind.Absolute, out var uri) && uri.IsFile)
            return uri.LocalPath;

        // caminho relativo: resolve a partir da pasta do .m3u8
        return System.IO.Path.IsPathRooted(entry)
            ? entry
            : System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, entry));
    }
}