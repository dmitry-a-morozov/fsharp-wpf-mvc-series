namespace global

open System
open System.Windows
open Microsoft.FSharp.Linq
open Linq.NullableOperators

type PositionState = Zero | Opened | Closed

type MainModel() =
    inherit Model() 
    
    let mutable symbol : string = null
    let mutable instrumentName : string = null
    let mutable price = Nullable<decimal>()
    let mutable priceFeedSimulation = false

    let mutable positionState = PositionState.Zero
    let mutable nextActionEnabled = true
    let mutable positionSize = 0
    let mutable ``open`` = Nullable<decimal>()
    let mutable close = Nullable<decimal>()
    let mutable pnl = 0M

    let mutable stopLossAt = Nullable<decimal>()
    let mutable takeProfitAt = Nullable<decimal>()

    member __.Symbol with get() = symbol and set value = symbol <- value; base.NotifyPropertyChanged "Symbol"
    member __.InstrumentName with get() = instrumentName and set value = instrumentName <- value; base.NotifyPropertyChanged "InstrumentName"
    member __.Price with get() = price and set value = price <- value; base.NotifyPropertyChanged "Price"
    member __.PriceFeedSimulation with get() = priceFeedSimulation and set value = priceFeedSimulation <- value; base.NotifyPropertyChanged "PriceFeedSimulation"

    member __.PositionState with get() = positionState and set value = positionState <- value; base.NotifyPropertyChanged "PositionState"
    member __.NextActionEnabled with get() = nextActionEnabled and set value = nextActionEnabled <- value; base.NotifyPropertyChanged "NextActionEnabled"
    member __.PositionSize with get() = positionSize and set value = positionSize <- value; base.NotifyPropertyChanged "PositionSize"
    member __.Open with get() = ``open`` and set value = ``open`` <- value; base.NotifyPropertyChanged "Open"
    member __.Close with get() = close and set value = close <- value; base.NotifyPropertyChanged "Close"
    member __.PnL with get() = pnl and set value = pnl <- value; base.NotifyPropertyChanged "PnL"

    member __.StopLossAt with get() = stopLossAt and set value = stopLossAt <- value; base.NotifyPropertyChanged "StopLossAt"
    member __.TakeProfitAt with get() = takeProfitAt and set value = takeProfitAt <- value; base.NotifyPropertyChanged "TakeProfitAt"
    
    member this.PositionOpenValue = decimal this.PositionSize *? this.Open
    member this.PositionCurrentValue = decimal this.PositionSize *? this.Price

    member this.ClosePosition() = 
        this.Close <- this.Price
        this.PositionState <- PositionState.Closed
        this.NextActionEnabled <- false