namespace global

open System
open Microsoft.FSharp.Linq
open Linq.NullableOperators

type MainContoller(symbology : string -> Symbology.Instrument option) = 

    new() = MainContoller(Symbology.yahoo)

    interface IController<MainEvents, MainModel> with

        member this.InitModel model = 
//            model.StopLossMargin <- Nullable -50M
//            model.TakeProfitMargin <- Nullable 50M
//            model.PositionChangeRatio <- Nullable 00.00M
            ()

        member this.EventHandler = function 
            | InstrumentInfo -> this.GetInstrumentInfo
            | PriceUpdate newPrice -> this.UpdateCurrentPrice newPrice
            | BuyOrSell -> this.MoveToNetxState

    member this.GetInstrumentInfo(model : MainModel) = 
        match symbology model.Symbol with
        | Some x -> 
            model.InstrumentName <- x.Name
        | None -> 
            model |> Validation.addError <@ fun m -> m.Symbol @> "Invalid symbol."
        ()

    member this.UpdateCurrentPrice newPrice (model : MainModel) =
        let prevPrice = model.Price
        model.Price <- Nullable newPrice
        match model.PositionState with
        | PositionState.Opened -> 
            model.PnL <- model.PositionCurrentValue ?-? model.PositionOpenValue
            let takeProfitLimit = prevPrice ?< newPrice && newPrice >=? model.TakeProfitAt
            let stopLossLimit = prevPrice ?> newPrice && newPrice <=? model.StopLossAt
            if takeProfitLimit || stopLossLimit 
            then model.ClosePosition()
        | _ -> ()

    member this.MoveToNetxState (model : MainModel) = 
        match model.PositionState with
        | PositionState.Zero ->
            model.Open <- model.Price
            model.PositionState <- PositionState.Opened
        | PositionState.Opened ->
            model.ClosePosition()
        | PositionState.Closed -> 
            ()
    