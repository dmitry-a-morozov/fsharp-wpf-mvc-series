open System
open System.Windows
open System.Windows.Controls

open Mvc.Wpf

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract Left : int with get, set
    abstract Right : int with get, set
    abstract Result : int with get, set

type SampleEvents = 
    | Add
    | Clear

type SampleView() as this =
    inherit XamlView<SampleEvents, SampleModel>(resourceLocator = Uri("/Window.xaml", UriKind.Relative))

    let addButton : Button = this ? Add
    let clearButton : Button = this ? Clear
    let left : TextBox = this ? Left
    let right : TextBox = this ? Right
    let result : TextBlock = this ? Result

    override this.EventStreams = 
        [
            addButton.Click |> Observable.map(fun _ -> Add)
            clearButton.Click |> Observable.map(fun _ -> Clear)
        ]

    override this.SetBindings model = 
        left.SetBinding(TextBox.TextProperty, "Left") |> ignore
        right.SetBinding(TextBox.TextProperty, "Right") |> ignore
        result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

[<STAThread>] 
do 
    let model = Model.Create()
    let view = SampleView()
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