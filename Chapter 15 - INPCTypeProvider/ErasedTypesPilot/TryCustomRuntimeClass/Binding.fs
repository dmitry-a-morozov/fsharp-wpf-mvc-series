[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharp.Windows.Binding

open System.Reflection
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open System.ComponentModel

type PropertyInfo with
    member this.DependencyProperty = 
        let dpInfo = 
            this.DeclaringType.GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy)
        assert (dpInfo <> null)
        dpInfo.GetValue(null, [||]) |> unbox<DependencyProperty> 

let rec (|PropertyPath|_|) = function 
    | PropertyGet( Some( Value _), sourceProperty, []) -> Some sourceProperty.Name
    | Coerce( PropertyPath path, _) 
    | SpecificCall <@ string @> (None, _, [ PropertyPath path ]) -> Some path
    //Support for type provider erased types
    | Call((Some (Value (:? ICustomTypeDescriptor as model, _))), method', [ Value(:? string as propertyName, _)]) 
        when method'.Name = "get_Item" && model.GetProperties().Find(propertyName, ignoreCase = false) <> null -> Some propertyName
    | Call((Some (Value (:? ICustomTypeDescriptor as model, _))), method', [ Value(:? string as propertyName, _)]) 
        when method'.Name = "get_Item" && model.GetProperties().Find(propertyName, ignoreCase = false) <> null -> Some propertyName
    | Call((Some (Value (:? ICustomTypeProvider as model, _))), method', [ Value(:? string as propertyName, _)]) when method'.Name = "get_Item" -> 
        model.GetCustomType().GetProperties() |> Array.map(fun p -> p.Name) |> Array.tryFind propertyName.Equals
    | _ -> None

type Expr with
    member this.ToBindingExpr() = 
        match this with
        | PropertySet
            (
                Some( Value(:? FrameworkElement as target, _)),
                targetProperty, 
                [], 
                PropertyPath path
            ) ->
                let binding = Binding(path, ValidatesOnDataErrors = true)
                target.SetBinding(targetProperty.DependencyProperty, binding)
        | _ -> invalidArg "expr" (string this) 

type Binding with
    static member FromExpression expr = 
        let rec split = function 
            | Sequential(head, tail) -> head :: split tail
            | tail -> [ tail ]

        for e in split expr do
            let be = e.ToBindingExpr()
            assert not be.HasError
    

