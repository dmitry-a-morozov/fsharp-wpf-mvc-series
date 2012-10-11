namespace Mvc.Wpf.Sample

open System.Windows.Data
open System.Drawing
open System.Windows.Forms.DataVisualization.Charting
open System.Collections.ObjectModel

open Mvc.Wpf
open Mvc.Wpf.UIElements

[<AbstractClass>]
type StockPricesChartModel() = 
    inherit Model()

    abstract StockPrices : ObservableCollection<string * decimal> with get, set

type StockPricesChartEvents = AddStockToPriceChart

type StockPricesChartView(control) as this =
    inherit PartialView<StockPricesChartEvents, StockPricesChartModel, StockPricesChartControl>(control)

    do 
        let area = new ChartArea() 
        area.AxisX.MajorGrid.LineColor <- Color.LightGray
        area.AxisY.MajorGrid.LineColor <- Color.LightGray        
        this.Control.StockPricesChart.ChartAreas.Add area
        let series = 
            new Series(
                ChartType = SeriesChartType.Column, 
                Palette = ChartColorPalette.EarthTones, 
                XValueMember = "Item1", 
                YValueMembers = "Item2")
        this.Control.StockPricesChart.Series.Add series
    
    override this.EventStreams = 
        [
            this.Control.AddStock.Click |> Observable.mapTo AddStockToPriceChart
        ]

    override this.SetBindings model = 
        this.Control.StockPricesChart.DataSource <- model.StockPrices
        model.StockPrices.CollectionChanged.Add(fun _ -> this.Control.StockPricesChart.DataBind())

type StockPricesController(view) = 
    inherit Controller<StockPricesChartEvents, StockPricesChartModel>()

    override this.InitModel model = 
        model.StockPrices <- ObservableCollection()

    override this.Dispatcher = function 
        AddStockToPriceChart -> Async <| fun model ->
            async {
                let view = StockPickerView()
                let! result = StockPickerController view |> Controller.asyncStart  
                result |> Option.iter (fun stockInfo ->
                    model.StockPrices.Add(stockInfo.Symbol, stockInfo.LastPrice)
                )
            }
