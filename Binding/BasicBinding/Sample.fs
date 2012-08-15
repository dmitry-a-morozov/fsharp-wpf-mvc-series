
namespace Mvc.Wpf.Sample

open System
open System.Windows.Controls
open System.Collections

open Mvc.Wpf

type Operations =
    | Add = 0
    | Subtract = 1 

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract AvailableOperations : IEnumerable with get, set
    abstract SelectedOperation : obj with get, set
    abstract X : string with get, set
    abstract Y : string with get, set
    abstract Result : string with get, set

type SampleEvents = 
    | Calculate
    | Clear 

type SampleView() =
    inherit View<SampleEvents, SampleModel, SampleWindow>()

    override this.EventStreams = 
        [
            this.Window.Calculate.Click |> Observable.map(fun _ -> Calculate)
            this.Window.Clear.Click |> Observable.map(fun _ -> Clear)
        ]

    override this.SetBindings model = 

        <@ this.Window.Operation.ItemsSource <- model.AvailableOperations @>.ToBindingExpr()
        <@ this.Window.Operation.SelectedItem <- model.SelectedOperation @>.ToBindingExpr()

        <@ this.Window.X.Text <- model.X @>.ToBindingExpr()
        <@ this.Window.Y.Text <- model.Y @>.ToBindingExpr()

        <@ this.Window.Result.Text <- model.Result @>.ToBindingExpr()

type SimpleController(view : IView<_, _>) = 
    inherit Controller<SampleEvents, SampleModel>(view)

    override this.InitModel model = 
        model.AvailableOperations <- Enum.GetValues typeof<Operations>
        model.SelectedOperation <- Operations.Add
        model.X <- "0"
        model.Y <- "0"
        model.Result <- "0"

    override this.EventHandler = function
        | Calculate -> this.Calculate
        | Clear -> this.InitModel

    member this.Calculate model = 
        match model.SelectedOperation with
        | (:? Operations as op) when op = Operations.Add -> model.Result <- int model.X + int model.Y |> string
        | (:? Operations as op) when op = Operations.Subtract -> model.Result <- int model.X - int model.Y |> string
        | op -> invalidArg "Op" (string op)
        
