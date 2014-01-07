
open System
open System.Windows
open FSharp.Windows
open FSharp.Windows.Sample

[<STAThread>] 
do 
    let model = SampleModel.Create()
    let view = SampleView()
    let controller = SampleController()
    let eventLoop = Mvc.start model view controller
    Application().Run view.Window |> ignore
