
namespace FSharp.Windows.Sample

open System
open System.Windows.Controls
open System.Collections

open FSharp.Windows

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
        //Binding to property of type string
        <@ this.Window.X.Text <- model.X @>.ToBindingExpr()
        <@ this.Window.Y.Text <- model.Y @>.ToBindingExpr()
        <@ this.Window.Result.Text <- model.Result @>.ToBindingExpr()
        //Binding to property of type IEnumerable
        <@ this.Window.Operation.ItemsSource <- model.AvailableOperations @>.ToBindingExpr()
        //Binding to property of type obj
        <@ this.Window.Operation.SelectedItem <- model.SelectedOperation @>.ToBindingExpr()

type SimpleController() = 

    interface IController<SampleEvents, SampleModel> with 
        member this.InitModel model = 
            model.AvailableOperations <- Enum.GetValues typeof<Operations>
            model.SelectedOperation <- Operations.Add
            model.X <- "0"
            model.Y <- "0"
            model.Result <- "0"

        member this.EventHandler = function
            | Calculate -> this.Calculate
            | Clear -> (this :> IController<_, _>).InitModel

    member this.Calculate(model : SampleModel) = 
        match model.SelectedOperation with
        | (:? Operations as op) when op = Operations.Add -> model.Result <- int model.X + int model.Y |> string
        | (:? Operations as op) when op = Operations.Subtract -> model.Result <- int model.X - int model.Y |> string
        | op -> invalidArg "Op" (string op)
        
