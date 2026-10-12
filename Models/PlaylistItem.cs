using System.Collections.Generic;

namespace IntuitiveMedia.Models;

// Já que se trata da mesma implementação de VideoItem, deixa PlaylistItem herdar as mesmas propriedades.
public class PlaylistItem : VideoItem
{
    // lista que guarda os vídeos. No caso, apenas o path deles.
    // 1 - toda vez que uma playlist for alterada ou salva, esta lista deve ser atualizada. E o arquivo de save da playlist deve puxar estes valores aqui pra salvar. No mesmo modo, ao carregar os valores da playlist, os paths devem vir para cá, e então serem mandados para a lista que controla os vídeos executados, etc. Deve haver um serviço de save e load que lida com todo esse processo.
    // 2 - A propriedade "thumbnail" deve puxar a thumb do primeiro vídeo de uma lista de vídeos carregados. Só que, no momento, ainda não foi implementado um serviço que salva estas thumbnails em um local. Tal serviço deve ser implementado antes.
    public List<string> MediaPaths = new List<string>();
}