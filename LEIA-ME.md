# XCIPTV-PC — player interno e login automático

Abra XCIPTV-PC.exe, preencha os dados do provedor e entre com **Lembrar senha e entrar automaticamente** marcado. A senha é protegida pelo Windows e guardada somente após autenticação bem-sucedida. Na próxima abertura, o programa tentará conectar uma vez. Para apagar o acesso salvo, use **Sair da conta** ou desmarque a opção.

Selecione um canal, filme ou episódio para reproduzir no player interno. Há controles de pausa, volume, parada, progresso e tela cheia. Escape sai da tela cheia. A barra de progresso só é habilitada para mídias que permitem buscar outra posição.

Requisitos: .NET Framework 4.5 ou posterior e VLC 3.x instalado, com a mesma arquitetura do executável. Use **Configurar VLC** para selecionar vlc.exe. Se trocar a instalação carregada pelo motor interno, feche e abra o programa. Quando o player interno não puder iniciar, o programa oferece o VLC externo como alternativa.

A senha criptografada fica em credentials.bin, ao lado das preferências no diretório local de dados anteriormente usado pelo EtherPlayer. A proteção é vinculada ao usuário do Windows; não é uma garantia contra outros programas executados com a mesma conta. A autenticação continua sujeita ao protocolo do provedor e ao uso de HTTP ou HTTPS pelo servidor.

Compilação e testes locais automatizados não substituem a verificação dos canais do seu provedor. Não há ainda gravação, multitelas, XMLTV completo, catch-up ou controle parental.
