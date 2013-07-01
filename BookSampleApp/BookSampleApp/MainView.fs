namespace global

open System 
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading

type MainEvents = 
    | InstrumentInfo
    | LivePriceUpdates of decimal
    | Start

type MainView(window : Window) = 

    let (?) (window : Window) name = window.FindName name |> unbox

    let symbol : TextBox = window ? Symbol
    let instrumentInfo : Button = window ? InstrumentInfo
    let instrumentName : TextBlock = window ? InstrumentName
    let price : TextBlock = window ? Price
    let livePriceUpdates : CheckBox = window ? LivePriceUpdates
    let start : Button = window ? Start

    //price feed simulation
    let priceFeed = DispatcherTimer(Interval = TimeSpan.FromSeconds 0.5)
    [<Literal>]
    let priceFluctuationPct = 20
    let mutable nextPrice = fun() -> 0M
    do
        livePriceUpdates.Checked |> Event.add (fun _ -> 
            let lastKnownPrice = decimal price.Text
            let random = Random(Seed = int lastKnownPrice)
            let delta = int lastKnownPrice * priceFluctuationPct / 100
            nextPrice <- fun() -> lastKnownPrice + decimal(random.Next(- delta, delta))
        )

    interface IView<MainEvents, MainModel> with
        member this.Subscribe observer = 
            let xs = 
                [
                    instrumentInfo.Click |> Observable.map (fun _ -> InstrumentInfo)
                    start.Click |> Observable.map (fun _ -> Start)

                    priceFeed.Tick |> Observable.map (fun _ -> LivePriceUpdates(nextPrice()))

                ] |> List.reduce Observable.merge 
            xs.Subscribe observer

        member this.SetBindings model = 
            window.DataContext <- model

            symbol.CharacterCasing <- CharacterCasing.Upper
            symbol.SetBinding(TextBox.TextProperty, "Symbol") |> ignore
            instrumentName.SetBinding(TextBlock.TextProperty, Binding(path = "InstrumentName", StringFormat = "Name : {0}")) |> ignore
            price.SetBinding(TextBlock.TextProperty, "Price") |> ignore
            livePriceUpdates.SetBinding(CheckBox.IsCheckedProperty, "LivePriceUpdates") |> ignore
            livePriceUpdates.SetBinding(CheckBox.IsCheckedProperty, Binding("IsEnabled", Mode = BindingMode.OneWayToSource, Source = priceFeed)) |> ignore
