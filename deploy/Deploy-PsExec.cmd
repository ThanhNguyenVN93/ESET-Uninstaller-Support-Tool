@echo off
REM ============================================================================
REM  Trien khai hang loat bang PsExec (Sysinternals) - IT chu dong chay tren
REM  cac may DA DUOC UY QUYEN. Mass deployment via PsExec on AUTHORIZED machines.
REM ----------------------------------------------------------------------------
REM  CANH BAO / WARNING:
REM  - Chi chay tren may ban DUOC PHEP quan tri. Only on machines you may manage.
REM  - Lenh nay se khoi dong lai may tu xa vao Safe Mode. It WILL reboot them.
REM  - Can PsExec: https://learn.microsoft.com/sysinternals/downloads/psexec
REM ============================================================================

REM  Danh sach may / target computers (hoac dung @hosts.txt):
set "TARGETS=PC-01 PC-02 PC-03"

REM  Duong dan tool tren share / tool path on share:
set "SHARE=\\server\share\ESET Uninstaller Support Tool.exe"

REM  Copy tool ve may dich roi chay --auto (vi du / example):
for %%M in (%TARGETS%) do (
    echo === %%M ===
    copy /Y "%SHARE%" "\\%%M\C$\Windows\Temp\ESETUnins.exe"
    psexec \\%%M -s -h "C:\Windows\Temp\ESETUnins.exe" --auto
)

REM  Ghi chu / Notes:
REM  -s = chay bang tai khoan SYSTEM (quyen cao) / run as SYSTEM
REM  -h = dung token nang quyen / elevated token
REM  Them --select de chon san pham, hoac --password=... de bat autologon.
REM  Add --select to pick a product, or --password=... to enable autologon.
REM  Exit code: 0=go xong, 1=con ESET, 2=loi, 3=khong co ESET.
