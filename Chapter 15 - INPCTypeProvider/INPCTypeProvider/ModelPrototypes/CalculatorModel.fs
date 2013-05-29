namespace FSharp.Windows.Sample.Models

open Microsoft.FSharp.Reflection

type Operations =
    | Add
    | Subtract
    | Multiply
    | Divide

    override this.ToString() = sprintf "%A" this

    static member Values = 
        typeof<Operations>
        |> FSharpType.GetUnionCases
        |> Array.map(fun x -> FSharpValue.MakeUnion(x, [||]))
        |> Array.map unbox<Operations>

type CalculatorModel = {
    mutable AvailableOperations : Operations[] 
    mutable SelectedOperation : Operations 
    mutable X : int 
    mutable Y : int 
    mutable Result : int
} 


