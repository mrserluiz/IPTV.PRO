# Análise estática e migração do XCIPTV 7.0 para Windows

## Escopo e método

Arquivo: XCIPTV.apk. Pacote confirmado pelo AndroidManifest: `com.nathnetwork.xciptv`. Versão: 7.0, código 907. SHA-256: `89a1bedcc70a953fab79368df45d37e0a769e505341179b20e62675cd177c396`.

Foi inspecionado o contêiner ZIP, o manifesto binário, os recursos e as tabelas de strings dos dois DEX (52.431 entradas antes de deduplicar). Foram desmontadas 63 classes do namespace `com.nathnetwork.xciptv`, preservando instruções e chamadas de métodos. Há também classes de suporte em namespaces ofuscados: a desmontagem das 63 classes não corresponde à recuperação de todo o programa. Foram examinadas as instruções dos pontos de login e atualização de catálogo. Não houve execução instrumentada, captura de tráfego, conexão a servidor de provedor, análise exaustiva das bibliotecas nativas nem recuperação integral de Java de alto nível.

O relatório distingue presença de código de funcionamento verificado: uma classe ou string demonstra implementação/referência no APK, mas não prova que o serviço atual habilite aquele recurso.

## Estrutura

| Componente | Evidência | Consequência na migração |
|---|---|---|
| Aplicação Android | Manifesto, Activities, SharedPreferences e serviços | Interface e ciclo de vida precisam ser reimplementados |
| DEX | `classes.dex` ~9,2 MiB; `classes2.dex` ~0,7 MiB | Bytecode Android não executa no CLR do Windows |
| Telas | 41 Activities declaradas; 282 arquivos nas pastas de layout | Layouts Android não podem ser usados diretamente como Windows Forms |
| Código nativo | ARM64, ARMv7, x86 e x86_64; `libnative-lib.so` | ELF Android não é DLL Windows, mesmo em x86_64 |
| Reprodução | `libvlc.so`, `libvlcjni.so`, `libffmpegJNI.so`, strings ExoPlayer 2.19.1 | Reutilizar VLC para Windows; substituir a integração Android |
| Persistência | SQL de favoritos, gravações, controle parental e multitelas | Modelos precisam de equivalentes e migração própria |
| Serviços | `RecordingServices`, `OTRServices`, WorkManager | Agendamentos Android precisam de equivalente desktop |
| Dependências adicionais | Classes/manifesto de Firebase, Google e publicidade | Não necessárias para reproduzir a API de catálogo; não migradas |

A presença de `widevine` e classes genéricas de DRM pode vir da biblioteca de reprodução: não confirma que o conteúdo do seu provedor use DRM. Não foi feita tentativa de contornar DRM ou obter chaves.

## Login Xtream confirmado nas instruções

Em `LoginActivity`, as instruções constroem:

```text
{servidor}/player_api.php?username={usuario}&password={senha}
```

Há chamadas a `URLEncoder.encode` nos dados de acesso e uma ramificação de configuração `send_udid`, comparada com `yes`, que acrescenta `&uid=` com o identificador armazenado em `did`. Isso significa que um cliente que implemente apenas Xtream padrão pode falhar em um provedor que exija esse complemento.

O protótipo implementa a rota base, codifica os parâmetros e verifica a resposta. Não inventa nem imita identificadores de dispositivos.

## Catálogos confirmados

`updatecontents/XCUpdateContents` monta as ações abaixo. Os mesmos nomes foram usados no cliente C#.

| Ação | Função | Estado no cliente |
|---|---|---|
| `get_live_categories` | Categorias de TV | Implementada |
| `get_live_streams` | Lista de canais | Implementada |
| `get_vod_categories` | Categorias de filmes | Implementada |
| `get_vod_streams` | Lista de filmes | Implementada |
| `get_vod_info` | Detalhes de filme | Referência no APK; não implementada |
| `get_series_categories` | Categorias de séries | Implementada |
| `get_series` | Lista de séries | Implementada |
| `get_series_info&series_id=` | Temporadas e episódios | Implementada para resposta padrão com `episodes` por temporada |

Os formatos JSON esperados pelo cliente são convenções Xtream e não foram confrontados com uma resposta real do seu provedor. IDs `stream_id`, `series_id`, `category_id` e extensões precisam ser fornecidos pelo painel.

## Reprodução e EPG

Encontrados os fragmentos `/live/`, `/movie/`, `/series/`, `/timeshift/` e `/xmltv.php?username=`. O cliente monta URLs Xtream padrão com usuário, senha, identificador e extensão. A montagem é uma reimplementação do protocolo; não foi provada equivalência de todas as ramificações do player Android.

O programa usa `.ts` por padrão para TV e a extensão fornecida para filmes/episódios, com fallback `.mp4`. Painéis que exijam HLS, `direct_source`, token específico, cabeçalhos extras ou outra construção podem precisar de ajuste.

A prévia consulta `get_short_epg` como recurso padrão Xtream. Essa escolha não deve ser confundida com tradução integral do EPG do APK: o código original também contém XMLTV, armazenamento local e outra API de EPG.

O VLC externo evita depender das bibliotecas `.so` Android e aproveita o que já está instalado no computador. Incorporar o vídeo requer LibVLC para Windows e gerenciamento de instância, janela, eventos, controle de reprodução e descarte; ainda não foi implementado.

## Outras famílias de conexão

Também existem referências a rotas com token:

```text
/server/get_channel_category?token=
/server/get_channel_list?token=
/server/get_movie_category?token=
/server/get_movie_list?token=
/server/get_series?token=
/server/get_series_category?token=
/server/get_series_episode?token=
/server/get_series_profile?token=
/server/get_epg_info?token=
/server/query_user_days_left?token=
```

A origem do token, o contrato completo dessas respostas e o tipo exato de painel não foram determinados. Essas rotas não foram tratadas como equivalentes de Xtream nem implementadas com base apenas em seus nomes. A busca por `stalker` não encontrou esse termo nas strings, o que não exclui suporte ofuscado ou indireto.

## Cobertura funcional

| Recurso original/referência | Evidência | Resultado da migração |
|---|---|---|
| Login e catálogo Xtream | Instruções de LoginActivity/XCUpdateContents | Primeira implementação C# |
| M3U | `M3UUpdateContents`, `#EXTM3U`, textos de entrada de URL | Importação local simplificada; sem URL remota nem diretivas especiais |
| TV/filmes/séries | Activities, ações e fragmentos de URL | Catálogo e reprodução via VLC externo |
| Rádio | `RadioPlayerActivity` | Sem seção dedicada |
| Busca e favoritos | `SearchActivity` e estruturas locais | Busca local e favoritos de lista |
| EPG | PlayStreamEPGActivity, XMLTV e rota com token | EPG curto Xtream; não equivale ao guia original |
| Catch-up | CatchupActivity, CatchupPlayerActivity, `/timeshift/` | Não implementado |
| Gravação/agendamento | RecordingServices, tabela `recordings`, textos de agendamento | Não implementado |
| Lembretes | ProgramRemindersActivity | Não implementado |
| Controle parental | ParentalControlActivity, tabelas por tipo | Não implementado |
| Multitelas | MultiScreenActivityEXO, `multiscreen.db` | Não implementado |
| Histórico/perfis/backup | UsersHistoryActivity, BackupActivity | Não implementado |
| Manutenção/configuração remota | MaintenanceActivity, ServiceStatusActivity, OTRServices | Contratos não reproduzidos |
| Rotina nativa de criptografia | `encryption/Encrypt` carrega `native-lib`; métodos nativos | Não portada; necessário estudo separado se indispensável |

## Conversão de conceitos para C#

| Android/Java | Implementação Windows |
|---|---|
| Activity/layout XML | Form, TableLayoutPanel e controles Windows Forms |
| Requisições assíncronas Android | Task + HttpWebRequest, sem bloquear a interface |
| Dados JSON | JavaScriptSerializer do .NET Framework |
| Atualização de catálogo | Objetos Entry e filtros de listas |
| Player Android/VLC JNI | Processo VLC Windows com URL de reprodução |
| Favoritos locais | Arquivo local de identificadores |
| Seleção de arquivo M3U | OpenFileDialog e parser simplificado |

O código entregue é uma implementação independente da parcela descrita. Não é uma tradução linha por linha do Java desmontado e não contém código nativo Android convertido. O pacote não inclui o APK, imagens/marca do XCIPTV nem segredos extraídos do aplicativo.

## Próximos pontos necessários para paridade

1. Testar login, catálogos e reprodução com o tipo real de acesso usado no MuMu, sem divulgar a senha nesta conversa.
2. Validar variações de servidor, formato de séries, extensões, redirecionamentos e requisitos de dispositivo.
3. Incorporar LibVLC e desenvolver controles de áudio, legendas, volume, tela cheia e tratamento de erros.
4. Implementar XMLTV/cache e grade, catch-up, gravação, lembretes, parental, histórico e perfis.
5. Testar Windows 10/11, arquiteturas e catálogos grandes; criar instalador e atualizar a documentação.

## Referências técnicas

- Microsoft, opções do compilador C#: https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/
- VideoLAN, LibVLC: https://videolan.videolan.me/vlc/master/libvlc.html
- VideoLAN, incorporação da janela Windows: https://videolan.videolan.me/vlc-3.0/group__libvlc__media__player.html

As conclusões sobre o XCIPTV vêm do arquivo enviado, não dessas referências gerais.
