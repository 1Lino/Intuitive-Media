# IntuitiveMedia Player

O objetivo deste projeto é desenvolver um aplicativo Desktop de reprodução de mídia. Como o nome já diz, "IntuitiveMedia Player" pretende ser um reprodutor de mídia que possui funcionalidades simples e intuitivas. Além das funções genéricas de um reprodutor de mídia, o aplicativo possuirá ainda outras, tais como:

1. Print de tela
2. Playlists customizadas
3. Rearranjo de itens de mídia por meio de um menu suspenso
4. Opções de reprodução automática

### Tecnologias utilizadas

- Plataforma .NET (C#)
- Framework UI Avalonia
- LibVLC
- Fluent UI System

No contexto deste projeto, essas libs do **VLC** são necessárias:

```Shell
dotnet add package LibVLCSharp && dotnet add package LibVLCSharp.Avalonia
```

E para que o **VLC** funcione, também:

```Shell
dotnet add package VideoLAN.LibVLC.Windows
```

Este último comando é necessário porque .NET precisa dos binários nativos do VLC. Para mais detalhes sobre esta lib e sua instalação, veja: [code.videolan.org/videolan/LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp)

Quanto ao framework do **Avalonia**, segue o tutorial oficial de instalação e preparação do ambiente: [docs.avaloniaui.net/docs/get-started/install-avalonia](https://docs.avaloniaui.net/docs/get-started/install-avalonia)

Sobre **Fluent UI System**, veja:

Ícones:[ icon-sets.iconify.design/fluent](https://icon-sets.iconify.design/fluent/)

Repositório oficial: [github.com/microsoft/fluentui-system-icons](https://github.com/microsoft/fluentui-system-icons)

### Abrir vídeos pelo Windows Explorer

O executável recebe os caminhos enviados pelo Windows como argumentos, carrega os arquivos existentes na playlist atual e inicia o primeiro. A publicação portátil inclui `register-open-with.ps1`, que registra o aplicativo apenas para o usuário atual e o disponibiliza em **Abrir com** para formatos de vídeo comuns. O script não altera o aplicativo padrão e não precisa de privilégios de administrador.

Extraia a publicação em uma pasta permanente e execute nela:

```powershell
powershell -ExecutionPolicy Bypass -File .\register-open-with.ps1
```

Para remover a integração:

```powershell
powershell -ExecutionPolicy Bypass -File .\register-open-with.ps1 -Unregister
```

Também é possível iniciar o app diretamente com um ou mais caminhos entre aspas:

```powershell
.\IntuitiveMedia.exe "C:\Videos\video 1.mp4" "C:\Videos\video 2.mkv"
```

### Licença e dependências de terceiros

O código original deste projeto é distribuído sob a licença MIT. Consulte [LICENSE](./LICENSE).

Essa licença não se estende às dependências, fontes ou recursos de terceiros. A publicação portátil inclui o LibVLC, licenciado sob LGPL-2.1-or-later, e os avisos correspondentes. Antes de redistribuir o aplicativo, leia [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md) e cumpra as condições da licença do LibVLC. Os arquivos de licença e avisos são incluídos na pasta publicada.
