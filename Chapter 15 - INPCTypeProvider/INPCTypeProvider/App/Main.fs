namespace FSharp.Windows.Sample

open System
open System.Diagnostics
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open FSharp.Windows
open FSharp.Windows.UIElements

type MainEvents = 
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
            yield this.Control.RestartWatch.Click |> Observable.mapTo RestartWatch
            yield pause.Checked |> Observable.mapTo StopWatch
            yield pause.Unchecked |> Observable.mapTo StartWatch
            yield fail.Checked |> Observable.mapTo StartFailingWatch
            yield fail.Unchecked |> Observable.mapTo StopFailingWatch
        ]

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Control.Tabs.SelectedItem <- model.ActiveTab
                this.Control.Title <- model.Title
                pause.IsChecked <- Nullable model.Paused 
                fail.IsChecked <- Nullable model.Fail
                this.Control.RunningTime.Text <- String.Format("Running time: {0:hh\:mm\:ss}", model.RunningTime)
                this.Control.RestartWatch.IsEnabled <- not model.Fail
             @>

type MainController(stopWatch : StopWatchObservable) = 
    inherit Controller<MainEvents, MainModel>()

    override this.InitModel model = 
        model.ProcessName <- Process.GetCurrentProcess().ProcessName
        model.RunningTime <- TimeSpan.Zero
        model.Paused <- false
        model.Fail <- false

        model.Calculator <- CalculatorModel()
        model.TempConveter <- TempConveterModel()
        model.StockPricesChart <- StockPricesChartModel()

    override this.Dispatcher = Sync << function
        | StopWatch -> ignore >> stopWatch.Pause
        | StartWatch -> ignore >> stopWatch.Start
        | RestartWatch -> this.RestartWatch
        | StartFailingWatch -> fun _ -> stopWatch.GenerateFailures <- true
        | StopFailingWatch -> fun _ -> stopWatch.GenerateFailures <- false

    member this.RestartWatch model =
        stopWatch.Restart()
        model.Paused <- false
