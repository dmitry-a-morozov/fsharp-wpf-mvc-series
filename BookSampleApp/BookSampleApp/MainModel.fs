namespace global

open System
//open Microsoft.FSharp.Linq

type MainModel() =
    inherit Model() 
    
    let mutable symbol : string = null
    let mutable instrumentName : string = null
    let mutable price = Nullable<decimal>()
    let mutable livePriceUpdates = false
    let mutable positionSize = Nullable<int>()

    member __.Symbol with get() = symbol and set value = symbol <- value; base.NotifyPropertyChanged "Symbol"
    member __.InstrumentName with get() = instrumentName and set value = instrumentName <- value; base.NotifyPropertyChanged "InstrumentName"
    member __.Price with get() = price and set value = price <- value; base.NotifyPropertyChanged "Price"
    member __.LivePriceUpdates with get() = livePriceUpdates and set value = livePriceUpdates <- value; base.NotifyPropertyChanged "LivePriceUpdates"
    member __.PositionSize with get() = positionSize and set value = positionSize <- value; base.NotifyPropertyChanged "PositionSize"
    