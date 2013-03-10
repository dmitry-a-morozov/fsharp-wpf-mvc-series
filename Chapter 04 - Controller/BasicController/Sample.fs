
namespace FSharp.Windows.Sample

open System
open System.Windows.Controls
open System.Windows

open FSharp.Windows

[<AbstractClass>]
type SampleModel() = 
    inherit Model()

    abstract X : int with get, set
    abstract Y : int with get, set
    abstract Result : int with get, set

    abstract Title : string with get, set

type SampleEvents = 
    | Add
    | Subtract of int * int
    | Clear

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

type SampleController() = 

    interface IController<SampleEvents, SampleModel> with 
        member this.InitModel model = 
            model.X <- 0
            model.Y <- 0
            model.Result <- 0

            model.Title <- 
                sprintf "Process name: %s" <| System.Diagnostics.Process.GetCurrentProcess().ProcessName

        member this.EventHandler = function
            | Add -> this.Add
            | Subtract(x, y) -> this.Subtract x y
            | Clear -> (this :> IController<_, _>).InitModel
    
    member this.Add(model : SampleModel) = 
        model.Result <- model.X + model.Y
        
    member this.Subtract x y (model : SampleModel) =  
        model.Result <- x - y
