namespace Mvc.Wpf

open System.Runtime.CompilerServices
open System.Windows

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<Extension>]
module Application = 

    [<Extension>] //for C#
    let AttachController(this : Application, window : Window, controller : SupervisingController<_, _>) =
        this.Startup.Add <| fun _ ->
            this.MainWindow <- window 
            Model.Create() |> controller.AsyncStart |> Async.Ignore |> Async.StartImmediate

    type Application with 
        member this.Run(window, controller) =
            AttachController(this, window, controller)
            this.Run window


            

