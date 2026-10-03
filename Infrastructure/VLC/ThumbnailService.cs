using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using LibVLCSharp.Shared;

namespace IntuitiveMedia.Services;

// Esta é talvez a parte mais chata deste projeto (pra quem não é fã de matemática), pois envolve alguns cálculos de computação gráfica (da qual entendo muito pouco, diga-se de passagem, por isso precisei pesquisar ainda mais pra implementar esse trecho aqui).
public static class ThumbnailService
{
    private const int W = 200;
    private const int H = 112;          // 16:9
    private const int Pitch = W * 4;    // RV32 = 4 bytes por pixel

    private static readonly SemaphoreSlim Gate = new(1, 1);   // pra capturar thumb de somente um vídeo por vez
    private static LibVLC? _vlc;

    public static async Task<Bitmap?> GenerateAsync(string videoPath)
    {
        if (Uri.TryCreate(videoPath, UriKind.Absolute, out var uri) && uri.IsFile)
            videoPath = uri.LocalPath;

        // Console.WriteLine($"[Thumb] iniciando: {videoPath} (existe: {File.Exists(videoPath)})");

        await Gate.WaitAsync();
        try
        {
            var bmp = await CaptureAsync(videoPath);
            // Console.WriteLine($"[Thumb] resultado: {(bmp is null ? "NULL (timeout/sem frame)" : "ok")}");
            return bmp;
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"[Thumb] ERRO: {ex}");
            return null;
        }
        finally { Gate.Release(); }
    }

    private const double GoodEnough = 50;   // IMPORTANTE: essa é a nota a partir da qual esse sistema deve parar de procurar pelo frame perfeito
    private static async Task<Bitmap?> CaptureAsync(string path)
    {
        // instância paralela do libVLC, sem áudio e sem decodificação por hardware
        _vlc ??= new LibVLC("--no-audio", "--avcodec-hw=none", "--no-video-title-show");

        // 1) duração
        long durationMs = 0;
        using (var probe = new Media(_vlc, path, FromType.FromPath))
        {
            await probe.Parse(MediaParseOptions.ParseLocal);
            durationMs = probe.Duration;
        }

        var starts = durationMs > 0
            ? new[] { 0.20, 0.40, 0.60 }.Select(f => f * durationMs / 1000.0).ToArray()
            : new[] { 1.0 };

        // 2) candidatos + pontuação
        byte[]? best = null;
        var bestScore = -1.0;

        foreach (var start in starts)
        {
            foreach (var frame in await GrabCandidatesAsync(path, start))
            {
                var score = Score(frame);
                if (score > bestScore) { best = frame; bestScore = score; }
            }
            // Console.WriteLine($"[Thumb] início {start:F1}s, melhor nota até agora: {bestScore:F1}");
            if (bestScore >= GoodEnough) break;
        }

        if (best is null) return null;

        // 3) BGRA -> Bitmap
        var bmp = new WriteableBitmap(new PixelSize(W, H), new Vector(96, 96),
                                      PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using (var fb = bmp.Lock())
        {
            for (var y = 0; y < H; y++)
                Marshal.Copy(best, y * Pitch, fb.Address + y * fb.RowBytes, Pitch);
        }
        return bmp;
    }

    // método que efetivamente pega os frames mais apropriados para a thumb do vídeo.
    private static async Task<List<byte[]>> GrabCandidatesAsync(string path, double startSeconds)
    {
        const int SkipFrames = 15;   // descarta os primeiros frames
        const int Step = 5;          // espaçamento entre frames candidatos
        const int Count = 5;         // quantos candidatos guardar

        var buffer = new byte[Pitch * H];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var candidates = new List<byte[]>();
        var frames = 0;

        MediaPlayer.LibVLCVideoLockCb lockCb = (opaque, planes) =>
        {
            Marshal.WriteIntPtr(planes, handle.AddrOfPinnedObject());
            return IntPtr.Zero;
        };
        MediaPlayer.LibVLCVideoUnlockCb unlockCb = (opaque, picture, planes) => { };
        MediaPlayer.LibVLCVideoDisplayCb displayCb = (opaque, picture) =>
        {
            var n = Interlocked.Increment(ref frames);
            if (n < SkipFrames || (n - SkipFrames) % Step != 0) return;

            lock (candidates)
            {
                if (candidates.Count < Count)
                    candidates.Add((byte[])buffer.Clone());
                if (candidates.Count >= Count)
                    tcs.TrySetResult(true);
            }
        };

        try
        {
            using var media = new Media(_vlc!, path, FromType.FromPath);
            media.AddOption($":start-time={startSeconds.ToString("F2", CultureInfo.InvariantCulture)}");

            using var player = new MediaPlayer(media);
            player.SetVideoFormat("RV32", W, H, Pitch);
            player.SetVideoCallbacks(lockCb, unlockCb, displayCb);
            player.Play();

            await Task.WhenAny(tcs.Task, Task.Delay(5000));
            await Task.Run(() => player.Stop());
        }
        finally
        {
            GC.KeepAlive(lockCb); GC.KeepAlive(unlockCb); GC.KeepAlive(displayCb);
            handle.Free();
        }

        lock (candidates) return new List<byte[]>(candidates);
    }

    // Score faz alguns cálculos para determinar quais frames devem ser ignorados. Basicamente, frames com cores estouradas ou que sejam completamente pretos não passam, e não viram thumbnail:
    private static double Score(byte[] px)
    {
        var n = W * H;
        var luma = new double[n];
        double sum = 0, sum2 = 0;

        for (int i = 0, p = 0; i < px.Length; i += 4, p++)
        {
            var l = 0.299 * px[i + 2] + 0.587 * px[i + 1] + 0.114 * px[i];
            luma[p] = l;
            sum += l;
            sum2 += l * l;
        }

        var mean = sum / n;
        var std = Math.Sqrt(Math.Max(0, sum2 / n - mean * mean));

        // força média das bordas (gradiente horizontal + vertical)
        double edge = 0;
        var count = 0;
        for (var y = 1; y < H - 1; y++)
        {
            for (var x = 1; x < W - 1; x++)
            {
                var gx = luma[y * W + x + 1] - luma[y * W + x - 1];
                var gy = luma[(y + 1) * W + x] - luma[(y - 1) * W + x];
                edge += Math.Abs(gx) + Math.Abs(gy);
                count++;
            }
        }
        edge /= count;

        // penaliza só frames completamente pretos ou estourados
        var penalty = (mean < 15 || mean > 240) ? 0.3 : 1.0;

        return (std * 0.5 + edge * 2) * penalty;
    }
}