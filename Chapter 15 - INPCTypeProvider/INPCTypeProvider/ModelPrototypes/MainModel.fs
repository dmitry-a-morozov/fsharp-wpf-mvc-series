namespace FSharp.Windows.Sample.Models

open System
open System.Windows.Controls

type MainModel = 
    { 
        mutable Calculator : CalculatorModel
        mutable TempConveter : TempConveterModel
        mutable StockPricesChart : StockPricesChartModel

        mutable ProcessName : string
        mutable ActiveTab : TabItem
        mutable RunningTime : TimeSpan
        mutable Paused : bool
        mutable Fail : bool
    }

    [<ReflectedDefinition>]
    member this.Title = sprintf "%s-%O" this.ProcessName this.ActiveTab.Header