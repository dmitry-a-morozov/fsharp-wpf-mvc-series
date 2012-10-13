namespace Mvc.Wpf.Sample

open System
open System.Windows.Controls
open System.Windows.Data
open Mvc.Wpf
open Mvc.Wpf.UIElements

[<AbstractClass>]
type MainModel() = 
    inherit Model()

    abstract Calculator : CalculatorModel with get, set
    abstract TempConveter : TempConveterModel with get, set
    abstract StockPricesChart : StockPricesChartModel with get, set

    abstract RunningTime : TimeSpan with get, set
    abstract Paused : Nullable<bool> with get, set

type MainEvents = 
    | StopWatch
    | StartWatch
    | RestartWatch

type MainView() as this = 
    inherit View<MainEvents, MainModel, MainWindow>()

    let pause = this.Control.PauseWatch

    override this.EventStreams = 
        [   
            yield this.Control.RestartWatch.Click |> Observable.mapTo RestartWatch
            yield pause.Checked |> Observable.mapTo StopWatch
            yield pause.Unchecked |> Observable.mapTo StartWatch
        ]

    override this.SetBindings model = 
        Binding.FromExpression <@ this.Control.PauseWatch.IsChecked <- model.Paused @>
        this.Control.RunningTime.SetBinding(TextBlock.TextProperty, Binding(path = "RunningTime", StringFormat = "Running time: {0:hh\:mm\:ss}")) |> ignore

type MainController(view, stopWatch : StopWatchObservable) = 
    inherit SupervisingController<MainEvents, MainModel>(view)

    override this.InitModel model = 
        model.RunningTime <- TimeSpan.Zero
        model.Paused <- Nullable false

        model.Calculator <- Model.Create()
        model.TempConveter <- Model.Create()
        model.StockPricesChart <- Model.Create()

    override this.Dispatcher = function
        | StopWatch -> Sync(ignore >> stopWatch.Pause)
        | StartWatch -> Sync(ignore >> stopWatch.Start)
        | RestartWatch -> Sync(this.RestartWatch)

    member this.RestartWatch model =
        stopWatch.Restart()
        model.Paused <- Nullable false

