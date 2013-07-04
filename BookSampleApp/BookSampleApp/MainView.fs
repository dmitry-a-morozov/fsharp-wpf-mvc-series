namespace global

open System 
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading

type MainEvents = 
    | InstrumentInfo
    | LivePriceUpdates of decimal
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
    let slider : Slider = window ? Indicator
    let positionPnLPct : TextBlock = window ? PositionPnLPct

    let stopLossMargin : TextBox = window ? StopLossMargin
    let takeProfitMargin : TextBox = window ? TakeProfitMargin
    let strategyAction : Button = window ? StrategyAction

    //price feed simulation
    let priceFeed = DispatcherTimer(Interval = TimeSpan.FromSeconds 0.5)
    let mutable nextPrice = fun() -> 0M
    do
        livePriceUpdates.Checked |> Event.add (fun _ -> 
            let lastKnownPrice = decimal price.Text
            let random = Random()//Seed = int lastKnownPrice)
            let deviation = int(lastKnownPrice / 2M)
            nextPrice <- fun() -> lastKnownPrice + decimal(random.Next(-deviation / 2, deviation))
        )

    interface IView<MainEvents, MainModel> with
        member this.Subscribe observer = 
            let xs = 
                [
                    instrumentInfo.Click |> Observable.map (fun _ -> InstrumentInfo)
                    priceFeed.Tick |> Observable.map (fun _ -> LivePriceUpdates(nextPrice()))
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

            slider.SetBinding(Slider.ValueProperty, Binding("PositionChangeRatio", FallbackValue = 0.0)) |> ignore
            slider.SetBinding(Slider.SelectionStartProperty, Binding("StopLossMargin", FallbackValue = 0.0)) |> ignore
            slider.SetBinding(Slider.SelectionEndProperty, Binding("TakeProfitMargin", FallbackValue = 0.0)) |> ignore
            positionPnLPct.SetBinding(TextBlock.TextProperty, Binding("PositionChangeRatio", StringFormat = "{0:00.00}%")) |> ignore

            positionPnLPct.SetBinding(
                TextBlock.ForegroundProperty, 
                Binding("PositionChangeRatio", Converter = {
                    new IValueConverter with
                        member this.Convert(value, _, _, _) = 
                            match value with 
                            | :? decimal as x -> box(if x < 0M then "Red" else "Green") 
                            | _ -> DependencyProperty.UnsetValue
                        member this.ConvertBack(_, _, _, _) = DependencyProperty.UnsetValue
                })
            ) |> ignore

            stopLossMargin.SetBinding(TextBox.TextProperty, "StopLossMargin") |> ignore
            takeProfitMargin.SetBinding(TextBox.TextProperty, "TakeProfitMargin") |> ignore
            strategyAction.SetBinding(Button.ContentProperty, "StrategyAction") |> ignore
