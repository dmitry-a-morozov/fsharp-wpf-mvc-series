namespace Mvc.Wpf

open System
open System.Windows

type IView<'Event, 'Model> =
    inherit IObservable<'Event>

    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type View<'Event, 'Model, 'Window when 'Window :> Window and 'Window : (new : unit -> 'Window)>() = 

    let window = new 'Window()
    member this.Window = window
    
    interface IView<'Event, 'Model> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model; 
            this.SetBindings model

    abstract EventStreams : IObservable<'Event> list
    abstract SetBindings : 'Model -> unit

