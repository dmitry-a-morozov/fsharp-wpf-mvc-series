namespace FSharp.Windows

open System
open System.Windows
open System.Runtime.CompilerServices

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<Extension>]
module Application = 

    [<Extension>] //for C#
    let AttachMvc(this : Application, mvc : Mvc<_, _>) = 
        this.Startup.Add <| fun _ -> 
            assert(this.MainWindow <> null)
            let eventProcessing = mvc.Start()
            this.MainWindow.Closed.Add <| fun _ -> eventProcessing.Dispose()

    type Application with 
        member this.Run(mvc, mainWindow : #Window) =
            AttachMvc(this, mvc)
            this.Run mainWindow


            

