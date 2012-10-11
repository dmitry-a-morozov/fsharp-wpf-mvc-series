namespace Mvc.Wpf.Sample

open System
open System.Windows.Controls
open System.Windows.Data
open System.Threading
open System.Collections.Generic
open System.Drawing
open System.Windows.Forms.DataVisualization.Charting
open System.Collections.ObjectModel

open Microsoft.FSharp.Reflection

open Mvc.Wpf

open CSharpWindow.TempConverter
open Mvc.Wpf.UIElements

type Operations =
    | Add
    | Subtract
    | Multiply
    | Divide

    override this.ToString() = sprintf "%A" this

    static member Values = 
        typeof<Operations>
        |> FSharpType.GetUnionCases
        |> Array.map(fun x -> FSharpValue.MakeUnion(x, [||]))
        |> Array.map unbox<Operations>

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

    abstract StockPrices : ObservableCollection<string * decimal> with get, set

type SampleEvents = 
    | Calculate
    | Clear 
    | CelsiusToFahrenheit
    | FahrenheitToCelsius
    | CancelAsync
    | Hex1
    | Hex2
    | AddStockToPriceChart
    | YChanged of string

type SampleView() as this =
    inherit View<SampleEvents, SampleModel, MainWindow>()

    do 
        let area = new ChartArea() 
        area.AxisX.MajorGrid.LineColor <- Color.LightGray
        area.AxisY.MajorGrid.LineColor <- Color.LightGray        
        this.Control.StockPricesChart.ChartAreas.Add area
        let series = 
            new Series(
                ChartType = SeriesChartType.Column, 
                Palette = ChartColorPalette.EarthTones, 
                XValueMember = "Item1", 
                YValueMembers = "Item2")
        this.Control.StockPricesChart.Series.Add series
    
    override this.EventStreams = 
        [ 
            yield! [
                this.Control.Calculate, Calculate
                this.Control.Clear, Clear
                this.Control.CelsiusToFahrenheit, CelsiusToFahrenheit
                this.Control.FahrenheitToCelsius, FahrenheitToCelsius
                this.Control.CancelAsync, CancelAsync
                this.Control.Hex1, Hex1
                this.Control.Hex2, Hex2
                this.Control.AddStock, AddStockToPriceChart
            ]
            |> List.ofButtonClicks
                 
            yield this.Control.Y.TextChanged |> Observable.map(fun _ -> YChanged(this.Control.Y.Text))
        ] 

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Control.Operation.ItemsSource <- model.AvailableOperations 
                this.Control.Operation.SelectedItem <- model.SelectedOperation
                this.Control.X.Text <- string model.X
                this.Control.Y.Text <- string model.Y 
                this.Control.Result.Text <- string model.Result 

                this.Control.TempConverterGroup.Header <- model.TempConverterHeader
                this.Control.Celsius.Text <- string model.Celsius
                this.Control.Fahrenheit.Text <- string model.Fahrenheit
                this.Control.Delay.Text <- string model.Delay
            @>

        this.Control.StockPricesChart.DataSource <- model.StockPrices
        model.StockPrices.CollectionChanged.Add(fun _ -> this.Control.StockPricesChart.DataBind())

type SimpleController(view) = 
    inherit Controller<SampleEvents, SampleModel>(view)

    let service = new TempConvertSoapClient(endpointConfigurationName = "TempConvertSoap")

    override this.InitModel model = 
        model.AvailableOperations <- Operations.Values |> Array.filter(fun op -> op <> Operations.Divide)
        model.SelectedOperation <- Operations.Add
        model.X <- 0
        model.Y <- 0
        model.Result <- 0

        model.TempConverterHeader <- "Async TempConveter"
        model.Delay <- 3

        model.StockPrices <- ObservableCollection()

    override this.Dispatcher = function
        | Calculate -> Sync this.Calculate
        | Clear -> Sync this.InitModel
        | CelsiusToFahrenheit -> Async this.CelsiusToFahrenheit
        | FahrenheitToCelsius -> Async this.FahrenheitToCelsius
        | CancelAsync -> Sync(ignore >> Async.CancelDefaultToken)
        | Hex1 -> Sync this.Hex1
        | Hex2 -> Sync this.Hex2
        | AddStockToPriceChart -> Async this.AddStockToPriceChart
        | YChanged text -> Sync(this.YChanged text)

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
            use! cancelHandler = Async.OnCancel(fun() -> 
                context.Post((fun _ -> model.TempConverterHeader <- "Async TempConverter. Request cancelled."), null)) 
            model.TempConverterHeader <- "Async TempConverter. Waiting for response ..."            
            do! Async.Sleep(model.Delay * 1000)
            let! celsius = service.AsyncFahrenheitToCelsius model.Fahrenheit
            do! Async.SwitchToContext context
            model.TempConverterHeader <- "Async TempConverter. Response received."            
            model.Celsius <- celsius
        }

    member this.Hex1 model = 
        let view = HexConverter.view()
        let controller = HexConverter.controller view
        let childModel : HexConverter.Model = Model.Create() 
        childModel.Value <- model.X

        if controller.Start childModel
        then 
            model.X <- childModel.Value 

    member this.Hex2 model = 
        HexConverter.view()
        |> HexConverter.controller 
        |> Controller.start
        |> Option.iter(fun resultModel ->
            model.Y <- resultModel.Value 
        )

    member this.AddStockToPriceChart model = 
        async {
            let view = StockPriceView()
            let! result = StockPriceController view |> Controller.asyncStart  
            result |> Option.iter (fun stockInfo ->
                model.StockPrices.Add(stockInfo.Symbol, stockInfo.LastPrice)
            )
        }

    member this.YChanged text model = 
        if text <> "0"
        then 
            model.AvailableOperations <- Operations.Values
        else 
            model.AvailableOperations <- Operations.Values |> Array.filter(fun op -> op <> Operations.Divide)

