
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

type SimpleView() as this =
    inherit XamlView<SampleEvents, SampleModel>(resourceLocator = Uri("/Window.xaml", UriKind.Relative))

    let addButton : Button = this ? Add
    let subtractButton : Button = this ? Subtract
    let clearButton : Button = this ? Clear
    let x : TextBox = this ? X
    let y : TextBox = this ? Y
    let result : TextBlock = this ? Result

    override this.EventStreams = 
        [
            addButton.Click |> Observable.map(fun _ -> Add)
            clearButton.Click |> Observable.map(fun _ -> Clear)
            subtractButton.Click |> Observable.map(fun _ -> Subtract(int x.Text, int y.Text))
        ]

    override this.SetBindings model = 
        x.SetBinding(TextBox.TextProperty, "X") |> ignore
        y.SetBinding(TextBox.TextProperty, "Y") |> ignore
        result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

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
