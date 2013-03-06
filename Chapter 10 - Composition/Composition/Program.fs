
open System
open System.Windows
open FSharp.Windows.Sample
open FSharp.Windows

[<STAThread>] 
do
    let view = MainView()

    let mvc = 
        Mvc(MainModel.Create(), view, MainController())
            <+> (CalculatorController(), CalculatorView(view.Control.Calculator), fun m -> m.Calculator)
            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)
            <+> (StockPricesChartController(), StockPricesChartView(view.Control.StockPricesChart), fun m -> m.StockPricesChart)

    mvc.Start() |> ignore
