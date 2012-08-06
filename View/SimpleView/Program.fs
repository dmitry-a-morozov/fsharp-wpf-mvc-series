open System
open System.Windows
open System.Windows.Controls

open Mvc.Wpf

[<AbstractClass>]
type SimpleModel() = 
    inherit Model()

    abstract Left : int with get, set
    abstract Right : int with get, set
    abstract Result : int with get, set

type SimpleWindow() as this = 
    inherit Window(Title = "Simple view", Width = 200., Height = 200.)

    let left = TextBox()
    let right = TextBox()
    let result = TextBlock()
    let add = Button(Content = "Add")
    let clear = Button(Content = "Clear")
    let panel = StackPanel()
    do
        panel.Children.Add left |> ignore
        panel.Children.Add right |> ignore
        panel.Children.Add result |> ignore
        panel.Children.Add add |> ignore
        panel.Children.Add clear |> ignore
        
        this.Content <- panel

    member this.Left = left
    member this.Right = right
    member this.Result = result
    member this.Add = add
    member this.Clear = clear

type SimpleEvents = 
    | Add
    | Clear

type SimpleView() =
    inherit View<SimpleEvents, SimpleModel, SimpleWindow>()

    override this.EventStreams = 
        [
            this.Window.Add.Click |> Observable.map(fun _ -> Add)
            this.Window.Clear.Click |> Observable.map(fun _ -> Clear)
        ]

    override this.SetBindings model = 
        this.Window.Left.SetBinding(TextBox.TextProperty, "Left") |> ignore
        this.Window.Right.SetBinding(TextBox.TextProperty, "Right") |> ignore
        this.Window.Result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

[<STAThread>] 
do 
    let model = Model.Create<SimpleModel>()
    let view = SimpleView()
    let iview = view :> IView<_, _>
    iview.SetBindings model
    iview |> Observable.add(function
        | Add -> model.Result <- model.Left + model.Right
        | Clear -> 
            model.Left <- 0
            model.Right <- 0
            model.Result <- 0
    )

    Application().Run view.Window |> ignore