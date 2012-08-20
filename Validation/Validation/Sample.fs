namespace Mvc.Wpf.Sample

open System
open System.Windows.Controls
open System.Windows.Data
open Microsoft.FSharp.Reflection
open Mvc.Wpf

type Operations =
    | Add
    | Subtract
    | Multiply
    | Divide

    override this.ToString() = sprintf "%A" this

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract AvailableOperations : Operations[] with get, set
    abstract SelectedOperation : Operations with get, set
    abstract X : int with get, set
    abstract Y : int with get, set
    abstract Result : int with get, set

type SampleEvents = 
    | Calculate
    | Clear 

type SampleView() =
    inherit View<SampleEvents, SampleModel, SampleWindow>()

    override this.EventStreams = 
        [
            this.Window.Calculate.Click |> Observable.mapTo Calculate
            this.Window.Clear.Click |> Observable.mapTo Clear
        ]

    override this.SetBindings model = 
        Binding.FromExpression 
            <@ 
                this.Window.Operation.ItemsSource <- model.AvailableOperations 
                this.Window.Operation.SelectedItem <- model.SelectedOperation
                this.Window.X.Text <- string model.X
                this.Window.Y.Text <- string model.Y 
                this.Window.Result.Text <- string model.Result 
            @>

type SimpleController(view : IView<_, _>) = 
    inherit Controller<SampleEvents, SampleModel>(view)

    override this.InitModel model = 
        model.AvailableOperations <- 
            typeof<Operations>
            |> FSharpType.GetUnionCases
            |> Array.map(fun x -> FSharpValue.MakeUnion(x, [||]))
            |> Array.map unbox
        model.SelectedOperation <- Operations.Add
        model.X <- 0
        model.Y <- 0
        model.Result <- 0

    override this.EventHandler = function
        | Calculate -> this.Calculate
        | Clear -> this.InitModel

    member this.Calculate model = 
        match model.SelectedOperation with
        | Add -> 
            if model.Y < 0 
            then 
                model |> Validation.positive <@ fun m -> m.Y @>
            else 
                model.Result <- model.X + model.Y
        | Subtract -> 
            if model.Y < 0 
            then 
                model |> Validation.positive <@ fun m -> m.Y @>
            else 
                model.Result <- model.X - model.Y
        | Multiply -> 
            model.Result <- model.X * model.Y
        | Divide -> 
            if model.Y = 0 
            then
                model |> Validation.setError <@ fun m -> m.Y @> "Attempted to divide by zero."
            else
                model.Result <- model.X / model.Y
