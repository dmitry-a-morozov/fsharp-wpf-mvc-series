﻿namespace Mvc.Wpf.Sample

open System
open System.Globalization
open System.Windows.Data
open System.Windows.Controls
open System.Net
open System.Linq
open Mvc.Wpf
open Mvc.Wpf.UIElements

[<AbstractClass>]
type StockPickerModel() = 
    inherit Model()

    abstract Symbol : string with get, set
    abstract CompanyName : string with get, set
    abstract LastPrice : decimal with get, set
    abstract AddToChartEnabled : bool with get, set

type StockPickerView() as this =
    inherit View<unit, StockPickerModel, StockPickerWindow>()

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
                this.Control.LastPrice.Text <- string model.LastPrice 
                this.Control.AddToChart.IsEnabled <- model.AddToChartEnabled
                this.Control.Retrieve.IsEnabled <- isNotNull model.Symbol
            @>

        Binding.UpdateSourceOnChange <@ this.Control.Symbol.Text <- model.Symbol @>

type StockPickerController(view) = 
    inherit SupervisingController<unit, StockPickerModel>(view)

    override this.InitModel _ = ()
    override this.Dispatcher = fun() ->
        Async <| fun model ->
            async {
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
            }

