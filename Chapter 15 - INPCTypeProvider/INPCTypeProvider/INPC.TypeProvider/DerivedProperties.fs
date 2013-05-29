module FSharp.Windows.INPCTypeProvider.DerivedProperties

open System
open System.Collections.Generic
open System.Reflection
open System.Windows
open System.Windows.Data
open System.Diagnostics

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.ExprShape

type MemberInfo with
    member internal this.IsNullableMember =  
        this.DeclaringType.IsGenericType && this.DeclaringType.GetGenericTypeDefinition() = typedefof<Nullable<_>>

let (|SourceAndPropertyPath|_|) expr = 
    let source = ref None
    let rec loop e acc = 
        match e with
        | PropertyGet( Some tail, property, []) -> 
            Debug.WriteLineIf(
                List.isEmpty acc && (let setter = property.GetSetMethod() in setter <> null && not setter.IsVirtual), 
                sprintf "Binding to non-virtual writeable property: %O" property
            )
            //if property.IsNullable then acc else loop tail (property.Name :: acc)
            if property.IsNullableMember && property.Name = "Value" 
            then loop tail acc
            else loop tail (property.Name :: acc)

        | Value _ -> acc
        | Var v ->      
            source := Some v
            acc
        | _ -> []

    match loop expr [] with
    | [] -> None
    | x::_ as xs ->
        xs 
        |> Seq.pairwise 
        |> Seq.map (function 
            | "/", x -> x 
            | _, "/" -> "/" 
            | x, y -> "." + y) 
        |> String.concat ""
        |> ((+) x)
        |> fun propetyPath -> Some(!source, propetyPath)

type Expr with

    member this.ExpandLetBindings() = 
        match this with 
        | Let(binding, expandTo, tail) -> 
            tail.Substitute(fun var -> if var = binding then Some expandTo else None).ExpandLetBindings() 
        | ShapeVar var -> Expr.Var(var)
        | ShapeLambda(var, body) -> Expr.Lambda(var, body.ExpandLetBindings())  
        | ShapeCombination(shape, exprs) -> ExprShape.RebuildShapeCombination(shape, exprs |> List.map(fun e -> e.ExpandLetBindings()))

    member this.Dependencies = 
        seq {
            match this with 
            | SourceAndPropertyPath x -> yield x
            | ShapeVar _ -> ()
            | ShapeLambda(_, body) -> yield! body.Dependencies   
            | ShapeCombination(_, exprs) -> for subExpr in exprs do yield! subExpr.Dependencies 
        }

let derivedProperties = Dictionary()

let get (prototype : Type, targetType : Type) = 
    match derivedProperties.TryGetValue targetType with
    | true, xs -> xs
    | false, _ -> 
        let getPrototypeInstance = 
            let p = targetType.GetProperty("Prototype", BindingFlags.NonPublic ||| BindingFlags.Instance)
            fun model -> p.GetValue model
        let xs = [
            for p in prototype.GetProperties() do
                match p with
                | PropertyGetterWithReflectedDefinition (Lambda (model, propertyBody)) when not p.CanWrite ->

                    let binding = MultiBinding()
                    let self = Binding(RelativeSource = RelativeSource.Self) 
                    binding.Bindings.Add self

                    propertyBody
                        .ExpandLetBindings()
                        .Dependencies
                        |> Seq.distinct 
                        |> Seq.choose(function 
                            | Some source, path when source = model -> Some(Binding(path, RelativeSource = RelativeSource.Self))
                            | _ -> None)
                        |> Seq.iter binding.Bindings.Add

                    binding.Converter <- {
                        new IMultiValueConverter with

                            member this.Convert(values, _, _, _) = 
                                if values |> Array.exists (fun x -> x = DependencyProperty.UnsetValue)
                                then 
                                    DependencyProperty.UnsetValue
                                else
                                    try 
                                        let model = getPrototypeInstance values.[0] 
                                        p.GetValue(model)
                                    with _ ->
                                        DependencyProperty.UnsetValue

                            member this.ConvertBack(_, _, _, _) = raise <| NotImplementedException()
                    }
                    let dp = DependencyProperty.Register(p.Name, p.PropertyType, targetType)
                    yield dp, binding
                | _ -> ()
        ]
        derivedProperties.Add(targetType, xs)
        xs

