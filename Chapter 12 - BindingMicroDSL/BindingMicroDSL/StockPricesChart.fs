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

    abstract StocksInfo : ObservableCollection<StockInfoModel> with get, set
    abstract Selected : StockInfoModel with get, set

type StockPricesChartView(control) as this =
    inherit PartialView<unit, StockPricesChartModel, StockPricesChartControl>(control)

    do 
        let area = new ChartArea() 
        area.AxisX.MajorGrid.LineColor <- Color.LightGray
        area.AxisY.MajorGrid.LineColor <- Color.LightGray        
        this.Control.StockPricesChart.ChartAreas.Add area
        let series = 
            new Series(
                ChartType = SeriesChartType.Column, 
                Palette = ChartColorPalette.EarthTones, 
                XValueMember = "Symbol", 
                YValueMembers = "LastPrice")
        this.Control.StockPricesChart.Series.Add series
    
    override this.EventStreams = 
        [
            this.Control.AddStock.Click |> Observable.mapTo()
        ]

    override this.SetBindings model = 
        this.Control.StockPricesChart.DataSource <- model.StocksInfo
        model.StocksInfo.CollectionChanged.Add(fun _ -> this.Control.StockPricesChart.DataBind())

        this.Control.Symbol.SetBindings(
            itemsSource = <@ model.StocksInfo @>, 
            selectedItem = <@ model.Selected @>,
            displayMember = <@ fun m -> m.Symbol @> 
        )

        this.Control.Details.SetBindings(
            itemsSource = <@ model.Selected.Details @>, 
//            itemsSource = <@ model.StocksInfo.CurrentItem.Details @>, 
            columnBindings = fun stockProperty ->
                [
                    this.Control.DetailsName, <@@ stockProperty.Key @@>
                    this.Control.DetailsValue, <@@ stockProperty.Value @@>
                ]
        )

type StockPricesChartController() = 
    inherit Controller<unit, StockPricesChartModel>()

    override this.InitModel model = 
        model.StocksInfo <- ObservableCollection()

    override this.Dispatcher = fun() -> 
        Async <| fun model ->
            async {
                let view = StockPickerView()
                let! result = StockPickerController view |> Controller.asyncStart  
                result |> Option.iter(fun newItem -> 
                    model.StocksInfo.Add newItem
                    model.Selected <- newItem
                )
            }
