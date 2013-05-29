namespace SampleModelPrototypes

type Operations =
    | Add = 0
    | Subtract = 1
    | Multiply = 2
    | Divide = 3

type Calculator = 
    {
        mutable AvailableOperations : Operations[] 
        mutable SelectedOperation : Operations
        mutable X : int
        mutable Y : int
        mutable Result : int
    }

    [<ReflectedDefinition>]
    member self.Title = sprintf "%i %A %i" self.X self.SelectedOperation self.Y 
