; Let other windows crap start
Sleep 5000

; infinite loop
Loop {
  ; Run the program and remember ProcessID in PID
    Run D:\Exploded_02_Win\ExplodedViews.exe,,,PID

    ; take the mouse pointer out of the way
    MouseMove 2048, 768, 0

    ; Wait for our window to appear
    WinWait ahk_pid %PID%
    ; Activate us (necessary for the game to run)
    ; .... TODO toggle "run in background" in unity and build new exe
    WinActivate
    ; make our window aways on top of others
    WinSet AlwaysOnTop, On, ahk_pid %PID%
    ; hide title bar, border and other crap
    WinSet Style, -0xC40000, ahk_pid %PID%
    ; "Make it so, Number One"
    WinSet Redraw, , ahk_pid %PID%
    ; Resize the window to cover the holes left by the hidden crap
    WinMove ahk_pid %PID%, ,0, 0, 2048, 768

    ; wait for the exe to crash or exit
    WinWaitClose ahk_pid %PID%

    ; ask if the whole deal should be repeated
    MsgBox, 4,, Restart Exploded Views?
    ; if user refuses - break out of the infinite loop
    IfMsgBox No
      break
}
