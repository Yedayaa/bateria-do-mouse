# Bateria do mouse

Um indicador simples de bateria para Windows: a porcentagem fica perto do relógio e os avisos chegam quando o mouse precisa carregar.

**Suporte inicial testado: Redragon K1NG Pro 1K (M916 Pro 1K), pelo receptor USB de 2,4 GHz.**

[**Baixar o aplicativo para Windows**](https://github.com/Yedayaa/bateria-do-mouse/releases/latest) · [Relatar um problema ou solicitar outro modelo](https://github.com/Yedayaa/bateria-do-mouse/issues)

## O que o app faz

- Exibe a porcentagem na área de notificações, inclusive nos ícones ocultos.
- Consulta automaticamente a cada **1 hora**; clicar no ícone ou em **Atualizar agora** faz uma consulta imediata.
- Mostra um aviso em **15%**, outro em **5%** e um de **carga completa em 100%** enquanto carrega, sem repetir a cada consulta.
- Usa verde acima de 15%, amarelo de 6% a 15% e vermelho em 5% ou menos, no número e na barra, como um indicador de celular. Em alto contraste, preserva as cores de texto do sistema.
- Acompanha os temas claro e escuro do Windows, com suporte às cores de alto contraste.
- Oferece instalação por usuário, atalho no menu Iniciar e inicialização automática em segundo plano.
- Funciona localmente, sem criar uma conta ou instalar drivers adicionais.

## Baixar e usar

Na seção **Releases**, baixe o pacote `BateriaMouse-v1.3.1-windows.zip` e extraia os arquivos.

1. Abra `Instalar.exe` para instalar somente no seu usuário. A opção de iniciar com o Windows vem marcada e pode ser desativada.
2. O app fica em **Bateria do mouse**, no menu Iniciar. Ao entrar no Windows, inicia somente o indicador perto do relógio.
3. Clique no número da bateria para abrir os detalhes. Fechar a janela pelo **X** mantém o indicador funcionando.
4. Para encerrar, clique com o botão direito no ícone e escolha **Sair**.

O aplicativo é independente, sem vínculo com Microsoft ou Redragon. A versão inicial não possui assinatura digital de editor. Se preferir verificar o código, compile localmente seguindo as instruções abaixo; não é necessário desativar proteções do Windows.

Também é possível usar sem instalar: mantenha `BateriaMouse.exe` e `BateriaMouse.exe.config` na mesma pasta e abra o executável. No modo portátil, o caminho da inicialização automática depende dessa pasta.

O Windows pode colocar o ícone dentro da setinha de ícones ocultos. Para deixá-lo sempre à vista, arraste-o para perto do relógio.

## Instalação e remoção

A instalação usa `%LOCALAPPDATA%\Programs\Bateria do Mouse`, sem privilégios de administrador. O atalho fica no menu Iniciar do usuário. A opção de inicialização usa somente o valor `K1NGProBattery` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

Para remover a versão instalada, use **Configurações → Aplicativos → Aplicativos instalados → Bateria do mouse → Desinstalar**. O desinstalador remove apenas os arquivos conhecidos do app e as entradas que correspondem à instalação. Em uma atualização, uma cópia do executável anterior é mantida para recuperação.

## Compatibilidade e limites

| Dispositivo ou conexão | Situação |
| --- | --- |
| Redragon K1NG Pro 1K / M916 Pro 1K, receptor `3554:F5D5` | Testado em hardware |
| Receptor `3554:F55D` do mesmo modelo | Previsto no protocolo; sem teste físico nesta versão |
| Cabo USB, `3554:F55E` do mesmo modelo | Previsto no protocolo; sem teste físico nesta versão |
| Bluetooth | Sem suporte nesta versão |
| Outros modelos e marcas | Precisam de um adaptador compatível; não são detectados automaticamente |

A porcentagem é informada pelo firmware do mouse. Sua precisão depende do hardware e pode variar durante o carregamento. O app não mede a capacidade real da bateria.

Se uma consulta falhar, o ícone mostra `--`. Uma leitura anterior aparece somente nos detalhes, identificada pelo horário. Mova o mouse e atualize se ele estiver dormindo. O programa oficial da Redragon não precisa permanecer aberto.

Os avisos ocorrem na próxima consulta válida, mesmo se o firmware pular o número exato de 15% ou 5%. Se o app começar abaixo de 5%, mostra apenas o aviso crítico. Pequenas oscilações não geram avisos repetidos. Os avisos são reativados após recarga ou recuperação suficiente; o Windows pode silenciá-los conforme as configurações de notificações.

O aviso de carga completa exige uma leitura de 100% com o estado de carregamento informado pelo mouse. Ele aparece uma vez por conexão de carga, sem repetir por pequenas oscilações de 99% para 100%. Essa parte depende de o firmware informar corretamente que está carregando.

**O intervalo automático é de 1 hora para reduzir as consultas ao mouse. Os alertas dependem da próxima leitura e podem demorar até uma hora para aparecer.** Abra o indicador quando quiser uma leitura imediata. Não são enviados comandos de despertar nem sinais periódicos para manter o receptor acordado. A própria consulta ainda pode envolver comunicação por rádio; esta configuração não garante consumo zero da bateria.

## Privacidade e segurança

O app não possui conexões de rede, telemetria, histórico ou atualização automática. As leituras ficam apenas na memória. Ele não captura teclas, cliques, movimentos, telas, conteúdo de páginas ou documentos.

As consultas limitam-se aos identificadores USB de suporte e à coleção de controle HID `FF02:0002`, com relatórios de 17 bytes. As coleções de teclado, movimento e mídia são excluídas. O único comando construído é consulta de bateria (`04`); não há comandos de despertar ou manutenção de conexão, nem comandos para alterar configurações, parear, gravar memória ou atualizar firmware.

As respostas passam por validação de identificador, comando, endereço, tamanho, checksum, porcentagem, carga e tensão. Essas verificações reduzem erros e exposição; não representam certificação nem garantia de segurança absoluta.

O diagnóstico `Probe.exe`, quando compilado e executado manualmente, imprime somente identificadores USB genéricos, capacidades das interfaces e a resposta de controle da consulta. Não grava arquivo nem inclui números de série ou caminhos dos dispositivos. Não publique capturas de tela ou registros pessoais ao relatar problemas.

## Compilar e verificar

Requisitos: Windows 10 ou 11, .NET Framework 4.8 ou posterior e Windows PowerShell 5.1 ou PowerShell 7. Nenhum pacote externo ou download é necessário durante a compilação.

Execute na pasta do código:

```powershell
.\Compilar.ps1
```

O script gera os executáveis em `release`, verifica o protocolo e os alertas, e monta o ZIP em `dist`. Os testes cobrem uma resposta real do receptor, carregamento, checksum, dados inválidos, comandos permitidos, limites de 15% e 5%, carga completa em 100%, saltos entre faixas e prevenção de repetições. As notificações visuais dependem das configurações do Windows; os testes da política usam leituras simuladas.

Arquivos principais: `HidBattery.cs` (protocolo), `BatteryAlerts.cs` (avisos), `TrayApp.cs` e `NativeStyle.cs` (interface), `Installer.cs` (instalador), `Desinstalar.ps1` (remoção) e `Verify.cs` (verificação).

## Contribuir com outros mouses

Novos modelos precisam de suporte ao protocolo específico de leitura da bateria. Abra uma issue com o modelo, a conexão utilizada e, se souber, os IDs genéricos de fabricante e produto. Não envie números de série, credenciais ou registros completos do computador. Alterações de firmware ou comandos de gravação não são necessários para este projeto.

## Referência e licença

O protocolo foi consultado em [Redragon M916 Pro Protocol Reference](https://github.com/dongkid/g49-web-control/blob/main/SPEC.md). A coleção `FF02:0002` e a resposta com tamanho `02` foram confirmadas no receptor usado durante o desenvolvimento. A implementação deste app foi escrita em C# para esta finalidade.

Código aberto sob a licença MIT; consulte `LICENSE`.
