
open System
open System.Windows
open Mvc.Wpf.Sample
open Mvc.Wpf

[<STAThread>] 
[<EntryPoint>]
let main _ = 
    let stopWatch = StopWatchObservable(frequency = TimeSpan.FromSeconds(1.))
    let stopWatchController(runningTime : TimeSpan) = 
        Sync <| fun(model : MainModel) -> model.RunningTime <- runningTime

    let view = MainView()
    let controller = 
        MainController(view, stopWatch)
            .Compose(stopWatchController, stopWatch)
            <+> (CalculatorController(), CalculatorView(view.Control.Calculator), fun m -> m.Calculator)
            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)
            <+> (StockPricesChartController(), StockPricesChartView(view.Control.StockPricesChart), fun m -> m.StockPricesChart)

    let app = Application()
    app.DispatcherUnhandledException.Add <| fun args ->
        let why = match args.Exception with | PreserveStackTraceWrapper e -> e | e -> e
        () //some catch all error-handling logic here

    app.Run(Model.Create(), view.Control, controller)
