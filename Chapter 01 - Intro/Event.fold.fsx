
type Events = 
    | Add of int 
    | Subtract of int

type View = IEvent<Events>  

type Model = { State : int }

type Controller = (Events -> Model -> Model) -> Model -> IEvent<Events> -> Model



open Unchecked
let view = defaultof<View>
let model = defaultof<Model>
let eventHandler = defaultof<Events -> Model -> Model>
let controller = defaultof<Controller>

let result = view |> controller eventHandler model

