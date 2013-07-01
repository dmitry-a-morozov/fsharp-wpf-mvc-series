namespace global

open System

type MainContoller(symbology : string -> Symbology.Instrument option) = 
    
    new() = MainContoller(Symbology.yahoo)

    interface IController<MainEvents, MainModel> with
        member this.InitModel model = ()
        member this.EventHandler = function 
            | InstrumentInfo -> this.GetInstrumentInfo
            | LivePriceUpdates newPrice -> this.UpdateCurrentPrice newPrice
            | Start -> this.Start

    member this.GetInstrumentInfo(model : MainModel) = 
        match symbology model.Symbol with
        | Some x -> 
            model.InstrumentName <- x.Name
            model.Price <- Nullable x.LastPrice
        | None -> 
            model |> Validation.addError <@ fun m -> m.Symbol @> "Invalid symbol."
        ()

    member this.UpdateCurrentPrice newPrice (model : MainModel) =
        model.Price <- Nullable newPrice

    member this.Start model = 
        ()
