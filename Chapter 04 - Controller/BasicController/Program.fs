
open System
open System.Windows
open FSharp.Windows
open FSharp.Windows.Sample

[<STAThread>] 
do 
    let model = SampleModel.Create()
    let view = SampleView()
    let eventHandler = SampleController().EventHandler
    let mvc = Mvc(model, view, eventHandler)
    mvc.Start() |> ignore
    Application().Run view.Window |> ignore