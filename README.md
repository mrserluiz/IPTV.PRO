## Navegação 0.2.3

TV ao vivo inicia com um clique esquerdo na capa, sem outro botão de play. Clique direito na capa permite adicionar ou remover favoritos. Os favoritos de TV ao vivo, filmes e séries ficam separados; essa categoria reúne favoritos de todos os gêneros da seção atual. Favoritar um episódio marca sua série.

O botão Voltar no player retorna à lista ou à sinopse de origem, sem minimizar. A tela cheia usa a mesma janela principal e preserva o estado anterior ao sair. Trocas de tela são aplicadas de uma vez, sem tela vazia intermediária; as animações de janela do Windows são desativadas apenas para o aplicativo. Capas e fundo da sinopse são carregados em paralelo e aplicados juntos.

Os testes Windows verificam separação de favoritos, alvo do menu contextual, retorno sem minimizar e início automático de um canal local com um clique real.

## Correções 0.2.1

Rolagem dos gêneros pela roda do mouse mesmo sem foco na lista. Cache de capas com proteção das imagens visíveis e atualização apenas dos cartões afetados. Detecção do foco nativo do VLC para restaurar controles com o movimento do ponteiro; clique na área de vídeo alterna play/pausa, inclusive em tela cheia, sem interferir nos botões. A dica de rolagem acompanha o ponteiro sem redesenho contínuo.

O workflow testa rolagem sem foco, retenção das capas e interação real do mouse sobre um vídeo local do VLC (ocultar/restaurar controles e clicar para pausar/continuar), além das verificações anteriores.

# XCIPTV-PC para Windows

Cliente independente para Xtream e M3U, sem afiliação com o aplicativo XCIPTV oficial.

## Novidades 0.2: sinopse e player flutuante

Ícones circulares sem cantos pretos, responsivos até 380 px (duas vezes o tamanho anterior). Listas com roda do mouse, teclado e barras ocultas; após permanecer sobre uma lista, uma dica acompanha o ponteiro por até 30 segundos.

Filmes e séries abrem uma tela de sinopse com capa ampliada, fundo escurecido, nota, gênero, data, elenco e direção retornados pelo provedor. A classificação exibida é a nota enviada pela API, não uma classificação etária verificada. Dados ausentes são indicados. Séries permitem escolher o episódio após a sinopse.

Controles flutuantes sobre o vídeo com capa, título, metadados, tempo atual e final. Incluem saltos de ±1 minuto quando a mídia permite, botão único play/pausa, proporção/zoom, legendas embutidas ou arquivo local, volume visível, mudo e ajustes. Botão voltar à sinopse/lista e Início no canto superior esquerdo. No modo tela cheia ou maximizado, os controles somem após 3 s sem mover o mouse e voltam ao movimentá-lo. Espaço pausa/continua, setas saltam 1 minuto, F11 alterna tela cheia, Esc sai dela.

Gravação experimental em MPEG-TS: clique no disquete para iniciar e novamente para terminar; o vídeo pausa e o programa confirma se deseja salvar, abrindo por padrão Vídeos do Windows. O trecho é temporário até essa confirmação. Usa uma segunda conexão VLC com a mídia, portanto depende dos codecs, formato e limite de conexões do provedor. Em VOD tenta começar na posição atual, sujeita ao ponto de busca/keyframe. Não captura legendas renderizadas na imagem. Falhas são informadas; a função não contorna restrições do provedor.

## Navegação e Início

A tela Início reúne TV ao vivo, guia da TV, filmes e séries. O botão com desenho de casa retorna ao Início a partir do login, carregamento, listas e player (inclusive em tela cheia). Voltar ao Início para a reprodução. As listas possuem categorias laterais, contagem, busca e grade de logos/capas com cache limitado. O carregamento mostra o resultado de cada catálogo; o EPG curto é consultado ao escolher um canal.

## Login automático e player interno

Após um login válido, a opção **Lembrar senha e entrar automaticamente** guarda os dados de acesso criptografados pela DPAPI do Windows, vinculados ao usuário atual. Ao abrir novamente, o programa tenta entrar uma vez. Em caso de falha, a tela de login permanece disponível. Desmarque a opção ou use **Sair da conta** para remover o login salvo.

O player usa o motor LibVLC 3 da instalação do VLC, exibindo o vídeo dentro do XCIPTV-PC. Inclui pausa/continuação, parar, volume, busca quando a mídia permite e tela cheia com saída por Escape. O VLC 3.x deve ter a mesma arquitetura do executável (normalmente 64 bits). Em caso de falha ao carregar o motor, o aplicativo oferece abertura no VLC externo. Instalar o VLC continua sendo necessário; suas bibliotecas não estão embutidas neste executável.

[Baixar versões](https://github.com/mrserluiz/IPTV.PRO/releases)

## Validação

O workflow Windows compila o programa, verifica a criptografia, o tratamento de respostas e o layout. Também instala VLC e testa gravação local, áudio, proporção, legendas e vídeo AVI sintético local: associação da janela, saída de vídeo, avanço do tempo, pausa e parada. A reprodução do seu provedor precisa ser confirmada no seu computador.

## Recursos

Canais, filmes, séries, categorias, favoritos, busca, EPG curto e M3U local. Ainda não implementa agendamento de gravação, multitelas, XMLTV completo, catch-up e controle parental.

## Compilar

Execute Compilar.cmd ou use Actions → Windows EXE. Requer .NET Framework 4.5+ (preferencialmente 4.8) e VLC 3.x. Não coloque dados de acesso no repositório.




