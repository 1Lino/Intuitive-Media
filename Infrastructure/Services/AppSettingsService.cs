using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class SettingsStore
{
    // Esse path lê-se como: %LocalAppData%\IntuitiveMedia\settings.json
    private static readonly string FilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IntuitiveMedia", "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppSettings Load()
    {
        try
        {
            // processo simples de carregamento
            if (!File.Exists(FilePath)) return new AppSettings();
            var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options);
            return Sanitize(s ?? new AppSettings());
        }
        catch
        {
            // arquivo corrompido: guarda cópia e recomeça do padrão
            try { File.Move(FilePath, FilePath + ".bad", overwrite: true); } catch { }
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        // puxa diretório elaborado em FilePath e o cria.
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
        var tmp = FilePath + ".tmp"; // arquivo
        File.WriteAllText(tmp, JsonSerializer.Serialize(settings, Options)); // escreve os settings do app no arquivo
        File.Move(tmp, FilePath, overwrite: true);   // escreve o arquivo e o move para o destino, sobrescrevendo versões anteriores do arquivo na pasta.
    }

    // Aplica as regras de null, no caso de o arquivo de configurações do app estar com propriedades null.
    private static AppSettings Sanitize(AppSettings s)
    {
        // checa id da playlist atual carregada, se existir
        if (s.CurrentPlaylistId is { } id && s.Playlists.All(p => p.Id != id))
            s.CurrentPlaylistId = null;

        // faz o mesmo com cada item das playlists carregadas no app em sua abertura.
        // vale lembrar que o app deve carregar tais playlists automaticamente toda vez que for aberto, caso
        // tais playlists existam, claro.
        foreach (var p in s.Playlists)
        {
            if (p.CurrentItemId is { } cur && p.Items.All(i => i.Id != cur))
            {
                p.CurrentItemId = null;
                p.PositionMs = 0;
            }
            if (p.CurrentItemId is null) p.PositionMs = 0;
        }
        return s;
    }
}