
open System
open System.Diagnostics
open System.Windows
open FSharp.Windows.Sample
open FSharp.Windows
open System.Reactive.Linq

[<STAThread>] 
[<EntryPoint>]
let main _ = 
    let stopWatch = StopWatchObservable(frequency = TimeSpan.FromSeconds(1.), failureFrequencyInSeconds = 5.)
    let stopWatchController = Controller.Create(fun (runningTime : TimeSpan) (model : MainModel) -> 
        model.RunningTime <- runningTime)

    let rec safeStopWatchEventSource() = Observable.Catch(stopWatch, fun(exn : exn)-> Debug.WriteLine exn.Message; safeStopWatchEventSource()) 

    let view = MainView()
    let mvc = 
        Mvc(MainModel(), view, MainController(stopWatch))
            .Compose(stopWatchController, safeStopWatchEventSource())
            <+> (CalculatorController(), CalculatorView(view.Control.Calculator), fun m -> m.Calculator)
            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)
            <+> (StockPricesChartController(), StockPricesChartView(view.Control.StockPricesChart), fun m -> m.StockPricesChart)

    let app = Application()
    app.DispatcherUnhandledException.Add <| fun args ->
        Debug.Fail("DispatcherUnhandledException handler", string args.Exception)

    app.Run(mvc, mainWindow = view.Control)
