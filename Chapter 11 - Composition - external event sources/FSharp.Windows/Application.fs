namespace FSharp.Windows

open System.Runtime.CompilerServices
open System.Windows

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<Extension>]
module Application = 

    [<Extension>] //for C#
    let StartImmediate(mvc : Mvc<_, _>) =
        mvc.AsyncStart() |> Async.Ignore |> Async.StartImmediate
    
    type Application with 
        member this.Run(mvc, mainWindow) =
            this.Startup.Add <| fun _ -> StartImmediate mvc
            this.Run mainWindow


            

