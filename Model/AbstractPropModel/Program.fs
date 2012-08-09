open System
open System.Windows
open System.Windows.Controls

open Mvc.Wpf

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract Left : int with get, set
    abstract Right : int with get, set
    abstract Result : int with get, set

[<STAThread>] 
do 
    let model : SampleModel = Model.Create()

    let left = TextBox()
    left.SetBinding(TextBox.TextProperty, "Left") |> ignore

    let right = TextBox()
    right.SetBinding(TextBox.TextProperty, "Right") |> ignore

    let result = TextBlock()
    result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

    let calculate = Button(Content = "Add")

    let panel = StackPanel()

    panel.Children.Add left |> ignore
    panel.Children.Add right |> ignore
    panel.Children.Add result |> ignore
    panel.Children.Add calculate |> ignore

    let window = Window(Title = "Simple model", Content = panel, Width = 200., Height = 200.)
    window.DataContext <- model

    calculate.Click.Add(fun _ -> model.Result <- model.Left + model.Right)

    Application().Run window |> ignore