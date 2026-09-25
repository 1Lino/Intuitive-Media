# Arquitetura do IntuitiveMedia

## Visão geral

Aplicativo desktop em C#/.NET 10 com Avalonia para a interface e LibVLCSharp para reprodução de mídia. A organização separa a interface, o estado apresentado à interface, o contrato do player e sua implementação baseada em VLC:

```text
Program
  -> App (inicialização e ciclo de vida)
      -> MainWindow
          -> PlayerView
              -> VideoView + menus e controles
                  -> PlayerViewModel
                      -> IMediaPlayerService
                          -> VlcMediaPlayerService
                              -> LibVLC / MediaPlayer
```

## Inicialização

1. `Program` configura e inicia o Avalonia.
2. Em `App.OnFrameworkInitializationCompleted`, `VlcCoreInitializer` inicializa o núcleo nativo do VLC antes da criação do player.
3. A aplicação cria `VlcMediaPlayerService` e o injeta em `PlayerViewModel`.
4. A janela principal recebe esse `PlayerViewModel` como `DataContext`; `PlayerView` usa o mesmo contexto para seus controles.
5. No encerramento, `App` descarta o núcleo LibVLC.

O container de injeção de dependências é configurado em `App`, mas a janela e o ViewModel são construídos diretamente no código atual.

## Responsabilidades

- **`Views/`**: define a janela, o vídeo, os controles e os handlers de interface. `PlayerView` conecta o handle nativo ao `VideoView` e cuida da visibilidade temporizada dos controles e do fullscreen.
- **`ViewModels/PlayerViewModel`**: mantém estado observável para a interface (estado, posição, duração, volume, arquivo atual e visibilidade) e encaminha ações ao serviço. Não depende diretamente dos tipos do VLC.
- **`Core/IMediaPlayerService`**: contrato de reprodução usado pelo ViewModel. Expõe comandos, estado, eventos e um handle opaco necessário para hospedar o vídeo.
- **`Infrastructure/VLC/`**: inicializa LibVLC e implementa o contrato com `MediaPlayer`. Converte eventos do VLC para os estados da aplicação e despacha notificações para a thread da UI.

## Fluxo de reprodução

1. O botão **Load** abre o seletor de arquivos do Avalonia.
2. O caminho selecionado é adicionado a `PlayList`, definido como `CurrentFile` e enviado a `PlayerViewModel.Play`.
3. O ViewModel zera posição e duração e chama `IMediaPlayerService.Play`.
4. `VlcMediaPlayerService` cria a mídia, inicia o `MediaPlayer` e descarta a mídia anterior.
5. Eventos do VLC atualizam estado, posição, duração ou erro. O ViewModel atualiza suas propriedades e os bindings refletem os valores na interface.
6. O slider de posição chama `Seek`; o slider de volume chama `SetVolume`. O botão Play pausa quando o estado é `Playing` e, nos demais estados, solicita retomada.

```text
Controles -> PlayerViewModel -> IMediaPlayerService -> LibVLC
Controles <- propriedades/eventos <- PlayerViewModel <- eventos VLC
```

## Estado e ciclo de vida

`PlaybackState` pertence à aplicação (`Stopped`, `Opening`, `Playing`, `Paused`, `Ended`, `Error`) e evita expor o enum do VLC ao ViewModel. Os eventos nativos chegam de threads do VLC; o serviço usa `Dispatcher.UIThread.Post` para notificar a interface sem bloquear a thread nativa.

Ao trocar de arquivo, o serviço libera a mídia anterior. Ao descartar o serviço, a ordem é parar o player, descartar a mídia atual e descartar o `MediaPlayer`. O núcleo LibVLC é mantido pelo inicializador até o encerramento da aplicação.

## Estado atual e limites

- `MainViewModel` existe, mas a janela em execução recebe `PlayerViewModel` diretamente.
- O seletor aceita um arquivo por vez. Os controles **Prev** e **Next** ainda não têm handlers.
- `MediaDrawer` apresenta itens estáticos de exemplo; ainda não reflete a playlist.
- `AutoRepeat` e a lógica de fim de reprodução existem no ViewModel, mas a navegação de playlist está incompleta.
- `ReachedEndOfPlay` contém dois blocos consecutivos para tratar o fim; com `IsPlaylistEnd` verdadeiro, pode solicitar `Stop` ou `Play` duas vezes.
- Os registros de serviços no container de DI não são usados para criar a janela atual.
- `PlayerViewModel.Dispose()` libera o serviço e remove os handlers, mas o encerramento atual da aplicação descarta apenas `VlcCoreInitializer`; o descarte do ViewModel não está conectado ao shutdown.

## Onde procurar

- Inicialização e composição da janela: `App.axaml.cs`
- Contrato e estados: `Core/IMediaPlayerService.cs`, `Core/PlaybackState.cs`
- Integração VLC: `Infrastructure/VLC/VlcCoreInitializer.cs`, `Infrastructure/VLC/VlcMediaPlayerService.cs`
- Estado e lógica de reprodução: `ViewModels/PlayerViewModel.cs`
- Interface e handlers: `Views/PlayerView.axaml.cs`, `Views/PlayerControls.cs`