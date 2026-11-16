@echo off
REM ============================================================================
REM  ESET Uninstaller Support Tool - chay tu NETWORK SHARE (nguoi dung tu chay)
REM  Run from a NETWORK SHARE (user double-clicks or runs this .cmd)
REM ----------------------------------------------------------------------------
REM  App se tu copy chinh no ve o dia cuc bo (%ProgramData%\frmesetunins) truoc
REM  khi vao Safe Mode, vi Safe Mode minimal KHONG co mang. Khong can lam gi them.
REM  The app self-copies to local disk before Safe Mode (which has no network).
REM
REM  Yeu cau / Requirements: chay bang quyen Administrator (app.manifest da ep UAC)
REM ============================================================================

set "TOOL=%~dp0ESET Uninstaller Support Tool.exe"

REM --- Che do co giao dien (user bam nut) / Interactive GUI mode:
start "" "%TOOL%"

REM --- Che do khong giam sat / Unattended (bo dau REM o dong duoi de dung):
REM "%TOOL%" --auto
REM "%TOOL%" --auto --password=TheWindowsPassword
REM "%TOOL%" --auto --select
