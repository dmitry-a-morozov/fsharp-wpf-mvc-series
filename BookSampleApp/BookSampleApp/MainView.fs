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
open System.Windows.Documents

type MainEvents = 
    | InstrumentInfo
    | PriceUpdate of decimal
    | BuyOrSell 

type MainView(window : Window) = 

    //dynamic lookup op
    let (?) (window : Window) name = window.FindName name |> unbox

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

    let series = new Series(ChartType = SeriesChartType.FastLine, XValueType = ChartValueType.Int32, YValueType = ChartValueType.Double)
    let lossStrip = new StripLine(IntervalOffset = 0., BackColor = Color.FromArgb(32, Color.Red))
    let profitStrip = new StripLine(BackColor = Color.FromArgb(32, Color.Green))
    let stopLoss = new CustomLabel(RowIndex = 1, LabelMark = LabelMarkStyle.LineSideMark)
    let takeProfit = new CustomLabel(RowIndex = 1, LabelMark = LabelMarkStyle.LineSideMark)
    
    let area = new ChartArea() 

    let priceFeed = DispatcherTimer(Interval = TimeSpan.FromSeconds 0.5)
    let minPrice, maxPrice = ref Decimal.MaxValue, ref Decimal.MinValue
    let mutable nextPrice = fun() -> raise <| NotImplementedException()

    do
        let chart : Chart = window ? Chart
        chart.ChartAreas.Add area

        area.AxisX.LabelStyle.Enabled <- false
        //area.AxisX.IsMarginVisible <- false
        area.AxisX.MajorGrid.LineColor <- Color.LightGray    

        area.AxisY.StripLines.Add lossStrip
        area.AxisY.StripLines.Add profitStrip
        area.AxisY.MajorGrid.LineColor <- Color.LightGray    
        area.AxisY.LabelStyle.Format <- "F0"   
        area.AxisY.LabelAutoFitMinFontSize <- 10

        area.AxisY2.Enabled <- AxisEnabled.True
        //area.AxisY2.LabelStyle.Enabled <- false
        //area.AxisY2.MajorGrid.Enabled <- false
        //area.AxisY2.MajorTickMark.Enabled <- false
//        area.AxisY2.IsLabelAutoFit <- false
//        area.AxisY.CustomLabels.Add stopLoss
//        area.AxisY.CustomLabels.Add takeProfit

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
            stopLoss.FromPosition <- area.AxisY.Minimum
            takeProfit.ToPosition <- area.AxisY.Maximum
            area.AxisY2.Minimum <- area.AxisY.Minimum
            area.AxisY2.Maximum <- area.AxisY.Maximum
            //area.AxisY2.LabelStyle.Enabled <- true
//            let x = area.AxisY.CustomLabels.Add(area.AxisY.Minimum, area.AxisY.Minimum + 2.0, "Stop Loss", 1, LabelMarkStyle.LineSideMark) 
//            area.AxisY.CustomLabels.Add(area.AxisY.Minimum + 2.0, area.AxisY.Maximum, "Take Profit", 1, LabelMarkStyle.LineSideMark) |> ignore
            //area.AxisY2.CustomLabels.Add(area.AxisY2.Minimum, area.AxisY2.Minimum + 10.0, "Stop Loss") |> ignore
            //area.AxisY2.CustomLabels.Add(area.AxisY2.Maximum - 10.0, area.AxisY2.Maximum, "Take Profit") |> ignore

            let index = ref 0
            nextPrice <- fun() -> 
                if !index < prices.Length
                then
                    incr index 
                    Some(!index, prices.[!index - 1])
                else 
                    None
        )

    interface IView<MainEvents, MainModel> with
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
            priceFeedSimulation.SetBinding(CheckBox.IsCheckedProperty, Binding("IsEnabled", Mode = BindingMode.OneWayToSource, Source = priceFeed)) |> ignore

            action.SetBinding(Button.IsEnabledProperty, "NextActionEnabled") |> ignore
            actionText.SetBinding(
                Run.TextProperty, 
                Binding("PositionState", StringFormat = "{0} At", Converter = {
                    new IValueConverter with
                        member this.Convert(value, _, _, _) = 
                            match value with 
                            | :? PositionState as x -> 
                                box(
                                    match x with 
                                    | Zero -> "Buy" 
                                    | Opened -> "Sell" 
                                    | Closed -> "Current"
                                )
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
                            | :? decimal as x -> box(if x < 0M then "Red" else "Green") 
                            | _ -> DependencyProperty.UnsetValue
                        member this.ConvertBack(_, _, _, _) = DependencyProperty.UnsetValue
                })
            ) |> ignore

            stopLossAt.SetBinding(TextBox.TextProperty, Binding("StopLossAt", UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged)) |> ignore
            takeProfitAt.SetBinding(TextBox.TextProperty, Binding("TakeProfitAt", UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged)) |> ignore

            let inpc : System.ComponentModel.INotifyPropertyChanged = upcast model
            inpc.PropertyChanged.Add <| fun args ->
                match args.PropertyName with 
                | "Open" -> 
                    assert model.Open.HasValue
                    lossStrip.StripWidth <- float model.Open.Value
                    profitStrip.IntervalOffset <- float model.Open.Value
                    profitStrip.StripWidth <- series.Points.FindMaxByValue().YValues.[0]

//                | "StopLossAt" -> 
//                    if model.StopLossAt.HasValue 
//                    then
//                        match area.AxisY2.CustomLabels |> Seq.tryFind (fun x -> x.Text = "Stop Loss") with
//                        | None -> 
//                            //let label = new CustomLabel()
//                            let label = area.AxisY2.CustomLabels.Add(float model.StopLossAt.Value - 0.5, float model.StopLossAt.Value + 0.5, "Stop Loss") 
//                            label.GridTicks <- GridTickTypes.All
//                        | Some x -> 
//                            x.FromPosition <- float model.StopLossAt.Value - 0.5
//                            x.ToPosition <- float model.StopLossAt.Value + 0.5
////
////                            let removed = area.AxisY2.CustomLabels.Remove x
////                            assert removed
////                            let label = area.AxisY2.CustomLabels.Add(float model.StopLossAt.Value - 0.5, float model.StopLossAt.Value + 0.5, "Stop Loss") 
////                            label.GridTicks <- GridTickTypes.All
//
//                | "TakeProfitAt" -> 
//                    if model.TakeProfitAt.HasValue 
//                    then 
//                        takeProfit.FromPosition <- float model.TakeProfitAt.Value
                | _ -> ()
