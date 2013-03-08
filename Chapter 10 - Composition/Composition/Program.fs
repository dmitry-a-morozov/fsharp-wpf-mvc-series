
open System
open System.Windows
open FSharp.Windows.Sample
open FSharp.Windows

[<STAThread>] 
do
    let view = MainView()

    let mvc = 
        Mvc(MainModel.Create(), view, MainController())
            <+> ((fun m -> m.Calculator), CalculatorView(view.Control.Calculator), CalculatorController())
            <+> ((fun m -> m.TempConveter), TempConveterView(view.Control.TempConveterControl), TempConveterController())
            <+> ((fun m -> m.StockPricesChart), StockPricesChartView(view.Control.StockPricesChart), StockPricesChartController())

    mvc.Start() |> ignore
