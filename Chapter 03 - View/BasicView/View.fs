namespace FSharp.Windows

open System
open System.Windows

type IView<'Events> =
    inherit IObservable<'Events>

    abstract SetBindings : obj -> unit

[<AbstractClass>]
type View<'Events, 'Window when 'Window :> Window and 'Window : (new : unit -> 'Window)>() = 

    let window = new 'Window()
    member this.Window = window
    
    interface IView<'Events> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model
            this.SetBindings model

    abstract EventStreams : IObservable<'Events> list
    abstract SetBindings : obj -> unit

