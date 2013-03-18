namespace SampleApp

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Threading
open System.Threading.Tasks
open System.IO
open Microsoft.FSharp.Reflection
open FSharp.Windows
open SampleApp.Services
open SampleApp

type Operations =
    | Add
    | Subtract
    | Multiply
    | Divide

    override this.ToString() = sprintf "%A" this

[<AbstractClass>]
type MainModel() = 
    inherit Model()

    abstract AvailableOperations : Operations[] with get, set
    abstract SelectedOperation : Operations with get, set
    abstract X : int with get, set
    abstract Y : int with get, set
    abstract Result : int with get, set

    abstract Celsius : float with get, set
    abstract Fahrenheit : float with get, set
    abstract TempConverterHeader : string with get, set
    abstract Delay : int with get, set

    abstract Title : string with get, set

type MainEvents = 
    | Calculate
    | Clear 
    | CelsiusToFahrenheit
    | FahrenheitToCelsius
    | CancelAsync
    | Kaboom

type MainView(control) =
    inherit View<MainEvents, MainModel, MainPage>(control)

    override this.EventStreams = 
        [
            this.Control.Calculate, Calculate
            this.Control.Clear, Clear
            this.Control.CelsiusToFahrenheit, CelsiusToFahrenheit
            this.Control.FahrenheitToCelsius, FahrenheitToCelsius
            this.Control.CancelAsync, CancelAsync
            this.Control.Kaboom, Kaboom
        ]
        |> List.map(fun(button, value) -> button.Click |> Observable.mapTo value)

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Control.Operation.ItemsSource <- model.AvailableOperations 
            @>

        Binding.TwoWay 
            <@ 
                this.Control.Operation.SelectedItem <- model.SelectedOperation
                this.Control.X.Text <- string model.X
                this.Control.Y.Text <- string model.Y 
                this.Control.Result.Text <- string model.Result 

                //this.Control.TempConverterGroup.Header <- model.TempConverterHeader
                this.Control.Celsius.Text <- string model.Celsius
                this.Control.Fahrenheit.Text <- string model.Fahrenheit
                this.Control.Delay.Text <- string model.Delay

            @>

        //this.Control.SetBinding(Window.TitleProperty, "Title") |> ignore
         
type MainController() = 
    inherit Controller<MainEvents, MainModel>()

    let service = new TempConvertSoapClient(endpointConfigurationName = "TempConvertSoap")

    override this.InitModel model = 
        model.AvailableOperations <- 
            typeof<Operations>
            |> FSharpType.GetUnionCases
            |> Array.map(fun x -> FSharpValue.MakeUnion(x, [||]))
            |> Array.map unbox
        model.SelectedOperation <- Operations.Add

        model.TempConverterHeader <- "Async TempConveter"
        model.Delay <- 3

    override this.Dispatcher = function
        | Calculate -> Sync this.Calculate
        | Clear -> Sync this.Clear
        | CelsiusToFahrenheit -> Async this.CelsiusToFahrenheit
        | FahrenheitToCelsius -> Async(fun model -> 
            let context = SynchronizationContext.Current
            Async.TryCancelled(
                computation = this.FahrenheitToCelsius model,
                compensation = fun error -> 
                    context.Post((fun _ -> model.TempConverterHeader <- "Async TempConverter. Request cancelled."), null) 
            ))
        | CancelAsync -> Sync(ignore >> Async.CancelDefaultToken)
        | Kaboom -> Sync(fun _  -> failwith "Kaboom !")

    member this.Clear model = 
        model.X <- 0
        model.Y <- 0
        model.Result <- 0

    member this.Calculate model = 
        model.ClearAllErrors()
        match model.SelectedOperation with
        | Add -> 
            model |> Validation.positive <@ fun m -> m.Y @>
            if not model.HasErrors
            then 
                model.Result <- model.X + model.Y
        | Subtract -> 
            model |> Validation.positive <@ fun m -> m.Y @>
            if not model.HasErrors
            then 
                model.Result <- model.X - model.Y
        | Multiply -> 
            model.Result <- model.X * model.Y
        | Divide -> 
            if model.Y = 0 
            then
                model |> Validation.setError <@ fun m -> m.Y @> "Attempted to divide by zero."
            else
                model.Result <- model.X / model.Y

    member this.CelsiusToFahrenheit model = 
        async {
            let context = SynchronizationContext.Current
            use! cancelHandler = Async.OnCancel(fun() -> 
                context.Post((fun _ -> model.TempConverterHeader <- "Async TempConverter. Request cancelled."), null)) 
            model.TempConverterHeader <- "Async TempConverter. Waiting for response ..."            
            do! Async.Sleep(model.Delay * 1000)
            let! fahrenheit = service.AsyncCelsiusToFahrenheit model.Celsius
            do! Async.SwitchToContext context
            model.TempConverterHeader <- "Async TempConverter. Response received."            
            model.Fahrenheit <- fahrenheit
        }

    member this.FahrenheitToCelsius model = 
        async {
                let context = SynchronizationContext.Current
                model.TempConverterHeader <- "Async TempConverter. Waiting for response ..."            
                do! Async.Sleep(model.Delay * 1000)
                let! celsius = service.AsyncFahrenheitToCelsius model.Fahrenheit
                do! Async.SwitchToContext context
                model.TempConverterHeader <- "Async TempConverter. Response received."            
                model.Celsius <- celsius
        }

                
