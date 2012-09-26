
namespace Mvc.Wpf.Sample

open System
open System.Windows.Controls
open System.Windows

open Mvc.Wpf

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract X : int with get, set
    abstract Y : int with get, set
    abstract Result : int with get, set

    abstract Title : string with get, set

type SampleEvents = 
    | Add
    | Clear
    | Subtract of int * int

type SampleView() =
    inherit View<SampleEvents, SampleWindow>()

    override this.EventStreams = 
        [
            this.Window.Add.Click |> Observable.map(fun _ -> Add)
            this.Window.Clear.Click |> Observable.map(fun _ -> Clear)
            this.Window.Subtract.Click |> Observable.map(fun _ -> Subtract(int this.Window.X.Text, int this.Window.Y.Text))
        ]

    override this.SetBindings model = 
        this.Window.X.SetBinding(TextBox.TextProperty, "X") |> ignore
        this.Window.Y.SetBinding(TextBox.TextProperty, "Y") |> ignore
        this.Window.Result.SetBinding(TextBlock.TextProperty, "Result") |> ignore
        this.Window.SetBinding(Window.TitleProperty, "Title") |> ignore

type SimpleController(view) = 
    inherit Controller<SampleEvents, SampleModel>(view)

    override this.InitModel model = 
        model.X <- 0
        model.Y <- 0
        model.Result <- 0

        model.Title <- 
            sprintf "Process name: %s" <| System.Diagnostics.Process.GetCurrentProcess().ProcessName

    override this.EventHandler = function
        | Add -> this.Add
        | Clear -> this.InitModel
        | Subtract(x, y) -> this.Subtract x y

    member this.Add model = 
        model.Result <- model.X + model.Y
        
    member this.Subtract x y model = 
        model.Result <- x - y
