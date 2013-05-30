namespace FSharp.Windows.Sample

open FSharp.Windows.INPCTypeProvider

type ViewModels = NotifyPropertyChanged<"ModelPrototypes">

type CalculatorModel = ViewModels.CalculatorModel
type TempConveterModel = ViewModels.TempConveterModel
type StockInfoModel = ViewModels.StockInfoModel
type StockPricesChartModel = ViewModels.StockPricesChartModel