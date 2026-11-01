# XCIPTV-PC para Windows

Cliente independente em C# para serviços Xtream e listas M3U locais, sem afiliação com o XCIPTV oficial.

## Download

[Baixar XCIPTV-PC.exe v0.1.3](https://github.com/mrserluiz/IPTV.PRO/releases/download/v0.1.3/XCIPTV-PC.exe)

[Versões disponíveis](https://github.com/mrserluiz/IPTV.PRO/releases)

Abra o executável no Windows. Requer .NET Framework 4.5 ou posterior (preferencialmente 4.8) e VLC instalado. A reprodução abre em uma janela separada do VLC.

## Recursos e validação

Login Xtream, categorias, canais, filmes, episódios, busca, favoritos, programação curta e importação M3U local. Inclui diagnóstico de DNS, conexão, TLS, HTTP e resposta da API. O workflow verifica compilação, tratamento de respostas e inicialização da interface. O acesso e a reprodução com seu provedor ainda precisam de teste.

Não possui todos os recursos do XCIPTV: ainda faltam player incorporado, gravação, multitelas, XMLTV completo, catch-up e controle parental.

O nome do aplicativo e do executável passou de EtherPlayer para XCIPTV-PC. Os favoritos continuam na pasta de dados anterior para preservar as preferências existentes. O repositório permanece em mrserluiz/IPTV.PRO.

Consulte [LEIA-ME.md](LEIA-ME.md) e [ANALISE_TECNICA.md](ANALISE_TECNICA.md).

## Compilar

Execute Compilar.cmd ou use Actions → Windows EXE → Run workflow. Não coloque credenciais no código ou no repositório.
