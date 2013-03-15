namespace FSharp.Windows

open System.Runtime.CompilerServices
open System.Windows
open System.Threading

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<Extension>]
module Application = 

    [<Extension>] //for C#
    let AttachMvc(mvc : Mvc<_, _>) =
        let cts = new CancellationTokenSource()
        Async.StartImmediate(mvc.AsyncStart() |> Async.Ignore, cts.Token)
    
    type Application with 
        member this.Run(mvc, mainWindow) =
            this.Startup.Add <| fun _ -> AttachMvc mvc
            this.Run mainWindow

            

