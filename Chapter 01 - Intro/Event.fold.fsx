

type Model = { State : int }

type View = IEvent<Events>  
and Events = Incr | Decr

type Controller = Model -> Events -> Model

type Mvc = Controller -> Model -> IEvent<Events> -> Model

