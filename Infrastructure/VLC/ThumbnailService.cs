using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using LibVLCSharp.Shared;

namespace IntuitiveMedia.Services;

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

        Console.WriteLine($"[Thumb] iniciando: {videoPath} (existe: {File.Exists(videoPath)})");

        await Gate.WaitAsync();
        try
        {
            var bmp = await CaptureAsync(videoPath);
            Console.WriteLine($"[Thumb] resultado: {(bmp is null ? "NULL (timeout/sem frame)" : "ok")}");
            return bmp;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Thumb] ERRO: {ex}");
            return null;
        }
        finally { Gate.Release(); }
    }

    private static async Task<Bitmap?> CaptureAsync(string path)
    {
        // instância paralela do libVLC, sem áudio e sem decodificação por hardware
        _vlc ??= new LibVLC("--no-audio", "--avcodec-hw=none", "--no-video-title-show");

        var buffer = new byte[Pitch * H];
        var snapshot = new byte[Pitch * H];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        MediaPlayer.LibVLCVideoLockCb lockCb = (opaque, planes) =>
        {
            Marshal.WriteIntPtr(planes, handle.AddrOfPinnedObject());
            return IntPtr.Zero;
        };
        MediaPlayer.LibVLCVideoUnlockCb unlockCb = (opaque, picture, planes) => { };

        // isso aqui pode ser ajustado para que a thumbnail capturada seja uma imagem bem formada
        const int SkipFrames = 30;
        var frames = 0;

        MediaPlayer.LibVLCVideoDisplayCb displayCb = (opaque, picture) =>
        {
            var n = Interlocked.Increment(ref frames);

            // guarda sempre o frame mais recente; os primeiros serão sobrescritos
            Buffer.BlockCopy(buffer, 0, snapshot, 0, buffer.Length);

            if (n >= SkipFrames)
                tcs.TrySetResult(true);
        };

        try
        {
            using var media = new Media(_vlc, path, FromType.FromPath);
            media.AddOption(":start-time=1");   // começa em 1s para evitar frame preto inicial

            using var player = new MediaPlayer(media);
            player.SetVideoFormat("RV32", W, H, Pitch);
            player.SetVideoCallbacks(lockCb, unlockCb, displayCb);
            player.Play();

            var finished = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            await Task.Run(() => player.Stop());

            // só desiste se nenhum frame chegou
            if (finished != tcs.Task && Volatile.Read(ref frames) == 0)
                return null;
        }
        finally
        {
            GC.KeepAlive(lockCb); GC.KeepAlive(unlockCb); GC.KeepAlive(displayCb);
            handle.Free();
        }

        // conversão do buffer BGRA em Bitmap:
        var bmp = new WriteableBitmap(new PixelSize(W, H), new Vector(96, 96),
                                      PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using (var fb = bmp.Lock())
        {
            for (var y = 0; y < H; y++)
                Marshal.Copy(snapshot, y * Pitch, fb.Address + y * fb.RowBytes, Pitch);
        }
        return bmp;
    }
}