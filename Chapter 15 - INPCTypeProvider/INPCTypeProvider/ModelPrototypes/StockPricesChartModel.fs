namespace FSharp.Windows.Sample.Models

open System.Collections.ObjectModel

type StockPricesChartModel = {
    mutable StocksInfo : StockInfoModel ObservableCollection
    mutable SelectedStock : StockInfoModel 
}

