namespace Mvc.Wpf.Sample

open System
open System.Collections.Generic
open System.Globalization
open System.Windows.Data
open System.Windows.Controls
open System.Net
open System.Linq
open Mvc.Wpf
open Mvc.Wpf.UIElements

[<AbstractClass>]
type StockInfoModel() = 
    inherit Model()

    abstract Symbol : string with get, set
    abstract CompanyName : string with get, set
    abstract LastPrice : decimal with get, set
    abstract DaysLow : decimal with get, set
    abstract DaysHigh : decimal with get, set
    abstract Volume : decimal with get, set

    abstract AddToChartEnabled : bool with get, set

    [<NotifyDependencyChanged>]
    member this.AccDist = 
        let moneyFlowMultiplier = (this.LastPrice - this.DaysLow) - (this.DaysHigh - this.LastPrice) / (this.DaysHigh - this.DaysLow)
        let moneyFlowVolume  = moneyFlowMultiplier * this.Volume
        sprintf "Accumulation/Distribution: %M" <| Decimal.Round(moneyFlowVolume, 2)

open FSharpx
type StockPickerWindow = XAML<"View\StockPickerWindow.xaml">

type StockPickerView(xaml : StockPickerWindow) as this =
    inherit XamlProviderView<unit, StockInfoModel>(xaml.Root)

    let companyName = xaml.CompanyName
    let addToChart = xaml.AddToChart
    let retrieve = xaml.Retrieve
    let symbol = xaml.Symbol

    do
        symbol.CharacterCasing <- CharacterCasing.Upper
        this.CancelButton <- xaml.CloseButton
        this.DefaultOKButton <- xaml.AddToChart

    override this.EventStreams = 
        [
            xaml.Retrieve.Click |> Observable.mapTo()
        ]

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                companyName.Text <- model.CompanyName
                addToChart.IsEnabled <- model.AddToChartEnabled
                retrieve.IsEnabled <- isNotNull model.Symbol
            @>

        Binding.UpdateSourceOnChange <@ symbol.Text <- model.Symbol @>

        let converter = BooleanToVisibilityConverter()
        Binding.FromExpression 
            <@ 
                addToChart.Visibility <- converter.Apply model.AddToChartEnabled
            @>

type StockPickerController(view) = 
    inherit SupervisingController<unit, StockInfoModel>(view)

    static let tags = [
            "n", "Name"
            "l1", "Last Trade (Price Only)"
            "h", "Day’s High"
            "g", "Day’s Low"
            "v", "Volume"
        ]

    override this.InitModel _ = ()
    override this.Dispatcher = fun() ->
        Async <| fun model ->
            async {
                use wc = new WebClient()
                let uri = tags |> List.map fst |> String.concat "" |> sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=%s" model.Symbol
                let! data = wc.AsyncDownloadString <| Uri uri
                match data.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries).Single().Split(',') with
                | [| name; lastPrice; high; low; volume |] ->
                    if name = sprintf "\"%s\"" model.Symbol && lastPrice = "0.00"
                    then
                        model |> Validation.setError <@ fun m -> m.Symbol @> "Invalid security symbol."
                    else
                        model.CompanyName <- name
                        model.LastPrice <- decimal lastPrice
                        model.DaysHigh <- decimal high
                        model.DaysLow <- decimal low
                        model.Volume <- decimal volume
                        model.AddToChartEnabled <- true
                | _ -> failwithf "Unexpected result service call result.\nRequest: %O.\nResponse: %s" uri data
            }

