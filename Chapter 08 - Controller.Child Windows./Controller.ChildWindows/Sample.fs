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

    abstract StockPrices : ObservableCollection<string * decimal> with get, set

type SampleEvents = 
    | Calculate
    | Clear 
    | CelsiusToFahrenheit
    | FahrenheitToCelsius
    | Hex1
    | Hex2
    | AddStockToPriceChart

type SampleView() as this =
    inherit View<SampleEvents, SampleModel, SampleWindow>()

    do 
        let area = new ChartArea() 
        area.AxisX.MajorGrid.LineColor <- Color.LightGray
        area.AxisY.MajorGrid.LineColor <- Color.LightGray        
        this.Window.StockPricesChart.ChartAreas.Add area
        let series = 
            new Series(
                ChartType = SeriesChartType.Column, 
                Palette = ChartColorPalette.EarthTones, 
                XValueMember = "Item1", 
                YValueMembers = "Item2")
        this.Window.StockPricesChart.Series.Add series
    
    override this.EventStreams = 
        [
            this.Window.Calculate, Calculate
            this.Window.Clear, Clear
            this.Window.CelsiusToFahrenheit, CelsiusToFahrenheit
            this.Window.FahrenheitToCelsius, FahrenheitToCelsius
            this.Window.Hex1, Hex1
            this.Window.Hex2, Hex2
            this.Window.AddStock, AddStockToPriceChart
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
            @>

        this.Window.StockPricesChart.DataSource <- model.StockPrices
        model.StockPrices.CollectionChanged.Add(fun _ -> this.Window.StockPricesChart.DataBind())

type SimpleController(view : IView<_, _>) = 
    inherit Controller<SampleEvents, SampleModel>(view)

    let service = new TempConvertSoapClient(endpointConfigurationName = "TempConvertSoap")

    override this.InitModel model = 
        model.AvailableOperations <- 
            typeof<Operations>
            |> FSharpType.GetUnionCases
            |> Array.map(fun x -> FSharpValue.MakeUnion(x, [||]))
            |> Array.map unbox
        model.SelectedOperation <- Operations.Add
        model.X <- 0
        model.Y <- 0
        model.Result <- 0

        model.TempConverterHeader <- "Async TempConveter"

        model.StockPrices <- ObservableCollection()

    override this.Dispatcher = function
        | Calculate -> Sync this.Calculate
        | Clear -> Sync this.InitModel
        | CelsiusToFahrenheit -> Async this.CelsiusToFahrenheit
        | FahrenheitToCelsius -> Async this.FahrenheitToCelsius
        | Hex1 -> Sync this.Hex1
        | Hex2 -> Sync this.Hex2
        | AddStockToPriceChart -> Async this.AddStockToPriceChart

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
            model.TempConverterHeader <- "Async TempConverter. Waiting for response ..."            
            let context = SynchronizationContext.Current
            let! fahrenheit = service.AsyncCelsiusToFahrenheit model.Celsius
            do! Async.Sleep 5000
            do! Async.SwitchToContext context
            model.TempConverterHeader <- "Async TempConverter. Response received."            
            model.Fahrenheit <- fahrenheit
        }

    member this.FahrenheitToCelsius model = 
        async {
            model.TempConverterHeader <- "Async TempConverter. Waiting for response ..."            
            let context = SynchronizationContext.Current
            let! celsius = service.AsyncFahrenheitToCelsius model.Fahrenheit
            do! Async.Sleep 5000
            do! Async.SwitchToContext context
            model.TempConverterHeader <- "Async TempConverter. Response received."            
            model.Celsius <- celsius
        }

    member this.Hex1 model = 
        let view = HexConverterView()
        let controller = HexConverterController view
        let childModel : HexConverterModel = Model.Create() 
        childModel.SetValue(model.X)

        if controller.Start childModel
        then 
            assert childModel.Value.IsSome
            model.X <- Option.get childModel.Value 

    member this.Hex2 model = 
        let view = HexConverterView()
        let controller = HexConverterController view
        controller.Start()
        |> Option.iter(fun resultModel ->
            assert resultModel.Value.IsSome
            model.Y <- Option.get resultModel.Value 
        )

    member this.AddStockToPriceChart model = 
        async {
            let view = StockPriceView()
            let controller = StockPriceController view
            let! result = controller.AsyncStart()
            result |> Option.iter (fun stockInfo ->
                model.StockPrices.Add(stockInfo.Symbol, stockInfo.LastPrice)
            )
        }