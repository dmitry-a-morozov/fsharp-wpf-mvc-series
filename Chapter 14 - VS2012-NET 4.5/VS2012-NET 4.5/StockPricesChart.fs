namespace FSharp.Windows.Sample

open System.Windows.Data
open System.Drawing
open System.Windows.Forms.DataVisualization.Charting
open System.Collections.ObjectModel

open FSharp.Windows
open FSharp.Windows.UIElements

[<AbstractClass>]
type StockPricesChartModel() = 
    inherit Model()

    abstract StocksInfo : ObservableCollection<StockInfoModel> with get, set
    abstract SelectedStock : StockInfoModel with get, set

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
            selectedItem = <@ model.SelectedStock @>,
            displayMember = <@ fun m -> m.Symbol @> 
        )

        Binding.FromExpression
            <@
                this.Control.CompanyName.Text <- model.SelectedStock.CompanyName
                this.Control.AccDist.Text <- model.SelectedStock.AccDist
            @>

        Binding.UpdateSourceOnChange
            <@
                this.Control.LastPrice.Text <- coerce model.SelectedStock.LastPrice
                this.Control.DaysHigh.Text <- coerce model.SelectedStock.DaysHigh
                this.Control.DaysLow.Text <- coerce model.SelectedStock.DaysLow
                this.Control.Volume.Text <- coerce model.SelectedStock.Volume
            @>

type StockPricesChartController() = 
    inherit Controller<unit, StockPricesChartModel>()

    override this.InitModel model = 
        model.StocksInfo <- ObservableCollection()

    override this.Dispatcher = fun() -> 
        Async <| fun model ->
            async {
                let window = StockPickerWindow()
                let view = StockPickerView(window)
                let controller = StockPickerController()
                let! result = Mvc.startWindow(view, controller)  
                result |> Option.iter(fun newItem -> 
                    model.StocksInfo.Add newItem
                    model.SelectedStock <- newItem
                )
            }

