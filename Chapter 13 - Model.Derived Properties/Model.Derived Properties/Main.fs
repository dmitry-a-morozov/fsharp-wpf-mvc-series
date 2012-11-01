namespace Mvc.Wpf.Sample

open System
open System.Diagnostics
open System.Windows
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

    abstract ProcessName : string with get, set
    abstract ActiveTab : TabItem with get, set
    abstract RunningTime : TimeSpan with get, set
    abstract Paused : bool with get, set

    [<NotifyDependencyChanged>]
    member this.Title = sprintf "%s-%O" this.ProcessName this.ActiveTab.Header

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
        Binding.FromExpression 
            <@ 
                this.Control.Tabs.SelectedItem <- model.ActiveTab
                this.Control.Title <- model.Title
                pause.IsChecked <- Nullable model.Paused 
                this.Control.RunningTime.Text <- String.Format("Running time: {0:hh\:mm\:ss}", model.RunningTime)
                this.Control.RestartWatch.IsEnabled <- not model.Paused
            @>

type MainController(view, stopWatch : StopWatchObservable) = 
    inherit SupervisingController<MainEvents, MainModel>(view)

    override this.InitModel model = 
        model.ProcessName <- Process.GetCurrentProcess().ProcessName
        model.RunningTime <- TimeSpan.Zero
        model.Paused <- false

        model.Calculator <- Model.Create()
        model.TempConveter <- Model.Create()
        model.StockPricesChart <- Model.Create()

    override this.Dispatcher = Sync << function
        | StopWatch -> ignore >> stopWatch.Pause
        | StartWatch -> ignore >> stopWatch.Start
        | RestartWatch -> this.RestartWatch

    member this.RestartWatch model =
        stopWatch.Restart()
        model.Paused <- false

