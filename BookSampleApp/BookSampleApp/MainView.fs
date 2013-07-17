namespace global

open System 
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading
open System.Windows.Forms.DataVisualization.Charting
open System.Drawing
open System.Net
open Microsoft.FSharp.Linq

type MainEvents = 
    | InstrumentInfo
    | PriceUpdate of decimal
    | FlipPosition 
    | StrategyCommand

type MainView(window : Window) = 

    let (?) (window : Window) name = window.FindName name |> unbox

    let symbol : TextBox = window ? Symbol
    let instrumentInfo : Button = window ? InstrumentInfo
    let instrumentName : TextBlock = window ? InstrumentName
    let price : TextBlock = window ? Price
    let livePriceUpdates : CheckBox = window ? LivePriceUpdates

    let positionSize : TextBox = window ? PositionSize
    let flipPosition : Button = window ? FlipPosition
    let positionOpenedAt : TextBlock = window ? OpenedAt
    let positionClosedAt : TextBlock = window ? ClosedAt
    let positionPnL : TextBlock = window ? PnL

    let stopLossAt : TextBox = window ? StopLossAt
    let takeProfitAt : TextBox = window ? TakeProfitAt
    let strategyAction : Button = window ? StrategyAction

    //chart control
    [<Literal>]
    let topNAxisX = 100

    let series = new Series(ChartType = SeriesChartType.FastLine)
    let stopLoss = new StripLine(IntervalOffset = 0., BackColor = Color.FromArgb(32, Color.Red))
    let takeProfit = new StripLine(BackColor = Color.FromArgb(32, Color.Green))

    let priceFeed = DispatcherTimer(Interval = TimeSpan.FromSeconds 0.5)
    let minPrice, maxPrice = ref Decimal.MaxValue, ref Decimal.MinValue
    let mutable nextPrice = fun() -> raise <| NotImplementedException()

    do
        let chart : Chart = window ? Chart

        let area = new ChartArea() 
        chart.ChartAreas.Add area
        area.AxisY.StripLines.Add stopLoss
        area.AxisY.StripLines.Add takeProfit
        area.AxisX.MajorGrid.LineColor <- Color.LightGray
        area.AxisY.MajorGrid.LineColor <- Color.LightGray        
        area.AxisX.LabelStyle.Enabled <- false
        chart.Series.Add series

    //price feed simulation
        livePriceUpdates.Checked |> Event.add (fun _ -> 
            use wc = new WebClient()
            let today = DateTime.Today
            let yearBefore = today.AddYears -1
            let uri = sprintf "http://ichart.finance.yahoo.com/table.csv?s=%s&a=%i&b=%i&c=%i&d=%i&e=%i&f=%i&g=d&ignore=.csv" symbol.Text yearBefore.Month yearBefore.Day yearBefore.Year today.Month today.Day today.Year
            let response = wc.DownloadString uri
            let lines = response.Split('\n')
            let header = lines.[0] in assert (header = "Date,Open,High,Low,Close,Volume,Adj Close")
            let prices = 
                lines.[1.. ] 
                |> Array.filter (not << String.IsNullOrEmpty)
                |> Array.map (fun line -> 
                    let x = line.Split(',').[4] |> decimal
                    minPrice := min !minPrice x
                    maxPrice := max !maxPrice x
                    x 
                )
                |> Array.rev

            area.AxisX.Maximum <- float prices.Length
            area.AxisY.Minimum <- !minPrice |> Math.Floor |> float
            area.AxisY.Maximum <- !maxPrice |> Math.Floor |> float
//            area.AxisY.Interval <- 
//                if !maxPrice < 50M then 5.0
//                elif !maxPrice < 100M then 10.0
//                elif !maxPrice < 250M then 25.0
//                elif !maxPrice < 500M then 50.0
//                else 100.0

            let index = ref 0
            nextPrice <- fun() -> 
                if !index < prices.Length
                then
                    incr index 
                    Some(prices.[!index - 1])
                else 
                    None
        )

    interface IView<MainEvents, MainModel> with
        member this.Subscribe observer = 
            let xs = 
                [
                    instrumentInfo.Click |> Observable.map (fun _ -> InstrumentInfo)
                    priceFeed.Tick |> Observable.choose (fun _ -> 
                        nextPrice() |> Option.map (fun value ->
                            series.Points.AddY value |> ignore
                            PriceUpdate value
                        )
                    )
                    flipPosition.Click |> Observable.map (fun _ -> FlipPosition)
                    strategyAction.Click |> Observable.map (fun _ -> StrategyCommand)
                ] |> List.reduce Observable.merge 
            xs.Subscribe observer

        member this.SetBindings model = 
            window.DataContext <- model

            symbol.SetBinding(TextBox.TextProperty, "Symbol") |> ignore
            instrumentName.SetBinding(TextBlock.TextProperty, Binding(path = "InstrumentName", StringFormat = "Name : {0}")) |> ignore
            price.SetBinding(TextBlock.TextProperty, "Price") |> ignore
            livePriceUpdates.SetBinding(CheckBox.IsCheckedProperty, "LivePriceUpdates") |> ignore
            livePriceUpdates.SetBinding(CheckBox.IsCheckedProperty, Binding("IsEnabled", Mode = BindingMode.OneWayToSource, Source = priceFeed)) |> ignore

            flipPosition.SetBinding(Button.ContentProperty, "PositionAction") |> ignore
            positionSize.SetBinding(TextBox.TextProperty, "PositionSize") |> ignore
            positionOpenedAt.SetBinding(TextBlock.TextProperty, "PositionOpenedAt") |> ignore
            positionClosedAt.SetBinding(TextBlock.TextProperty, "PositionClosedAt") |> ignore
            positionPnL.SetBinding(TextBlock.TextProperty, "PositionPnL") |> ignore

//            slider.SetBinding(Slider.ValueProperty, Binding("PositionChangeRatio", FallbackValue = 0.0)) |> ignore
//            slider.SetBinding(Slider.SelectionStartProperty, Binding("StopLossMargin", FallbackValue = 0.0)) |> ignore
//            slider.SetBinding(Slider.SelectionEndProperty, Binding("TakeProfitMargin", FallbackValue = 0.0)) |> ignore

            positionPnL.SetBinding(
                TextBlock.ForegroundProperty, 
                Binding("PositionPnL", Converter = {
                    new IValueConverter with
                        member this.Convert(value, _, _, _) = 
                            match value with 
                            | :? decimal as x -> box(if x < 0M then "Red" else "Green") 
                            | _ -> DependencyProperty.UnsetValue
                        member this.ConvertBack(_, _, _, _) = DependencyProperty.UnsetValue
                })
            ) |> ignore

            stopLossAt.SetBinding(TextBox.TextProperty, Binding("StopLossAt", UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged)) |> ignore
            takeProfitAt.SetBinding(TextBox.TextProperty, Binding("TakeProfitAt", UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged)) |> ignore

            strategyAction.SetBinding(Button.ContentProperty, "StrategyAction") |> ignore

            let inpc : System.ComponentModel.INotifyPropertyChanged = upcast model
            inpc.PropertyChanged.Add <| fun args ->
                match args.PropertyName with 
                | "StopLossAt" -> 
                    if model.StopLossAt.HasValue 
                    then 
                        stopLoss.StripWidth <- float model.StopLossAt.Value 
                | "TakeProfitAt" -> 
                    if model.TakeProfitAt.HasValue 
                    then 
                        takeProfit.IntervalOffset <- float model.TakeProfitAt.Value
                        takeProfit.StripWidth <- float(!maxPrice - model.TakeProfitAt.Value)
                | _ -> ()
