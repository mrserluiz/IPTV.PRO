# XCIPTV-PC para Windows

Cliente independente para Xtream e M3U, sem afiliação com o aplicativo XCIPTV oficial.

## Novidades: login automático e player interno

Após um login válido, a opção **Lembrar senha e entrar automaticamente** guarda os dados de acesso criptografados pela DPAPI do Windows, vinculados ao usuário atual. Ao abrir novamente, o programa tenta entrar uma vez. Em caso de falha, a tela de login permanece disponível. Desmarque a opção ou use **Sair da conta** para remover o login salvo.

O player usa o motor LibVLC 3 da instalação do VLC, exibindo o vídeo dentro do XCIPTV-PC. Inclui pausa/continuação, parar, volume, busca quando a mídia permite e tela cheia com saída por Escape. O VLC 3.x deve ter a mesma arquitetura do executável (normalmente 64 bits). Em caso de falha ao carregar o motor, o aplicativo oferece abertura no VLC externo. Instalar o VLC continua sendo necessário; suas bibliotecas não estão embutidas neste executável.

[Baixar versões](https://github.com/mrserluiz/IPTV.PRO/releases)

## Validação

O workflow Windows compila o programa, verifica a criptografia, o tratamento de respostas e o layout. Também instala VLC e testa vídeo AVI sintético local: associação da janela, saída de vídeo, avanço do tempo, pausa e parada. A reprodução do seu provedor precisa ser confirmada no seu computador.

## Recursos

Canais, filmes, séries, categorias, favoritos, busca, EPG curto e M3U local. Ainda não implementa gravação, multitelas, XMLTV completo, catch-up e controle parental.

## Compilar

Execute Compilar.cmd ou use Actions → Windows EXE. Requer .NET Framework 4.5+ (preferencialmente 4.8) e VLC 3.x. Não coloque dados de acesso no repositório.
