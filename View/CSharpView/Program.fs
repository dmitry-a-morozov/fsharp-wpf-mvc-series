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
        this.Window.LeftArg.SetBinding(TextBox.TextProperty, "Left") |> ignore
        this.Window.RightArg.SetBinding(TextBox.TextProperty, "Right") |> ignore
        this.Window.Result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

[<STAThread>] 
do 
    let model = Model.Create<SimpleModel>()
    let view = SimpleView()
    let iview = view :> IView<_, _>
    iview.SetBindings(model)
    iview |> Observable.add(function
        | Add -> model.Result <- model.Left + model.Right
        | Clear -> 
            model.Left <- 0
            model.Right <- 0
            model.Result <- 0
    )

    Application().Run view.Window |> ignore