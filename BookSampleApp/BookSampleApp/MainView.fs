namespace global

open System 
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading
open System.Windows.Forms.DataVisualization.Charting
open System.Drawing
open System.ComponentModel;
open System.Net
open System.Windows.Documents

type MainEvents = 
    | InstrumentInfo
    | PriceUpdate of decimal
    | BuyOrSell 

type MainView(window : Window) = 

    //dynamic control lookup
    let (?) (window : Window) name = name |> window.FindName |> unbox

    let symbol : TextBox = window ? Symbol
    let instrumentInfo : Button = window ? InstrumentInfo
    let instrumentName : TextBlock = window ? InstrumentName
    let priceFeedSimulation : CheckBox = window ? PriceFeedSimulation

    let positionSize : TextBox = window ? PositionSize
    let stopLossAt : TextBox = window ? StopLossAt
    let takeProfitAt : TextBox = window ? TakeProfitAt

    let action : Button = window ? Action
    let actionText : Run = window ? ActionText
    let price : Run = window ? Price

    let ``open`` : TextBlock = window ? Open
    let close : TextBlock = window ? Close
    let pnl : TextBlock = window ? PnL

    //Chart
    let area = new ChartArea() 
    let series = new Series(ChartType = SeriesChartType.FastLine, XValueType = ChartValueType.Int32, YValueType = ChartValueType.Double)
    let lossStrip = new StripLine(IntervalOffset = 0., BackColor = Color.FromArgb(32, Color.Red))
    let profitStrip = new StripLine(BackColor = Color.FromArgb(32, Color.Green))
   
    let priceFeed = DispatcherTimer(Interval = TimeSpan.FromSeconds 0.5)
    let minPrice, maxPrice = ref Decimal.MaxValue, ref Decimal.MinValue
    let mutable nextPrice = fun() -> raise <| NotImplementedException()

    do
        let chart : Chart = window ? Chart
        chart.ChartAreas.Add area

        area.AxisX.LabelStyle.Enabled <- false
        area.AxisX.IsMarginVisible <- false
        area.AxisX.MajorGrid.LineColor <- Color.LightGray    

        area.AxisY.StripLines.Add lossStrip
        area.AxisY.StripLines.Add profitStrip
        area.AxisY.MajorGrid.LineColor <- Color.LightGray    
        area.AxisY.LabelStyle.Format <- "F0"   
        area.AxisY.LabelAutoFitMinFontSize <- 10

        area.AxisY2.CustomLabels.Add <| new CustomLabel(GridTicks = GridTickTypes.All, Text = "Take Profit", FromPosition = 0.0, ToPosition = 0.0)
        area.AxisY2.CustomLabels.Add <| new CustomLabel(GridTicks = GridTickTypes.All, Text = "Stop Loss", FromPosition = 0.0, ToPosition = 0.0)
        area.AxisY2.LabelAutoFitMinFontSize <- 10

        chart.Series.Add series

        //price feed simulation
        priceFeedSimulation.Checked |> Event.add (fun _ -> 
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
            area.AxisY2.Minimum <- area.AxisY.Minimum
            area.AxisY2.Maximum <- area.AxisY.Maximum

            area.AxisY2.Enabled <- AxisEnabled.True
            let x = area.AxisY2.CustomLabels |> Seq.find (fun x -> x.Text = "Stop Loss")
            x.FromPosition <- area.AxisY.Minimum - 0.5
            x.ToPosition <- area.AxisY.Minimum + 0.5
            let x = area.AxisY2.CustomLabels |> Seq.find (fun x -> x.Text = "Take Profit")
            x.FromPosition <- area.AxisY.Maximum - 0.5
            x.ToPosition <- area.AxisY.Maximum + 0.5


            let index = ref 0
            nextPrice <- fun() -> 
                if !index < prices.Length
                then
                    incr index 
                    Some(!index, prices.[!index - 1])
                else 
                    None
        )

    interface IView<MainEvents> with
        member this.Subscribe observer = 
            let xs = 
                [
                    instrumentInfo.Click |> Observable.map (fun _ -> InstrumentInfo)
                    priceFeed.Tick |> Observable.choose (fun _ ->   
                        nextPrice() |> Option.map (fun(index, value) ->
                            let i = series.Points.AddXY(box index, value)
                            PriceUpdate value
                        )
                    )
                    action.Click |> Observable.map (fun _ -> BuyOrSell)
                ] 
                |> List.reduce Observable.merge 

            xs.Subscribe observer

        member this.SetBindings model = 
            window.DataContext <- model

            symbol.SetBinding(TextBox.TextProperty, "Symbol") |> ignore
            instrumentName.SetBinding(TextBlock.TextProperty, Binding(path = "InstrumentName", StringFormat = "Name : {0}")) |> ignore
            priceFeedSimulation.SetBinding(CheckBox.IsCheckedProperty, "PriceFeedSimulation") |> ignore

            action.SetBinding(Button.IsEnabledProperty, "NextActionEnabled") |> ignore
            actionText.SetBinding(
                Run.TextProperty, 
                Binding("PositionState", Converter = {
                    new IValueConverter with
                        member this.Convert(value, _, _, _) = 
                            match value with 
                            | :? PositionState as x -> 
                                match x with  | Zero -> "Buy" | Opened -> "Sell" | Closed -> "Current"
                                |> box
                            | _ -> DependencyProperty.UnsetValue
                        member this.ConvertBack(_, _, _, _) = DependencyProperty.UnsetValue
                })
            ) |> ignore
            price.SetBinding(Run.TextProperty, "Price") |> ignore

            positionSize.SetBinding(TextBox.TextProperty, "PositionSize") |> ignore

            ``open``.SetBinding(TextBlock.TextProperty, "Open") |> ignore
            close.SetBinding(TextBlock.TextProperty, "Close") |> ignore
            pnl.SetBinding(TextBlock.TextProperty, "PnL") |> ignore
            pnl.SetBinding(
                TextBlock.ForegroundProperty, 
                Binding("PnL", Converter = {
                    new IValueConverter with
                        member this.Convert(value, _, _, _) = 
                            match value with 
                            | :? decimal as x -> 
                                if x < 0M then box "Red" 
                                elif x > 0M then box "Green" 
                                else DependencyProperty.UnsetValue
                            | _ -> DependencyProperty.UnsetValue
                        member this.ConvertBack(_, _, _, _) = DependencyProperty.UnsetValue
                })
            ) |> ignore

            stopLossAt.SetBinding(TextBox.TextProperty, Binding("StopLossAt", UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, TargetNullValue = Nullable<decimal>())) |> ignore
            takeProfitAt.SetBinding(TextBox.TextProperty, Binding("TakeProfitAt", UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, TargetNullValue = Nullable<decimal>())) |> ignore

            priceFeedSimulation.SetBinding(CheckBox.IsCheckedProperty, Binding("IsEnabled", Mode = BindingMode.OneWayToSource, Source = priceFeed)) |> ignore

            let inpc : INotifyPropertyChanged = unbox model
            let model : MainModel = unbox model 
            inpc.PropertyChanged.Add <| fun args ->
                match args.PropertyName with 
                | "Open" -> 
                    assert model.Open.HasValue
                    lossStrip.StripWidth <- float model.Open.Value
                    profitStrip.IntervalOffset <- float model.Open.Value
                    profitStrip.StripWidth <- series.Points.FindMaxByValue().YValues.[0]
                | "StopLossAt" -> 
                    assert model.StopLossAt.HasValue 
                    area.AxisY2.Enabled <- AxisEnabled.True
                    let x = area.AxisY2.CustomLabels |> Seq.find (fun x -> x.Text = "Stop Loss")
                    x.FromPosition <- float model.StopLossAt.Value - 0.5
                    x.ToPosition <- float model.StopLossAt.Value + 0.5
                | "TakeProfitAt" -> 
                    assert model.TakeProfitAt.HasValue 
                    area.AxisY2.Enabled <- AxisEnabled.True
                    let x = area.AxisY2.CustomLabels |> Seq.find (fun x -> x.Text = "Take Profit")
                    x.FromPosition <- float model.TakeProfitAt.Value - 0.5
                    x.ToPosition <- float model.TakeProfitAt.Value + 0.5
                | _ -> ()
