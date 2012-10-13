
open System
open System.Windows
open Mvc.Wpf.Sample
open Mvc.Wpf

[<STAThread>] 
do
    let view = MainView()

    let controller = 
        MainController(view)
            .Compose(CalculatorController(), CalculatorView(view.Control.Calculator), fun m -> m.Calculator)
            <+> (TempConveterController(), TempConveterView(view.Control.TempConveterControl), fun m -> m.TempConveter)
            <+> (StockPricesChartController(), StockPricesChartView(view.Control.StockPricesChart), fun m -> m.StockPricesChart)

    controller |> Controller.start |> ignore
