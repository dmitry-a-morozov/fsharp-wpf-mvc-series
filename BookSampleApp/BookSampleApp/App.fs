module App

open System
open System.Windows

[<STAThread>]
[<EntryPoint>]
let main _ = 
    let forceLoad = typeof<System.Windows.Forms.Integration.WindowsFormsHost>
    let mainWindow = Uri("/Mainwindow.xaml", UriKind.Relative) |> Application.LoadComponent |> unbox<Window>
    let model, view, controller = MainModel(), MainView mainWindow, MainContoller()
    use eventLoop =  Mvc.start model view controller
    Application().Run mainWindow

