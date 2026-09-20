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

### Licença

O código é livre e pode ser utilizado por qualquer um para uso pessoal, para estudos, como referência ou como base para outro projeto qualquer, caso seja do interesse.
