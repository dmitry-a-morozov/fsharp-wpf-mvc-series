namespace FSharp.Windows.Sample

open System
open System.Windows.Controls
open System.Windows.Data
open Microsoft.FSharp.Reflection
open FSharp.Windows

type Operations =
    | Add
    | Subtract

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
            this.Window.Calculate.Click |> Observable.map(fun _ -> Calculate)
            this.Window.Clear.Click |> Observable.map(fun _ -> Clear)
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

type SimpleController(view) = 
    inherit Controller<SampleEvents, SampleModel>()

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
        | Add -> model.Result <- model.X + model.Y
        | Subtract -> model.Result <- model.X - model.Y
