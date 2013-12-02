namespace FSharp.Windows.Sample

open System.Threading
open System.Windows.Data
open FSharp.Windows
open FSharp.Windows.UIElements

[<AbstractClass>]
type TempConveterModel() = 
    inherit Model()

    abstract Celsius : float with get, set
    abstract Fahrenheit : float with get, set
    abstract ResponseStatus : string with get, set
    abstract Delay : int with get, set

type TempConveterEvents = 
    | CelsiusToFahrenheit
    | FahrenheitToCelsius
    | CancelAsync

type TempConveterView(control) =
    inherit PartialView<TempConveterEvents, TempConveterModel, TempConverterControl>(control)
   
    override this.EventStreams = 
        [
            this.Control.CelsiusToFahrenheit, CelsiusToFahrenheit
            this.Control.FahrenheitToCelsius, FahrenheitToCelsius
            this.Control.CancelAsync, CancelAsync
        ]
        |> List.ofButtonClicks

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Control.ResponseStatus.Content <- model.ResponseStatus
                this.Control.Celsius.Text <- string model.Celsius
                this.Control.Fahrenheit.Text <- string model.Fahrenheit
                this.Control.Delay.Text <- string model.Delay
            @>

type TempConveterController() = 
    inherit Controller<TempConveterEvents, TempConveterModel>()

    let service = new TempConverter.TempConvertSoapClient(endpointConfigurationName = "TempConvertSoap")

    override this.InitModel model = 
        model.ResponseStatus <- "Async TempConveter"
        model.Delay <- 3

    override this.Dispatcher = function
        | CelsiusToFahrenheit -> Async this.CelsiusToFahrenheit
        | FahrenheitToCelsius -> Async this.FahrenheitToCelsius
        | CancelAsync -> Sync <| fun _ -> Async.CancelDefaultToken()

    member this.CelsiusToFahrenheit model = 
        async {
            let context = SynchronizationContext.Current
            use! cancelHandler = Async.OnCancel(fun() -> 
                context.Post((fun _ -> model.ResponseStatus <- "Async TempConverter. Request cancelled."), null)) 
            model.ResponseStatus <- "Async TempConverter. Waiting for response ..."            
            do! Async.Sleep(model.Delay * 1000)
            let! fahrenheit = service.AsyncCelsiusToFahrenheit model.Celsius
            do! Async.SwitchToContext context
            model.ResponseStatus <- "Async TempConverter. Response received."            
            model.Fahrenheit <- fahrenheit
        }

    member this.FahrenheitToCelsius model = 
        async {
            let context = SynchronizationContext.Current
            use! cancelHandler = Async.OnCancel(fun() -> 
                context.Post((fun _ -> model.ResponseStatus <- "Async TempConverter. Request cancelled."), null)) 
            model.ResponseStatus <- "Async TempConverter. Waiting for response ..."            
            do! Async.Sleep(model.Delay * 1000)
            let! celsius = service.AsyncFahrenheitToCelsius model.Fahrenheit
            do! Async.SwitchToContext context
            model.ResponseStatus <- "Async TempConverter. Response received."            
            model.Celsius <- celsius
        }

