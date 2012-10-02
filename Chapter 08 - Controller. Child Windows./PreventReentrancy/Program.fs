
open System
open System.Windows
open Mvc.Wpf.Sample

[<STAThread>] 
do 
    let view = SampleView()
    let controller = SimpleController(view)
    controller.Start() |> ignore
