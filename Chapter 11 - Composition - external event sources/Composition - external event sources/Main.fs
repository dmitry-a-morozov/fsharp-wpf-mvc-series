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
    abstract ActiveTab : string with get, set
    abstract RunningTime : TimeSpan with get, set
    abstract Paused : Nullable<bool> with get, set
    abstract Fail : Nullable<bool> with get, set

type MainEvents = 
    | ActiveTabChanged of string
    | StopWatch
    | StartWatch
    | RestartWatch
    | StartFailingWatch
    | StopFailingWatch

type MainView() as this = 
    inherit View<MainEvents, MainModel, MainWindow>()

    let pause = this.Control.PauseWatch
    let fail = this.Control.Fail

    override this.EventStreams = 
        [   
            yield this.Control.Tabs.SelectionChanged |> Observable.map(fun _ -> 
                let activeTab : TabItem = unbox this.Control.Tabs.SelectedItem
                let header = string activeTab.Header
                ActiveTabChanged header)

            yield this.Control.RestartWatch.Click |> Observable.mapTo RestartWatch
            yield pause.Checked |> Observable.mapTo StopWatch
            yield pause.Unchecked |> Observable.mapTo StartWatch
            yield fail.Checked |> Observable.mapTo StartFailingWatch
            yield fail.Unchecked |> Observable.mapTo StopFailingWatch
        ]

    override this.SetBindings model = 
        let titleBinding = MultiBinding(StringFormat = "{0} - {1}")
        titleBinding.Bindings.Add <| Binding("ProcessName")
        titleBinding.Bindings.Add <| Binding("ActiveTab")
        this.Control.SetBinding(Window.TitleProperty, titleBinding) |> ignore

        Binding.FromExpression 
            <@ 
                this.Control.PauseWatch.IsChecked <- model.Paused 
                this.Control.Fail.IsChecked <- model.Fail
            @>
        this.Control.RunningTime.SetBinding(TextBlock.TextProperty, Binding(path = "RunningTime", StringFormat = "Running time: {0:hh\:mm\:ss}")) |> ignore

type MainController(view, stopWatch : StopWatchObservable) = 
    inherit SupervisingController<MainEvents, MainModel>(view)

    override this.InitModel model = 
        model.ProcessName <- Process.GetCurrentProcess().ProcessName
        model.ActiveTab <- "Calculator"
        model.RunningTime <- TimeSpan.Zero
        model.Paused <- Nullable false
        model.Fail <- Nullable false

        model.Calculator <- Model.Create()
        model.TempConveter <- Model.Create()
        model.StockPricesChart <- Model.Create()

    override this.Dispatcher = Sync << function
        | ActiveTabChanged header -> this.ActiveTabChanged header
        | StopWatch -> ignore >> stopWatch.Pause
        | StartWatch -> ignore >> stopWatch.Start
        | RestartWatch -> this.RestartWatch
        | StartFailingWatch -> fun _ -> stopWatch.GenerareFailures <- true
        | StopFailingWatch -> fun _ -> stopWatch.GenerareFailures <- false

    member this.ActiveTabChanged header model =
        model.ActiveTab <- header

    member this.RestartWatch model =
        stopWatch.Restart()
        model.Paused <- Nullable false
    
    override this.OnError why = Debug.WriteLine why.Message
