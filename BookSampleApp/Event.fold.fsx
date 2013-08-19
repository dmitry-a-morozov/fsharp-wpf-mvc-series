open System

type View = IObservable<Events>  
and Events = Incr | Decr

type Model = { mutable State : int }
type Controller = Model -> Events -> unit
type Mvc = Controller -> Model -> IObservable<Events> -> IDisposable

let subject = Event<_>()
let raiseEvents xs = List.iter subject.Trigger xs
let view = subject.Publish

let model : Model = { State = 6 }

let controller model event = 
    match event with
    | Incr -> model.State <- model.State + 1 
    | Decr -> model.State <- model.State - 1

let mvc : Mvc = fun controller model view -> 
    view.Subscribe(fun event -> 
        controller model event 
        printfn "Model: %A" model)

let subscription = view |> mvc controller model

raiseEvents [Incr; Decr; Incr]

subscription.Dispose()
