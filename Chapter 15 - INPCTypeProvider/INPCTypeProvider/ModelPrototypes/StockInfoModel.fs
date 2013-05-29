namespace FSharp.Windows.Sample.Models

open System

type StockInfoModel = 
    {
        mutable Symbol : string
        mutable CompanyName : string
        mutable LastPrice : decimal
        mutable DaysLow : decimal
        mutable DaysHigh : decimal
        mutable Volume : decimal

        mutable AddToChartEnabled : bool
    }

    [<ReflectedDefinition>]
    member this.AccDist = 
        if this.DaysLow = 0M && this.DaysHigh = 0M then "Accumulation/Distribution: N/A"
        else
            let moneyFlowMultiplier = (this.LastPrice - this.DaysLow) - (this.DaysHigh - this.LastPrice) / (this.DaysHigh - this.DaysLow)
            let moneyFlowVolume  = moneyFlowMultiplier * this.Volume
            sprintf "Accumulation/Distribution: %M" <| Decimal.Round(moneyFlowVolume, 2)

