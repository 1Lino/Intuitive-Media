# Avisos de terceiros

O arquivo [LICENSE](./LICENSE) cobre somente o código original deste projeto. Dependências, fontes e recursos de terceiros mantêm suas próprias licenças; este aviso não as substitui.

## Dependências diretas

Licenças conferidas nos metadados dos pacotes NuGet usados pelo projeto:

| Pacote | Versão | Licença |
|---|---:|---|
| Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent | 12.1.2 | MIT |
| Avalonia.Fonts.Inter | 12.1.2 | MIT para o pacote; a fonte Inter é SIL OFL 1.1 |
| CommunityToolkit.Mvvm | 8.4.2 | MIT |
| LibVLCSharp, LibVLCSharp.Avalonia | 3.10.1 | LGPL-2.1-or-later |
| VideoLAN.LibVLC.Windows | 3.0.23.1 | LGPL-2.1-or-later |
| AvaloniaUI.DiagnosticsSupport | 2.2.3 | Consulte os termos atuais do Avalonia DevTools; o pacote é usado somente em configuração Debug |

O restore também inclui dependências transitivas. Cada pacote transitivo continua sujeito à licença indicada por seu respectivo distribuidor; esta lista das dependências diretas não é um inventário completo dessas licenças.

## Recursos incluídos

- Os ícones Fluent em `Assets` são do Fluent UI System Icons, licenciados sob MIT. [Licença e avisos](https://github.com/microsoft/fluentui-system-icons/blob/main/LICENSE).
- A fonte Inter é licenciada sob SIL Open Font License 1.1. [Texto da licença](https://github.com/rsms/inter/blob/master/LICENSE.txt).

## LibVLC e redistribuição

As publicações portáteis deste projeto incluem os binários nativos do LibVLC por meio de `VideoLAN.LibVLC.Windows`. Antes de redistribuir uma publicação, cumpra a LGPL-2.1-or-later aplicável à versão incluída. Isso inclui fornecer os avisos e o texto da licença, bem como atender às condições da licença para a biblioteca e para a forma como ela é ligada e distribuída. A LGPL não altera a licença MIT do código original deste projeto.

Texto oficial: [GNU LGPL versão 2.1](https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html). O código-fonte do LibVLC está no [repositório VideoLAN VLC](https://code.videolan.org/videolan/vlc); confira as instruções de redistribuição e o código correspondente à versão efetivamente distribuída.

Este aviso é informativo e não substitui a análise dos pacotes e obrigações aplicáveis à publicação concreta.
