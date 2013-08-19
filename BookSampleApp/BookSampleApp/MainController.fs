namespace global

open System
open Microsoft.FSharp.Linq
open Linq.NullableOperators

type MainContoller(?symbology : string -> Symbology.Instrument option) = 

    let symbologyImpl = defaultArg symbology Symbology.yahoo

    interface IController<MainEvents, MainModel> with

        member this.InitModel model = 
            model.PositionSize <- 10

        member this.EventHandler = function 
            | InstrumentInfo -> this.GetInstrumentInfo
            | PriceUpdate newPrice -> this.UpdateCurrentPrice newPrice
            | BuyOrSell -> this.MoveToNextPositionState

    member this.GetInstrumentInfo(model : MainModel) = 
        symbologyImpl model.Symbol |> Option.map (fun x -> model.InstrumentName <- x.Name) |> ignore

    member this.UpdateCurrentPrice newPrice (model : MainModel) =
        let prevPrice = model.Price
        model.Price <- Nullable newPrice
        match model.PositionState with
        | PositionState.Opened -> 
            model.PnL <- model.PositionCurrentValue.Value - model.PositionOpenValue.Value
            let takeProfitLimit = prevPrice ?< newPrice && newPrice >=? model.TakeProfitAt
            let stopLossLimit = prevPrice ?> newPrice && newPrice <=? model.StopLossAt
            if takeProfitLimit || stopLossLimit 
            then model.ClosePosition()
        | _ -> ()

    member this.MoveToNextPositionState (model : MainModel) = 
        match model.PositionState with
        | PositionState.Zero ->
            model.Open <- model.Price
            model.PositionState <- PositionState.Opened
        | PositionState.Opened ->
            model.ClosePosition()
        | PositionState.Closed -> 
            ()