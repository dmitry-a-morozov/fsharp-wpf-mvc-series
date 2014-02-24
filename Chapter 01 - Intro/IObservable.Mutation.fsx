open System

type Events = Incr | Decr
type View = IObservable<Events>  
type Model = { mutable State : int }
type Controller = Model -> Events -> unit
type Mvc = Controller -> Model -> View -> IDisposable

let subject = new Event<Events>()
let raiseEvents xs = xs |> List.iter subject.Trigger
let view = subject.Publish

let model : Model = { State = 6 }

let controller model event = 
    match event with
    | Incr -> model.State <- model.State + 1
    | Decr -> model.State <- model.State - 1 

let mvc : Mvc = fun controller model view -> 
    view.Subscribe(fun event -> 
        controller model event; 
        printfn "Model: %A" model
    )

let subscription = view |> mvc controller model

raiseEvents [Incr; Decr; Incr]

subscription.Dispose()
