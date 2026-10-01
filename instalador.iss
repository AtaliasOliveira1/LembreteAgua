; Script do Inno Setup para o Lembrete de Água
#define MyAppName "Lembrete de Água"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Atalias Lô-Amí"
#define MyAppExeName "LembreteAgua.exe"
#define MyAppIcon "icone.ico"

[Setup]
; Identificador único do programa (Novo GUID gerado para o Lembrete de Água)
AppId={{E4A8F231-6B19-4C82-9D03-51F8762E19A4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

; Nome do arquivo do instalador que vai ser criado
OutputBaseFilename=LembreteAgua_v1.0.0_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; Define o ícone do arquivo do instalador (.exe do Setup)
SetupIconFile={#MyAppIcon}

; Define o ícone que aparece no "Adicionar ou Remover Programas" do Windows
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copia o arquivo de ícone para dentro da pasta instalada
Source: "{#MyAppIcon}"; DestDir: "{app}"; Flags: ignoreversion

; Copia o executável principal da pasta de publicação
Source: "bin\Release\net8.0-windows\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Copia TODOS os arquivos e DLLs da pasta publish
Source: "bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Cria o atalho no Menu Iniciar com o ícone personalizado
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppIcon}"

; Cria o atalho na Área de Trabalho com o ícone personalizado
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppIcon}"; Tasks: desktopicon

[Run]
; Opção para rodar o programa logo após terminar a instalação
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: postinstall skipifsilent