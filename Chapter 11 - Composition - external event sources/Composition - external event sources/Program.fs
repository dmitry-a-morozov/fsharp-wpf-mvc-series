
open System
open System.Diagnostics
open System.Windows
open FSharp.Windows.Sample
open FSharp.Windows

[<STAThread>] 
[<EntryPoint>]
let main _ = 
    let stopWatch = StopWatchObservable(frequency = TimeSpan.FromSeconds(1.), failureFrequencyInSeconds = 5.)
    let stopWatchController = Controller.Create(fun (runningTime : TimeSpan) (model : MainModel) -> 
        model.RunningTime <- runningTime)

    let view = MainView()
    let mvc = 
        Mvc(MainModel.Create(), view, MainController(stopWatch))
            .Compose(stopWatch, stopWatchController, fun exn -> Debug.WriteLine(string exn))
            <+> ((fun m -> m.Calculator), CalculatorView(view.Control.Calculator), CalculatorController())
            <+> ((fun m -> m.TempConveter), TempConveterView(view.Control.TempConveterControl), TempConveterController())
            <+> ((fun m -> m.StockPricesChart), StockPricesChartView(view.Control.StockPricesChart), StockPricesChartController())

    let app = Application()
    app.DispatcherUnhandledException.Add <| fun args ->
        Debug.Fail("DispatcherUnhandledException handler", string args.Exception)

    app.Run(mvc, mainWindow = view.Control)
