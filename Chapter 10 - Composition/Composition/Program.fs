
open System
open System.Windows
open Mvc.Wpf.Sample
open Mvc.Wpf

[<STAThread>] 
do 
    let view = MainView()
    let stopWatch = StopWatchObservable(TimeSpan.FromSeconds(1.))
    let controller = MainController(view, stopWatch)
    controller |> Controller.start |> ignore
