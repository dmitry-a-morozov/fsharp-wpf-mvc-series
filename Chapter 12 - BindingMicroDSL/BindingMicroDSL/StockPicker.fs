namespace FSharp.Windows.Sample

open System
open System.Collections.Generic
open System.Globalization
open System.Windows.Data
open System.Windows.Controls
open System.Net
open System.Linq
open FSharp.Windows
open FSharp.Windows.UIElements

[<AbstractClass>]
type StockInfoModel() = 
    inherit Model()

    abstract Symbol : string with get, set
    abstract CompanyName : string with get, set
    abstract LastPrice : decimal with get, set
    abstract AddToChartEnabled : bool with get, set
    abstract Details : IDictionary<string, string> with get, set

type StockPickerView() as this =
    inherit View<unit, StockInfoModel, StockPickerWindow>()

    do
        this.Control.Symbol.CharacterCasing <- CharacterCasing.Upper
        this.CancelButton <- this.Control.CloseButton
        this.DefaultOKButton <- this.Control.AddToChart

    override this.EventStreams = 
        [
            this.Control.Retrieve.Click |> Observable.mapTo()
        ]

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Control.CompanyName.Text <- model.CompanyName
                this.Control.AddToChart.IsEnabled <- model.AddToChartEnabled
                this.Control.Retrieve.IsEnabled <- isNotNull model.Symbol
            @>

        Binding.UpdateSourceOnChange <@ this.Control.Symbol.Text <- model.Symbol @>

        let converter = BooleanToVisibilityConverter()
        Binding.FromExpression 
            <@ 
                this.Control.AddToChart.Visibility <- converter.Apply model.AddToChartEnabled
            @>

type StockPickerController() = 
    inherit Controller<unit, StockInfoModel>()

    static let tags = [
            "n", "Name"
            "l1", "Last Trade (Price Only)"
            "d1", "Last Trade Date"
            "t1", "Last Trade Time"
            "c1", "Change"
            "h", "Day’s High"
            "g", "Day’s Low"
            "v", "Volume"
            "b", "Bid"
            "a", "Ask"
            "p2", "Change in Percent"
        ]

    override this.InitModel _ = ()
    override this.Dispatcher = fun() ->
        Async <| fun model ->
            async {
                use wc = new WebClient()
                let uri = tags |> List.map fst |> String.concat "" |> sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=%s" model.Symbol
                let! data = wc.AsyncDownloadString <| Uri uri
                match data.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries).Single().Split(',') |> Array.toList with
                | name :: lastPrice :: _ as details ->
                    if name = sprintf "\"%s\"" model.Symbol && lastPrice = "0.00"
                    then
                        model |> Validation.setError <@ fun m -> m.Symbol @> "Invalid security symbol."
                    else
                        model.CompanyName <- name
                        model.LastPrice <- decimal lastPrice
                        model.Details <- (List.map snd tags, details) ||> List.zip |> dict
                        model.AddToChartEnabled <- true
                | _ -> failwithf "Unexpected result service call result.\nRequest: %O.\nResponse: %s" uri data
            }

