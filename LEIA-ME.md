# EtherPlayer — prévia nativa para Windows 0.1

Este pacote contém uma análise estática do XCIPTV 7.0 enviado pelo usuário e uma implementação independente em C# / Windows Forms. Não é o XCIPTV oficial nem uma conversão integral do aplicativo Android.

## Executar

1. Extraia este ZIP para uma pasta no computador.
2. Se `EtherPlayer.exe` estiver incluído, abra esse arquivo. Caso contrário, execute `Compilar.cmd`: ele procura o compilador do .NET Framework instalado no Windows, gera o executável e abre o programa.
3. Informe o endereço base do seu servidor, usuário e senha do seu provedor Xtream. Não use a URL de um canal no campo servidor.
4. Escolha TV ao vivo, Filmes ou Séries. Selecione uma categoria e use o campo à direita para buscar.
5. Dê dois cliques em um canal, filme ou episódio. O vídeo abre em uma janela separada do VLC instalado.
6. Para séries, o primeiro duplo clique carrega os episódios; o segundo reproduz o episódio escolhido.

Requisitos: Windows com .NET Framework 4.5 ou posterior (preferencialmente 4.8), VLC para Windows e uma conta/servidor compatível com a API Xtream padrão. Se o VLC estiver fora do caminho habitual, o programa pedirá que você selecione `vlc.exe`.

## Recursos desta prévia

- Login Xtream, verificando `user_info.auth` e o estado da conta.
- Catálogos de TV ao vivo, filmes e séries, separados por categorias.
- Episódios de séries, busca por nome e favoritos locais da lista atual.
- Programação curta por canal quando o servidor fornecer `get_short_epg`.
- Importação de M3U/M3U8 **local** com links absolutos HTTP/HTTPS. Não serve para abrir um manifesto HLS como catálogo de canais. Diretivas especiais, URLs relativas, cookies e cabeçalhos da lista não são interpretados.
- Reprodução externa pelo VLC. Áudio, legendas e controles de reprodução ficam no VLC.

## Limitações reais

Não há ainda reprodução incorporada, XMLTV completo, gravação/agendamento, catch-up, multitelas, controle parental, histórico/retomada, perfis, login por token de outros painéis nem paridade visual com o XCIPTV. Favoritos são filtrados sobre o catálogo atualmente carregado; não são um catálogo global. Metadados completos de filmes, imagens e pôsteres não são exibidos.

A senha fica apenas em memória no aplicativo. O protocolo Xtream a inclui nas URLs de conexão e reprodução; ao abrir o VLC, a URL é passada como argumento de processo e pode aparecer nas ferramentas locais do sistema. Prefira HTTPS quando seu provedor suportar. Os favoritos guardam identificadores e endereço do servidor em `%LOCALAPPDATA%\EtherPlayer\favorites.txt`; para listas M3U podem guardar a URL do item, inclusive tokens presentes nela.

Nenhuma conexão com seu provedor foi feita durante a análise. O APK não fornece automaticamente os dados da sessão que você usa no MuMu. Provedores podem exigir UID, cabeçalhos ou integrações não implementadas.

Veja `ANALISE_TECNICA.md` para as evidências, a cobertura e o plano de migração. O estado dos testes está em `VALIDACAO.md`.
