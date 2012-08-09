open System
open System.Windows
open System.Windows.Controls

open Mvc.Wpf

type MyModel() = 
    inherit Model()

    let mutable left = 0
    let mutable right = 0
    let mutable result = 0

    abstract Left : int with get, set
    default this.Left with get() = left and set value = left <- value

    abstract Right : int with get, set
    default this.Right with get() = right and set value = right <- value

    abstract Result : int with get, set
    default this.Result with get() = result and set value = result <- value

[<STAThread>] 
do 
    let model = Model.Create<MyModel>()

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