namespace global

open System
open System.Windows
open Microsoft.FSharp.Linq
open Linq.NullableOperators

type PositionAction = Open = 0 | Close = 1
type StrategyAction = Start = 0 | Stop = 1

type MainModel() =
    inherit Model() 
    
    let mutable symbol : string = null
    let mutable instrumentName : string = null
    let mutable price = Nullable<decimal>()
    let mutable livePriceUpdates = false

    let mutable positionAction = PositionAction.Open
    let mutable positionSize = Nullable<int>()
    let mutable positionOpenedAt = Nullable<decimal>()
    let mutable positionClosedAt = Nullable<decimal>()
    let mutable positionPnL = Nullable<decimal>()

    let mutable stopLossAt = Nullable<decimal>()
    let mutable takeProfitAt = Nullable<decimal>()
    let mutable strategyAction = StrategyAction.Start

    member __.Symbol with get() = symbol and set value = symbol <- value; base.NotifyPropertyChanged "Symbol"
    member __.InstrumentName with get() = instrumentName and set value = instrumentName <- value; base.NotifyPropertyChanged "InstrumentName"
    member __.Price with get() = price and set value = price <- value; base.NotifyPropertyChanged "Price"
    member __.LivePriceUpdates with get() = livePriceUpdates and set value = livePriceUpdates <- value; base.NotifyPropertyChanged "LivePriceUpdates"

    member __.PositionAction with get() = positionAction and set value = positionAction <- value; base.NotifyPropertyChanged "PositionAction"
    member __.PositionSize with get() = positionSize and set value = positionSize <- value; base.NotifyPropertyChanged "PositionSize"
    member __.PositionOpenedAt with get() = positionOpenedAt and set value = positionOpenedAt <- value; base.NotifyPropertyChanged "PositionOpenedAt"
    member __.PositionClosedAt with get() = positionClosedAt and set value = positionClosedAt <- value; base.NotifyPropertyChanged "PositionClosedAt"
    member __.PositionPnL with get() = positionPnL and set value = positionPnL <- value; base.NotifyPropertyChanged "PositionPnL"

    member __.StopLossAt with get() = stopLossAt and set value = stopLossAt <- value; base.NotifyPropertyChanged "StopLossAt"
    member __.TakeProfitAt with get() = takeProfitAt and set value = takeProfitAt <- value; base.NotifyPropertyChanged "TakeProfitAt"
    member __.StrategyAction with get() = strategyAction and set value = strategyAction <- value; base.NotifyPropertyChanged "StrategyAction"
    
    member this.PositionOpenValue = Nullable.decimal this.PositionSize ?*? this.PositionOpenedAt
    member this.PositionCurrentValue = Nullable.decimal this.PositionSize ?*? this.Price
