
open System
open System.Windows
open Mvc.Wpf.Sample
open Mvc.Wpf

[<STAThread>] 
do 
    let view = MainView()
    let stopWatch = StopWatchObservable(TimeSpan.FromSeconds(1.))
    let controller = 
        MainController(view, stopWatch) 
            <+> (CalculatorController(), CalculatorView(view.Control.Calculator), fun m -> m.Calculator)
//            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)
//            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)

    controller |> Controller.start |> ignore
