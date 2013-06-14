module FSharp.Windows.INPCTypeProvider.DerivedProperties

open System
open System.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.ExprShape

type PropertyInfo with
    member internal this.IsNullableValue =  
        this.DeclaringType.IsGenericType && this.DeclaringType.GetGenericTypeDefinition() = typedefof<Nullable<_>> && this.Name = "Value"

let (|PropertyPathOfDependency|_|) this expr = 
    let rec loop e acc = 
        match e with
        | PropertyGet( Some tail, property, []) -> 
            if property.IsNullableValue 
            then loop tail acc 
            else loop tail (property.Name :: acc)
        | Var x when x = this -> acc
        | _ -> []

    match loop expr [] with
    | [] -> None
    | xs -> xs |> String.concat "." |> Some

let rec expandLetBindings = function
    | Let(binding, expandTo, tail) -> 
        tail.Substitute(fun var -> if var = binding then Some expandTo else None) |> expandLetBindings
    | ShapeVar var -> Expr.Var(var)
    | ShapeLambda(var, body) -> Expr.Lambda(var, expandLetBindings body)  
    | ShapeCombination(shape, exprs) -> ExprShape.RebuildShapeCombination(shape, exprs |> List.map expandLetBindings)

let rec extractDependencies this propertyBody = 
    seq {
        match propertyBody with 
        | PropertyPathOfDependency this path -> yield path
        | ShapeVar _ -> ()
        | ShapeLambda(_, body) -> yield! extractDependencies this body   
        | ShapeCombination(_, exprs) -> for subExpr in exprs do yield! extractDependencies this subExpr
    }

let getPropertyDependencies model propertyBody = 
    propertyBody
        |> expandLetBindings
        |> extractDependencies model
        |> Seq.distinct 



