namespace Mvc.Wpf

open System
open System.Windows

type IView<'E, 'M> =
    inherit IObservable<'E>

    abstract SetBindings : 'M -> unit

[<AbstractClass>]
type View<'E, 'M, 'W when 'W :> Window and 'W : (new : unit -> 'W)>() = 

    let window = new 'W()
    member this.Window = window
    
    interface IView<'E, 'M> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model; 
            this.SetBindings model

    abstract EventStreams : IObservable<'E> list
    abstract SetBindings : 'M -> unit

