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
    abstract Paused : bool with get, set

    [<NotifyDependencyChanged>]
    member this.Title = sprintf "%s-%s" this.ProcessName this.ActiveTab

type MainEvents = 
    | ActiveTabChanged of string
    | StopWatch
    | StartWatch
    | RestartWatch

type MainView() as this = 
    inherit View<MainEvents, MainModel, MainWindow>()

    let pause = this.Control.PauseWatch

    override this.EventStreams = 
        [   
            yield this.Control.Tabs.SelectionChanged |> Observable.map(fun _ -> 
                let activeTab : TabItem = unbox this.Control.Tabs.SelectedItem
                let header = string activeTab.Header
                ActiveTabChanged header)

            yield this.Control.RestartWatch.Click |> Observable.mapTo RestartWatch
            yield pause.Checked |> Observable.mapTo StopWatch
            yield pause.Unchecked |> Observable.mapTo StartWatch
        ]

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Control.Title <- model.Title
                pause.IsChecked <- Nullable model.Paused 
                this.Control.RunningTime.Text <- String.Format("Running time: {0:hh\:mm\:ss}", model.RunningTime)
                this.Control.RestartWatch.IsEnabled <- not model.Paused
            @>

type MainController(view, stopWatch : StopWatchObservable) = 
    inherit SupervisingController<MainEvents, MainModel>(view)

    override this.InitModel model = 
        model.ProcessName <- Process.GetCurrentProcess().ProcessName
        model.ActiveTab <- "Calculator"
        model.RunningTime <- TimeSpan.Zero
        model.Paused <- false

        model.Calculator <- Model.Create()
        model.TempConveter <- Model.Create()
        model.StockPricesChart <- Model.Create()

    override this.Dispatcher = Sync << function
        | ActiveTabChanged header -> this.ActiveTabChanged header
        | StopWatch -> ignore >> stopWatch.Pause
        | StartWatch -> ignore >> stopWatch.Start
        | RestartWatch -> this.RestartWatch

    member this.ActiveTabChanged header model =
        model.ActiveTab <- header

    member this.RestartWatch model =
        stopWatch.Restart()
        model.Paused <- false

    override this.OnError why = 
        System.Windows.MessageBox.Show(why.ToString(), "Error!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error) |> ignore
