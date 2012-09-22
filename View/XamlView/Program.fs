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
    inherit XamlView<SampleEvents>(resourceLocator = Uri("/Window.xaml", UriKind.Relative))

    let add : Button = this ? Add
    let clear : Button = this ? Clear
    let x : TextBox = this ? X
    let y : TextBox = this ? Y
    let result : TextBlock = this ? Result

    override this.EventStreams = 
        [
            add.Click |> Observable.map(fun _ -> Add)
            clear.Click |> Observable.map(fun _ -> Clear)
        ]

    override this.SetBindings model = 
        x.SetBinding(TextBox.TextProperty, "X") |> ignore
        y.SetBinding(TextBox.TextProperty, "Y") |> ignore
        result.SetBinding(TextBlock.TextProperty, "Result") |> ignore

[<STAThread>] 
do 
    let model : SampleModel = Model.Create()
    let view = SampleView()
    let iview = view :> IView<_>
    iview.SetBindings model
    iview.Add(callback = function
        | Add -> model.Result <- model.X + model.Y
        | Clear -> 
            model.X <- 0
            model.Y <- 0
            model.Result <- 0
    )

    Application().Run view.Window |> ignore