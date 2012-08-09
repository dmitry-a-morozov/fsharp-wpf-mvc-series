
namespace Mvc.Wpf.Sample

open Mvc.Wpf
open System
open System.Windows.Controls

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract X : int with get, set
    abstract Y : int with get, set
    abstract Result : int with get, set

type SampleEvents = 
    | Add
    | Clear
    | Subtract of int * int

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

type SimpleController(view : IView<_, _>) = 
    inherit Controller<SampleEvents, SampleModel>(view)

    override this.InitModel model = 
        model.X <- 0
        model.Y <- 0
        model.Result <- 0

    override this.EventHandler = function
        | Add -> this.Add
        | Clear -> this.InitModel
        | Subtract(x, y) -> this.Subtract x y

    member this.Add model = 
        model.Result <- model.X + model.Y
        
    member this.Subtract x y model = 
        model.Result <- x - y
