
namespace SampleApp

open System
open System.Windows.Controls
open System.Windows
open System.Windows.Data
open SampleApp

open FSharp.Windows

[<AbstractClass>]
type MainModel() = 
    inherit Model()

    abstract X : int with get, set
    abstract Y : int with get, set
    abstract Result : int with get, set

    abstract Title : string with get, set

type MainPageEvents = 
    | Add
    | Subtract of int * int
    | Clear

type MainView(control) =
    inherit View<MainPageEvents, MainPage>(control)

    override this.EventStreams = 
        [
            this.Control.Add.Click |> Observable.map(fun _ -> Add)
            this.Control.Clear.Click |> Observable.map(fun _ -> Clear)
            this.Control.Subtract.Click |> Observable.map(fun _ -> Subtract(int this.Control.X.Text, int this.Control.Y.Text))
        ]

    override this.SetBindings model = 
        this.Control.X.SetBinding(TextBox.TextProperty, Binding("X", Mode = BindingMode.TwoWay)) |> ignore
        this.Control.Y.SetBinding(TextBox.TextProperty, Binding("Y", Mode = BindingMode.TwoWay)) |> ignore
        this.Control.Result.SetBinding(TextBlock.TextProperty, Binding("Result")) |> ignore

type MainController() = 

    interface IController<MainPageEvents, MainModel> with 
        member this.InitModel model = 
            model.X <- 0
            model.Y <- 0
            model.Result <- 0

        member this.EventHandler = function
            | Add -> this.Add
            | Subtract(x, y) -> this.Subtract x y
            | Clear -> (this :> IController<_, _>).InitModel
    
    member this.Add(model : MainModel) = 
        model.Result <- model.X + model.Y
        
    member this.Subtract x y (model : MainModel) =  
        model.Result <- x - y
