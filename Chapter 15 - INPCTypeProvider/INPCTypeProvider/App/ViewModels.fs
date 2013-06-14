namespace FSharp.Windows.Sample

open FSharp.Windows
open FSharp.Windows.INPCTypeProvider

type ViewModels = NotifyPropertyChanged<"ModelPrototypes">

type CalculatorModel = ViewModels.CalculatorModel
type TempConveterModel = ViewModels.TempConveterModel
type StockInfoModel = ViewModels.StockInfoModel
type StockPricesChartModel = ViewModels.StockPricesChartModel
type HexConverterModel = ViewModels.HexConverterModel
//type MainModel = ViewModels.MainModel

[<RequireQualifiedAccess>]
module Mvc = 

    let inline startDialog(view, controller) = 
        let model = new 'Model()
        if Mvc<'Events, 'Model>(model, view, controller).StartDialog() then Some model else None

    let inline startWindow(view, controller) = 
        async {
            let model = new 'Model()
            let! isOk = Mvc<'Events, 'Model>(model, view, controller).StartWindow()
            return if isOk then Some model else None
        }