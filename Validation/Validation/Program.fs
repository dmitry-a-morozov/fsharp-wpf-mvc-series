
open System
open System.Windows
open Mvc.Wpf.Sample

[<STAThread>] 
do 
    let model = SampleModel.Create()
    let view = SampleView()
    let controller = SimpleController(view)
    controller.Start model |> ignore
    Application().Run view.Window |> ignore