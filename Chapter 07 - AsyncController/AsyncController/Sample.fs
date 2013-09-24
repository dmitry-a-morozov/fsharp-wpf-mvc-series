namespace FSharp.Windows.Sample

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Threading
open System.Threading.Tasks
open System.IO
open Microsoft.FSharp.Reflection
open FSharp.Windows
open CSharpWindow.TempConverter

type Operations =
    | Add
    | Subtract
    | Multiply
    | Divide

    override this.ToString() = sprintf "%A" this

[<AbstractClass>]
type SampleModel() = 
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

type SampleEvents = 
    | Calculate
    | Clear 
    | CelsiusToFahrenheit
    | FahrenheitToCelsius
    | CancelAsync
    | Kaboom

type SampleView() =
    inherit View<SampleEvents, SampleModel, SampleWindow>()

    override this.EventStreams = 
        [
            this.Window.Calculate, Calculate
            this.Window.Clear, Clear
            this.Window.CelsiusToFahrenheit, CelsiusToFahrenheit
            this.Window.FahrenheitToCelsius, FahrenheitToCelsius
            this.Window.CancelAsync, CancelAsync
            this.Window.Kaboom, Kaboom
        ]
        |> List.map(fun(button, value) -> button.Click |> Observable.mapTo value)

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Window.Operation.ItemsSource <- model.AvailableOperations 
                this.Window.Operation.SelectedItem <- model.SelectedOperation
                this.Window.X.Text <- string model.X
                this.Window.Y.Text <- string model.Y 
                this.Window.Result.Text <- string model.Result 

                this.Window.TempConverterGroup.Header <- model.TempConverterHeader
                this.Window.Celsius.Text <- string model.Celsius
                this.Window.Fahrenheit.Text <- string model.Fahrenheit
                this.Window.Delay.Text <- string model.Delay

            @>

        this.Window.SetBinding(Window.TitleProperty, "Title") |> ignore
         
type SampleController() = 
    inherit AsyncInitController<SampleEvents, SampleModel>()

    let service = new TempConvertSoapClient(endpointConfigurationName = "TempConvertSoap")

    override this.InitModel(model : SampleModel) = 
        model.AvailableOperations <- 
            typeof<Operations>
            |> FSharpType.GetUnionCases
            |> Array.map(fun x -> FSharpValue.MakeUnion(x, [||]))
            |> Array.map unbox
        model.SelectedOperation <- Operations.Add

        model.TempConverterHeader <- "Async TempConveter"
        model.Delay <- 3

        let folderToSearch = Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles
        model.Title <- sprintf "Files in %s: ..." folderToSearch

        async {
            try 
                let context = SynchronizationContext.Current
                do! Async.SwitchToThreadPool()
                let totalFiles = Directory.GetFiles(folderToSearch, "*.*", SearchOption.AllDirectories).Length
                do! Async.SwitchToContext context
                model.Title <- sprintf "Files in %s: - %i" folderToSearch totalFiles
            with e ->
                System.Diagnostics.Debug.WriteLine e.Message
                model.Title <- sprintf "Failed to count files in in %s." folderToSearch 
        }

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

                
