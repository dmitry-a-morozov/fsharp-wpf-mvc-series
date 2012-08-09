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
    | Subtract of int * int

type SimpleView() as this =
    inherit XamlView<SimpleEvents, SimpleModel>(resourceLocator = Uri("/Window.xaml", UriKind.Relative))

    let addButton : Button = this ? Add
    let subtractButton : Button = this ? Subtract
    let clearButton : Button = this ? Clear
    let left : TextBox = this ? Left
    let right : TextBox = this ? Right
    let result : TextBlock = this ? Result

    override this.EventStreams = 
        [
            addButton.Click |> Observable.map(fun _ -> Add)
            clearButton.Click |> Observable.map(fun _ -> Clear)
            subtractButton.Click |> Observable.map(fun _ -> Subtract(int left.Text, int right.Text))
        ]

    override this.SetBindings model = 
        left.SetBinding(TextBox.TextProperty, "Left") |> ignore
        right.SetBinding(TextBox.TextProperty, "Right") |> ignore
        result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

type SimpleController(view : IView<_, _>) = 
    inherit Controller<SimpleEvents, SimpleModel>(view)

    override this.InitModel model = 
        model.Left <- 0
        model.Right <- 0
        model.Result <- 0

    override this.EventHandler = function
        | Add -> this.Add
        | Clear -> this.InitModel
        | Subtract(left, right) -> this.Subtract left right

    member this.Add model = 
        model.Result <- model.Left + model.Right
        
    member this.Subtract left right model = 
        model.Result <- left - right
        
[<STAThread>] 
do 
    let model = Model.Create()
    let view = SimpleView()
    let controller = SimpleController(view)
    controller.Start model |> ignore
    Application().Run view.Window |> ignore