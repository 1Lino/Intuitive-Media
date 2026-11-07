# Arquitetura do IntuitiveMedia

## Visão geral

Aplicativo desktop em C# com .NET 10 e Avalonia 12. A interface é organizada em controles Avalonia e usa bindings para um ViewModel; a reprodução é feita pelo LibVLCSharp 3. O projeto também inclui persistência local de preferências e playlists, importação/exportação M3U8 e geração/cache de miniaturas.

```text
Program
  -> App (inicialização)
      -> VlcCoreInitializer
      -> MainWindow (DataContext = PlayerViewModel)
          -> PlayerView
              -> VideoView + menus, drawers e controles
                  -> PlayerViewModel
                      -> IMediaPlayerService
                          -> VlcMediaPlayerService
                              -> LibVLC / MediaPlayer
```

## Inicialização e composição

1. `Program` inicia o Avalonia com detecção da plataforma, tema Fluent, fonte Inter e ferramentas de desenvolvimento em Debug.
2. `App.OnFrameworkInitializationCompleted` cria e inicializa `VlcCoreInitializer` antes de criar o player.
3. Para o ciclo de vida desktop, `App` instancia diretamente `VlcMediaPlayerService`, `PlayerViewModel` e `MainWindow`. O ViewModel é atribuído como `DataContext` da janela e inicializado.
4. Ao abrir a janela, `PlayerViewModel.Play` é chamado para reproduzir o item atual, se houver.
5. Ao fechar, a janela chama `FlushSave`; no encerramento do aplicativo, `App` descarta o núcleo LibVLC.

Embora `App` monte um `ServiceCollection` com registros para `IMediaPlayerService` e `PlayerViewModel`, o provider não é usado para criar os objetos da janela. A composição em execução é manual.

## Camadas e responsabilidades

- **`Views/`**: janela e controles Avalonia. `PlayerView` associa o handle nativo ao `VideoView`, implementa fullscreen por duplo clique e oculta os controles após inatividade. `PlayerControls`, `MediaDrawer` e `PlaylistDrawer` tratam interações de interface, seletores de arquivo, ordenação por arrastar e soltar e ações de playlist.
- **`ViewModels/PlayerViewModel`**: estado observável da tela e lógica do player, da playlist atual, de playlists salvas, das preferências, do progresso e das miniaturas. Interage com a reprodução por `IMediaPlayerService`, sem usar tipos de LibVLCSharp diretamente.
- **`Core/`**: contrato `IMediaPlayerService`, enum `PlaybackState` e argumentos de mudança de estado. O estado da aplicação não expõe o enum do VLC.
- **`Infrastructure/VLC/`**: inicialização do núcleo LibVLC, implementação de `IMediaPlayerService` com `MediaPlayer` e serviço de geração/cache de miniaturas.
- **`Infrastructure/Services/` e `Models/`**: leitura/escrita de configurações e dados das playlists, além da serialização de playlists no formato M3U8.

`MainViewModel` existe, mas é apenas um placeholder e não participa da composição atual. `ViewLocator` está registrado como template da aplicação, mas a janela principal usa uma view definida diretamente e não depende dele para ser criada.

## Reprodução e interface

`PlayerViewModel` assina os eventos de estado, posição, duração, volume e erro do serviço. Os eventos nativos do LibVLC são encaminhados de forma assíncrona à thread da UI com `Dispatcher.UIThread.Post`.

- O seletor de mídia permite escolher vários arquivos. Eles substituem a lista atual, e o primeiro começa a tocar.
- Clicar em um item no drawer de mídias seleciona e reproduz esse item. O drawer permite reordenar os vídeos por arrastar e soltar, com animação.
- O botão de play/pausa, os controles anterior/próximo, o slider de posição e o controle de volume chamam operações do ViewModel/serviço.
- O menu de opções oferece autoplay, repetição do item, loop da playlist e shuffle.
- Os controles são exibidos ao mover o ponteiro sobre o vídeo e ocultados após três segundos, exceto quando o ponteiro está sobre controles, há um arraste em andamento ou o menu de opções está aberto.
- O handle nativo é exposto pelo contrato como `object`; somente `PlayerView` faz o cast para associá-lo ao `VideoView`.

O serviço troca a mídia atual ao iniciar outra reprodução, publica estado e erros e implementa play, pause, resume, stop, seek e volume. Ao ser descartado, interrompe o player e libera a mídia e o `MediaPlayer`.

## Playlists, configurações e miniaturas

- `PlayerViewModel.Playlist` contém os vídeos da playlist ativa; `Playlists` contém os itens mostrados no drawer de playlists.
- Clicar no item de uma playlist carrega seus itens sem iniciar reprodução. O botão de play sobre a miniatura carrega e inicia a playlist. As playlists podem ser reorganizadas com arrastar e soltar, criadas a partir da lista atual ou removidas.
- `M3u8Playlist` lê entradas M3U8, resolve caminhos relativos a partir da pasta do arquivo e grava playlists com título e caminho de cada mídia. A interface oferece importação e exportação por seletores de arquivo.
- `SettingsStore` serializa preferências e playlists em JSON em `%LocalAppData%\\IntuitiveMedia\\settings.json`. As preferências incluem volume, mute, autoplay, shuffle e modo de repetição. As playlists guardam seus itens, o item selecionado e uma posição em milissegundos. Alterações são salvas com debounce; algumas operações e o fechamento da janela fazem salvamento imediato.
- `ThumbnailService` gera miniaturas com uma instância própria do LibVLC, limitada a uma captura por vez. O cache PNG fica em `%LocalAppData%\\IntuitiveMedia\\Thumbnails`; sua chave considera versão do cache, caminho, tamanho e data de modificação do arquivo.

## Limitações e pontos de atenção atuais

- `PlayerViewModel` concentra reprodução, preferências, playlists, persistência e carregamento de miniaturas; é o principal ponto de concentração de responsabilidades.
- `MainViewModel` não é utilizado e o container de injeção de dependências também não participa da criação dos objetos em execução.
- O serviço de player do ViewModel é descartado por `PlayerViewModel.Dispose`, mas esse descarte não está conectado ao encerramento atual. `App` descarta o núcleo LibVLC no shutdown, e a janela apenas salva as configurações ao fechar.
- A inicialização restaura a playlist e o item selecionado, mas não aplica `PositionMs` para retomar do ponto salvo. `UpdateProgress` grava o progresso quando chamado, porém não há chamada a esse método conectada aos controles/timer atuais.
- Abrir um arquivo diretamente pelo sistema e soltar arquivos sobre o aplicativo ainda não estão implementados. O drag-and-drop existente reorganiza itens dentro dos drawers.
- `ReachedEndOfPlay` centraliza a navegação no fim da mídia, mas a lógica de avanço depende de `AutoPlay`; os demais modos e a navegação anterior/próxima devem ser verificados em conjunto antes de ampliar o comportamento de playlist.
- Os estados de janela e dimensões dos drawers são gerenciados parcialmente em code-behind e no ViewModel.

## Arquivos de referência

- Inicialização e janela: `Program.cs`, `App.axaml.cs`, `Views/MainWindow.axaml.cs`
- Contrato e estados: `Core/IMediaPlayerService.cs`, `Core/PlaybackState.cs`
- Player e preferências: `ViewModels/PlayerViewModel.cs`
- Integração LibVLC: `Infrastructure/VLC/VlcCoreInitializer.cs`, `Infrastructure/VLC/VlcMediaPlayerService.cs`
- Miniaturas: `Infrastructure/VLC/ThumbnailService.cs`
- Persistência JSON e M3U8: `Infrastructure/Services/AppSettingsService.cs`, `Infrastructure/Services/SaveLoadPlaylist.cs`, `Models/AppSettings.cs`
- Interface do player e playlists: `Views/PlayerView.axaml`, `Views/PlayerControls.axaml`, `Views/MediaDrawer.axaml`, `Views/PlaylistDrawer.axaml`
