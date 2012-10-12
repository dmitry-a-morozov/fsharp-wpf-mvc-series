
open System
open System.Windows
open Mvc.Wpf.Sample
open Mvc.Wpf

[<STAThread>] 
do 
    let view = MainView()
    let stopWatch = StopWatchObservable(TimeSpan.FromSeconds(1.))
    let stopWatchController(runningTime : TimeSpan) = Sync <| fun(model : MainModel) -> model.RunningTime <- runningTime

    let controller = 
        MainController(view, stopWatch)
            .Compose(stopWatchController, stopWatch)
            <+> (CalculatorController(), CalculatorView(view.Control.Calculator), fun m -> m.Calculator)
            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)
            <+> (StockPricesChartController(), StockPricesChartView(view.Control.StockPricesChart), fun m -> m.StockPricesChart)

    controller |> Controller.start |> ignore
