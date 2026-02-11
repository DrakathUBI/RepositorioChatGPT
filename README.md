# Macro Studio — Programa desktop real (.exe)

Atendendo ao pedido, o projeto agora é um **aplicativo desktop em C#/.NET (Windows Forms)**, compilável para **.exe**.

## Funcionalidades

- Gravação global de teclado e mouse (estilo macro recorder).
- Tela de inspeção no próprio app com tabela de ações (`Action`, `Value`, `Timestamp`, `Wait`, `Kind`).
- Filtro de inspeção por tipo: **Tudo / Cliques / Movimento mouse / Teclado / Espera**.
- Playback filter com checkboxes: **Mouse moves**, **Mouse clicks**, **Key presses**, **Wait times**.
- Replay da macro com controle de velocidade.
- Parâmetros (`{{nome}}`) para reutilizar macros.
- Modo IA opcional com Gemini (`GEMINI_API_KEY`).

## Requisitos

- Windows 10/11
- Conexão de internet para baixar o SDK na primeira execução (se não tiver `dotnet` já instalado).

## Quero rodar **na mesma máquina**, sem admin

## Fluxo rápido (se você vai baixar ZIP do GitHub)

Perfeito — esse é o caminho mais simples no seu cenário.

1. Baixe o ZIP: `https://github.com/DrakathUBI/RepositorioChatGPT/archive/refs/heads/main.zip`
2. Extraia o ZIP no Desktop.
3. Renomeie a pasta extraída para `RepositorioChatGPT` (se vier como `RepositorioChatGPT-main`).
4. Abra PowerShell e rode:

```powershell
cd C:\caminho\para\RepositorioChatGPT   # ex.: Desktop ou Downloads
powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1
```

> Se você já está dentro da pasta do repositório (como no seu log em `Downloads`), **não precisa dar `cd` para Desktop**: execute só o script.

5. Execute o programa:

```powershell
cd .\dist
.\MacroStudio.exe
```

> Se der erro de política ao rodar script (`ExecutionPolicy`), use o mesmo comando com `-ExecutionPolicy Bypass` como mostrado acima.


Sem usar outra máquina: rode o script abaixo, que instala o .NET SDK **só no seu usuário** (`%USERPROFILE%\.dotnet`) e gera o `.exe`.

> **Importante:** o nome correto do script é `build_local_no_admin.ps1` (termina com `.ps1`, não `.cs1`).

### 1) Gerar o executável localmente

Abra PowerShell e primeiro garanta que você está com o repositório completo.

**Se você tem Git instalado:**

```powershell
git clone https://github.com/DrakathUBI/RepositorioChatGPT.git
cd RepositorioChatGPT
```

**Se você NÃO tem Git instalado (seu caso):**

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\download_repo_no_git.ps1
cd C:\caminho\para\RepositorioChatGPT
```

Depois execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1
```

Esse script:

1. baixa `dotnet-install.ps1`;
2. instala SDK .NET 8 em `%USERPROFILE%\.dotnet` (sem admin);
3. roda `restore` + `publish`;
4. gera `dist\MacroStudio.exe`.

### 2) Executar

```bat
cd dist
MacroStudio.exe
```

## Comandos manuais (se preferir sem script)

```powershell
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile .\dotnet-install.ps1
powershell -ExecutionPolicy Bypass -File .\dotnet-install.ps1 -Version 8.0.303 -InstallDir "$env:USERPROFILE\.dotnet"
$env:DOTNET_ROOT="$env:USERPROFILE\.dotnet"
$env:PATH="$env:DOTNET_ROOT;$env:PATH"
$env:PATH="$env:DOTNET_ROOT\tools;$env:PATH"
& "$env:DOTNET_ROOT\dotnet.exe" restore .\MacroStudio\MacroStudio.csproj
& "$env:DOTNET_ROOT\dotnet.exe" publish .\MacroStudio\MacroStudio.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\dist
```



### Erro `git : O termo "git" não é reconhecido`

Isso significa que o Git não está instalado (ou não está no `PATH`).

Use uma destas opções:

1. **Sem instalar Git**: rode o script `scripts/download_repo_no_git.ps1` para baixar ZIP e extrair automaticamente.
2. **Manual sem script**: baixe `https://github.com/DrakathUBI/RepositorioChatGPT/archive/refs/heads/main.zip`, extraia e renomeie a pasta para `RepositorioChatGPT`.

Depois entre na pasta e rode:

```powershell
cd C:\caminho\para\RepositorioChatGPT   # ex.: Desktop ou Downloads
powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1
```


### Erro `NU1101: Não é possível encontrar o pacote ...`

Se aparecer erro de pacote no `restore`, atualize para a versão mais recente do repositório (o pacote correto agora é `MouseKeyHook`).

Depois rode novamente:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1
```

### Aviso `NU1603` do MouseKeyHook

Se aparecer `NU1603` dizendo que `5.6.1` não foi encontrada e que `5.7.1` foi usada, atualize para a versão mais recente do repositório.
Nesta versão já fixamos para `MouseKeyHook` `5.7.1`, então o aviso deve desaparecer.

### Erro `MSB1009: Arquivo de projeto não existe`

Esse erro acontece quando o script é executado fora da pasta correta ou com projeto incompleto.

Use exatamente (com Git):

```powershell
git clone https://github.com/DrakathUBI/RepositorioChatGPT.git
cd RepositorioChatGPT
powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1
```

Ou, sem Git:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\download_repo_no_git.ps1
cd C:\caminho\para\RepositorioChatGPT   # ex.: Desktop ou Downloads
powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1
```

O script foi ajustado para localizar o `MacroStudio.csproj` automaticamente e **interromper com erro claro** se não encontrar.

## Se a empresa bloquear execução

Em ambiente corporativo isso pode acontecer por AppLocker/Defender/EDR. Aí normalmente é necessário pedir ao TI:

- liberação do hash/assinatura do `MacroStudio.exe`;
- liberação da pasta `dist` no seu perfil de usuário;
- ou publicação oficial via SCCM/Intune.


## Sem conexão com GitHub no PC da empresa: como levar e usar

Se esse computador não acessa GitHub, você ainda consegue usar normalmente com pacote offline.

### Opção A (recomendada): levar pasta pronta em pendrive

Em um computador que tenha internet, prepare a pasta do projeto e copie para pendrive:

1. Baixe/clone o projeto.
2. Gere o executável local (`dist\MacroStudio.exe`) com o script `build_local_no_admin.ps1`.
3. Copie para pendrive:
   - pasta `dist` (obrigatório)
   - pasta `macros` (opcional, para seus JSON)
4. No PC empresarial, copie para sua pasta de usuário e execute `dist\MacroStudio.exe`.

### Opção B: levar código-fonte sem GitHub e compilar no PC da empresa

1. Em um PC com internet, compacte o repositório em `.zip`.
2. Leve o `.zip` por pendrive e extraia no PC empresarial.
3. Rode no PowerShell (sem admin):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build_local_no_admin.ps1
```

Isso instala o SDK em `%USERPROFILE%\.dotnet` e publica `dist\MacroStudio.exe`.

### Se PowerShell ou downloads externos forem bloqueados

Peça ao TI um destes caminhos:

- liberar execução do `build_local_no_admin.ps1`;
- liberar download do `https://dot.net/v1/dotnet-install.ps1`;
- ou receber da TI o `MacroStudio.exe` já publicado para sua máquina.


## Diagnóstico rápido no PowerShell (sem erro de sintaxe)

No seu print apareceu erro porque:

- `t status` está incorreto (o certo é `git status`);
- `&&` pode falhar em PowerShell antigo (Windows PowerShell 5.1).

Use assim no **PowerShell**:

```powershell
git status --short
rg -n "MacroStudio.csproj|DOTNET_CLI_TELEMETRY_OPTOUT|MSB1009|DrakathUBI/RepositorioChatGPT" README.md scripts/build_local_no_admin.ps1
```

Ou em uma linha no PowerShell 5.1:

```powershell
git status --short; rg -n "MacroStudio.csproj|DOTNET_CLI_TELEMETRY_OPTOUT|MSB1009|DrakathUBI/RepositorioChatGPT" README.md scripts/build_local_no_admin.ps1
```

Se quiser usar `&&`, abra o **PowerShell 7+** (`pwsh`).

## IA Gemini (opcional)

Defina variável de ambiente antes de abrir o app:

```bat
set GEMINI_API_KEY=SUA_CHAVE
dist\MacroStudio.exe
```

## Observações importantes para empresa

- O app roda em contexto de usuário. Não precisa admin para uso normal.
- Alguns antivírus/Windows podem solicitar permissões por causa de hook global de entrada.
- Em máquinas corporativas com proteção avançada, captura/replay de teclado e mouse pode ser bloqueada por política de segurança.
