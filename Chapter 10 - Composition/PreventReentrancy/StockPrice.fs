namespace Mvc.Wpf.Sample

open System
open System.Globalization
open System.Windows.Data
open System.Windows.Controls
open System.Net
open System.Linq
open Mvc.Wpf

[<AbstractClass>]
type StockPriceModel() = 
    inherit Model()

    abstract Symbol : string with get, set
    abstract CompanyName : string with get, set
    abstract LastPrice : decimal with get, set
    abstract AddToChartEnabled : bool with get, set

type StockPriceView() as this =
    inherit View<unit, StockPriceModel, StockPriceWindow>()

    do
        this.Window.Symbol.CharacterCasing <- CharacterCasing.Upper
        this.CancelButton <- this.Window.CloseButton
        this.OKButton <- this.Window.AddToChart

    override this.EventStreams = 
        [
            this.Window.Retrieve.Click |> Observable.mapTo()
        ]

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Window.Symbol.Text <- model.Symbol
                this.Window.CompanyName.Text <- model.CompanyName
                this.Window.LastPrice.Text <- string model.LastPrice 
                this.Window.AddToChart.IsEnabled <- model.AddToChartEnabled
            @>

type StockPriceController(view) = 
    inherit Controller<unit, StockPriceModel>(view)

    override this.InitModel _ = ()
    override this.Dispatcher = fun() ->
        Async(fun model ->
            async {
                model |> Validation.textRequired <@ fun m -> m.Symbol @>
                if not model.HasErrors 
                then 
                    use wc = new WebClient()
                    let url = Uri(sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=nl1" model.Symbol)
                    let! data = wc.AsyncDownloadString url
                    match data.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries).Single().Split(',') with
                    | [| n; "0.00" |] when n = sprintf "\"%s\"" model.Symbol -> 
                        model |> Validation.setError <@ fun m -> m.Symbol @> "Invalid security symbol."
                    | [| n; l1 |] -> 
                        model.CompanyName <- n
                        model.LastPrice <- decimal l1
                        model.AddToChartEnabled <- true
                    | _ -> failwithf "Unexpected result service call result.\nRequest: %O.\nResponse: %s" url data
            })

