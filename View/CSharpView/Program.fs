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

type SampleEvents = 
    | Add
    | Clear

type SampleView() as this =
    inherit View<SampleEvents, SampleModel, SampleWindow>()

    let x : TextBox = this ? LHS //dynamic lookup
    let y : TextBox = this ? RHS //dynamic lookup

    override this.EventStreams = 
        [
            this.Window.Add.Click |> Observable.map(fun _ -> Add)
            this.Window.Clear.Click |> Observable.map(fun _ -> Clear)
        ]

    override this.SetBindings model = 
        x.SetBinding(TextBox.TextProperty, "X") |> ignore
        y.SetBinding(TextBox.TextProperty, "Y") |> ignore
        //strong typed reference to this.Window.Result. Compare it with dynamically looked up x and y TextBoxes
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