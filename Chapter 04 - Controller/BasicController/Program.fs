
open System
open System.Windows
open FSharp.Windows
open FSharp.Windows.Sample

[<STAThread>] 
do 
    let model = SampleModel.Create()
    let view = SampleView()
    let controller = SampleController()
    let mvc = Mvc(model, view, controller)
    mvc.Start() |> ignore
    Application().Run view.Window |> ignore
