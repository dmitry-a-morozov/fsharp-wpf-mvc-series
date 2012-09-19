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

type SampleWindow() as this = 
    inherit Window(Title = "Simple view", Width = 200., Height = 200.)

    let x = TextBox()
    let y = TextBox()
    let result = TextBlock()
    let add = Button(Content = "Add")
    let clear = Button(Content = "Clear")
    let panel = StackPanel()
    do
        panel.Children.Add x |> ignore
        panel.Children.Add y |> ignore
        panel.Children.Add result |> ignore
        panel.Children.Add add |> ignore
        panel.Children.Add clear |> ignore
        
        this.Content <- panel

    member this.X = x
    member this.Y = y
    member this.Result = result
    member this.Add = add
    member this.Clear = clear

type SampleEvents = 
    | Add
    | Clear

type SampleView() =
    inherit View<SampleEvents, SampleModel, SampleWindow>()

    override this.EventStreams = 
        [
            this.Window.Add.Click |> Observable.map(fun _ -> Add)
            this.Window.Clear.Click |> Observable.map(fun _ -> Clear)
        ]

    override this.SetBindings model = 
        this.Window.X.SetBinding(TextBox.TextProperty, "X") |> ignore
        this.Window.Y.SetBinding(TextBox.TextProperty, "Y") |> ignore
        this.Window.Result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

[<STAThread>] 
do 
    let model = SampleModel.Create()
    let view = SampleView()
    let iview = view :> IView<_, _>
    iview.SetBindings model
    iview.Add(callback = function 
        | Add -> model.Result <- model.X + model.Y
        | Clear -> 
            model.X <- 0
            model.Y <- 0
            model.Result <- 0
    )

    Application().Run view.Window |> ignore