
namespace Mvc.Wpf.Sample

open Mvc.Wpf
open System
open System.Windows.Controls

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract X : string with get, set
    abstract Y : string with get, set
    abstract Result : string with get, set

type SampleEvents = 
    | Add
    | Clear
    | Subtract of int * int

type SampleView() =
    inherit View<SampleEvents, SampleModel, SampleWindow>()

    override this.EventStreams = 
        [
            this.Window.Add.Click |> Observable.map(fun _ -> Add)
            this.Window.Subtract.Click |> Observable.map(fun _ -> Subtract(int this.Window.X.Text, int this.Window.Y.Text))
            this.Window.Clear.Click |> Observable.map(fun _ -> Clear)
        ]

    override this.SetBindings model = 
        <@ this.Window.X.Text <- model.X @>.AsBinding()
        <@ this.Window.Y.Text <- model.Y @>.AsBinding()
        <@ this.Window.Result.Text <- model.Result @>.AsBinding()

type SimpleController(view : IView<_, _>) = 
    inherit Controller<SampleEvents, SampleModel>(view)

    override this.InitModel model = 
        model.X <- "0"
        model.Y <- "0"
        model.Result <- "0"

    override this.EventHandler = function
        | Add -> this.Add
        | Clear -> this.InitModel
        | Subtract(x, y) -> this.Subtract x y

    member this.Add model = 
        model.Result <- int model.X + int model.Y |> string
        
    member this.Subtract x y model = 
        model.Result <- int x - int y |> string
