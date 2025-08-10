[Setup]
AppName=R5Reloaded
AppVersion=1.3.6
WizardStyle=modern
DefaultDirName=C:\Program Files\R5Reloaded
DefaultGroupName=R5Reloaded
UninstallDisplayIcon={app}\R5R Launcher\launcher.exe
Compression=lzma2
SolidCompression=yes
OutputDir=..\bin\Publish
PrivilegesRequired=admin
UninstallFilesDir={app}\R5R Launcher\
SetupIconFile=..\Assets\launcher_x64.ico
AppPublisher=R5Reloaded
AppPublisherURL=https://r5reloaded.com
OutputBaseFilename=R5RLauncher-Setup
UsePreviousGroup=no
UsePreviousAppDir=no

[Files]
Source: "{tmp}\launcher.exe"; DestDir: "{app}\R5R Launcher\"; Flags: external

[Icons]
Name: "{group}\R5Reloaded"; Filename: "{app}\R5R Launcher\launcher.exe"

[Dirs]
Name: "{app}"; Permissions: users-full
Name: "{app}\R5R Launcher"; Permissions: users-full
Name: "{app}\R5R Library"; Permissions: users-full

[Code]
var
  DownloadPage: TDownloadWizardPage;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded file to {tmp}: %s', [FileName]));
  Result := True;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if CurPageID = wpReady then begin
    DownloadPage.Clear;
    DownloadPage.Add('http://cdn.r5r.org/launcher/launcher.exe', 'launcher.exe', '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
        Result := True;
      except
        SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
        Result := False;
      end;
    finally
      DownloadPage.Hide;
    end;
  end else
    Result := True;
end;