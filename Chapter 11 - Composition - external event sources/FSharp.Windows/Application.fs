namespace Mvc.Wpf

open System.Runtime.CompilerServices
open System.Windows
open System.Threading

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<Extension>]
module Application = 

    [<Extension>] //for C#
    let AttachController(this : Application, model, mainWindow, controller : SupervisingController<_, _>) =
        let cts = new CancellationTokenSource()
        this.Startup.Add <| fun _ ->
            this.MainWindow <- mainWindow 
            Async.StartImmediate(
                controller.AsyncStart model |> Async.Ignore,
                cts.Token
            )     
        this.Exit.Add(fun _ -> cts.Cancel())

    type Application with 
        member this.Run(model, mainWindow, controller) =
            AttachController(this, model, mainWindow, controller)
            this.Run mainWindow


            

