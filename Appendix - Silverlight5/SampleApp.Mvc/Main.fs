namespace SampleApp

open System
open System.Diagnostics
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Browser
open FSharp.Windows

[<AbstractClass>]
type MainModel() = 
    inherit Model()

    abstract Calculator : CalculatorModel with get, set
    abstract TempConveter : TempConveterModel with get, set

//    abstract ProcessName : string with get, set
//    abstract ActiveTab : string with get, set
//    member this.Title = sprintf "%s - %s" this.ProcessName this.ActiveTab
    abstract Title : string with get, set

    abstract RunningTime : TimeSpan with get, set
    abstract Paused : bool with get, set
    abstract Fail : bool with get, set

type MainEvents = 
    | ActiveTabChanged of string
    | StopWatch
    | StartWatch
    | RestartWatch
    | StartFailingWatch
    | StopFailingWatch

type MainView(control) as this = 
    inherit View<MainEvents, MainModel, MainWindow>(control)

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
//        let titleBinding = MultiBinding(StringFormat = "{0} - {1}")
//        titleBinding.Bindings.Add <| Binding("ProcessName")
//        titleBinding.Bindings.Add <| Binding("ActiveTab")
//        this.Control.SetBinding(Window.TitleProperty, titleBinding) |> ignore

        Binding.FromExpression 
            <@ 
                this.Control.Header.Text <- model.Title

                this.Control.PauseWatch.IsChecked <- Nullable model.Paused
                this.Control.Fail.IsChecked <- Nullable model.Fail
                //pause.IsChecked <- Nullable model.Paused 
                //fail.IsChecked <- Nullable model.Fail
                this.Control.RunningTime.Text <- String.Format("Running time: {0:hh\:mm\:ss}", model.RunningTime)
                this.Control.RestartWatch.IsEnabled <- not model.Fail
             @>

type MainController(stopWatch : StopWatchObservable) = 
    inherit Controller<MainEvents, MainModel>()

    override this.InitModel model = 
//        model.ProcessName <- HtmlPage.BrowserInformation.ProductName;
//        model.ActiveTab <- "Calculator"
        model.Title <- sprintf "%s - %s" HtmlPage.BrowserInformation.ProductName "Calculator"

        model.RunningTime <- TimeSpan.Zero
        model.Paused <- false
        model.Fail <- false

        model.Calculator <- Model.Create()
        model.TempConveter <- Model.Create()

    override this.Dispatcher = Sync << function
        | ActiveTabChanged header -> this.ActiveTabChanged header
        | StopWatch -> ignore >> stopWatch.Pause
        | StartWatch -> ignore >> stopWatch.Start
        | RestartWatch -> this.RestartWatch
        | StartFailingWatch -> fun _ -> stopWatch.GenerateFailures <- true
        | StopFailingWatch -> fun _ -> stopWatch.GenerateFailures <- false

    member this.ActiveTabChanged header model =
        //model.ActiveTab <- header
        model.Title <- sprintf "%s - %s" HtmlPage.BrowserInformation.ProductName header

    member this.RestartWatch model =
        stopWatch.Restart()
        model.Paused <- false

    static member Mvc(mainWindow : MainWindow) =
        let stopWatch = StopWatchObservable(frequency = TimeSpan.FromSeconds(1.), failureFrequencyInSeconds = 5.)
        let stopWatchController = Controller.Create(fun (runningTime : TimeSpan) (model : MainModel) -> 
            model.RunningTime <- runningTime)

        let view = MainView mainWindow
        Mvc(MainModel.Create(), view, MainController(stopWatch))
            .Compose(stopWatchController, stopWatch)
            <+> (CalculatorController(), CalculatorView(view.Control.Calculator), fun m -> m.Calculator)
            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)
        
