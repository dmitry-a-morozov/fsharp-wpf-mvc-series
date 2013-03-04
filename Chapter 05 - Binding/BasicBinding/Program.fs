
open System
open System.Windows
open FSharp.Windows
open FSharp.Windows.Sample

[<STAThread>] 
do 
    let view = SampleView()
    let controller = SimpleController()
    Mvc.start(view, controller) |> ignore
    Application().Run view.Window |> ignore

(*
    IController introduction
    Controller.fromEventHandler
    Mvc.start with default model instance
*)