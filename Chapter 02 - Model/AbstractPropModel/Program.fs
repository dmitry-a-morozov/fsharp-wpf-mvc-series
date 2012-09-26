open System
open System.Windows
open System.Windows.Controls

open Mvc.Wpf

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract X : int with get, set
    abstract Y : int with get, set
    abstract Result : int with get, set

[<STAThread>] 
do 
    let model : SampleModel = Model.Create()

    let x = TextBox()
    x.SetBinding(TextBox.TextProperty, "X") |> ignore

    let y = TextBox()
    y.SetBinding(TextBox.TextProperty, "Y") |> ignore

    let result = TextBlock()
    result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

    let calculate = Button(Content = "Add")

    let panel = StackPanel()

    panel.Children.Add x |> ignore
    panel.Children.Add y |> ignore
    panel.Children.Add result |> ignore
    panel.Children.Add calculate |> ignore

    let window = Window(Title = "Simple model", Content = panel, Width = 200., Height = 200.)
    window.DataContext <- model

    calculate.Click.Add(fun _ -> model.Result <- model.X + model.Y)

    Application().Run window |> ignore