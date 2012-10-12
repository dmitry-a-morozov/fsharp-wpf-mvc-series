
open System
open System.Windows
open Mvc.Wpf.Sample
open Mvc.Wpf

[<STAThread>] 
//[<EntryPoint>]
//let main _ = 
do
    let view = MainView()
    let stopWatch = StopWatchObservable(TimeSpan.FromSeconds(15.))
    let stopWatchController(runningTime : TimeSpan) = Sync <| fun(model : MainModel) -> model.RunningTime <- runningTime

    let controller = 
        MainController(view, stopWatch)
            .Compose(stopWatchController, stopWatch)
            .Compose(CalculatorController(), CalculatorView(view.Control.Calculator), fun m -> m.Calculator)
            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)
            <+> (StockPricesChartController(), StockPricesChartView(view.Control.StockPricesChart), fun m -> m.StockPricesChart)

    //Application().Run(view.Control, controller)

    controller |> Controller.start |> ignore
