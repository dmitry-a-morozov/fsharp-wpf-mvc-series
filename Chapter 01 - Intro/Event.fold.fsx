
type Events = 
    | Add of int 
    | Subtract of int

type View = IEvent<Events>  

type Model = { State : int }

type Controller = Events -> Model -> Model

type Mvc = Controller -> Model -> IEvent<Events> -> Model

