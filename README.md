# EtherPlayer para Windows

Cliente nativo em C# para serviços Xtream e listas M3U locais. Prévia independente, sem afiliação com XCIPTV.

## Download

[Baixar EtherPlayer.exe v0.1.1](https://github.com/mrserluiz/IPTV.PRO/releases/download/v0.1.1/EtherPlayer.exe)

[Versões disponíveis](https://github.com/mrserluiz/IPTV.PRO/releases)

Abra o executável diretamente no Windows. Requer .NET Framework 4.5 ou posterior (preferencialmente 4.8) e VLC instalado. Se necessário, selecione o caminho de vlc.exe quando solicitado.

## Estado verificado

A versão v0.1.1 foi compilada em um runner Windows do GitHub Actions. A inicialização da interface passou no teste automatizado `--smoke-test`. Login real, catálogos e reprodução com provedor IPTV ainda não foram testados.

## Recursos

Login Xtream, categorias, TV ao vivo, filmes, episódios de séries, busca, favoritos de lista, EPG curto e importação local simplificada de M3U. A reprodução abre no VLC em janela separada.

Ainda não há player incorporado, gravação, multitelas, XMLTV completo, catch-up e controle parental. Esta versão não tem paridade completa com o XCIPTV.

Consulte [LEIA-ME.md](LEIA-ME.md) e [ANALISE_TECNICA.md](ANALISE_TECNICA.md).

## Compilar

Execute `Compilar.cmd` no Windows ou use a aba Actions → Windows EXE → Run workflow. Cada compilação de main publica uma prévia após compilar e testar a abertura da interface. Não coloque credenciais do provedor no código ou no repositório.
