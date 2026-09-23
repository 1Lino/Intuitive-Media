using System;
using System.Threading;
using LibVLCSharp.Shared;

namespace IntuitiveMedia.Infrastructure.VLC;

public sealed class VlcCoreInitializer : IDisposable
{

    private static int _initialized;
    private LibVLC? _libVlc;
    public LibVLC LibVlc => _libVlc
        ?? throw new InvalidOperationException($"{nameof(VlcCoreInitializer)} não foi inicializado. Chame {nameof(EnsureInitialized)}() no startup.");

    public void EnsureInitialized(bool enableDebugLogs = false)
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return;

        LibVLCSharp.Shared.Core.Initialize();

        _libVlc = new LibVLC(enableDebugLogs);
    }

    public void Dispose()
    {
        _libVlc?.Dispose();
        _libVlc = null;
    }
}
