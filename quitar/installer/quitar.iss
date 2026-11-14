; Instalador de Quitar (nombre provisional). Se compila con Inno Setup 6.
; La versión llega desde afuera: ISCC.exe /DAppVersion=0.2.7 installer\quitar.iss
; Espera encontrar el ejecutable publicado en ..\publish\Quitar.exe

#ifndef AppVersion
  #define AppVersion "0.2.0"
#endif

[Setup]
AppId={{6C0B3E52-8D1F-4B7A-9E24-5A7D1C3F8B90}
AppName=Quitar
AppVersion={#AppVersion}
AppPublisher=Borrador
DefaultDirName={autopf}\Quitar
DefaultGroupName=Quitar
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=Quitar-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\Quitar.exe

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\Quitar.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Quitar"; Filename: "{app}\Quitar.exe"
Name: "{autodesktop}\Quitar"; Filename: "{app}\Quitar.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Quitar.exe"; Description: "{cm:LaunchProgram,Quitar}"; Flags: nowait postinstall skipifsilent
