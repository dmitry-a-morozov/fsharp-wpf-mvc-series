[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mvc.Wpf.Binding

open System
open System.Collections.Generic
open System.Reflection
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns

type IValueConverter with
    static member OneWay convert =  
        {
            new IValueConverter with
                member this.Convert(value, _, _, _) = try value |> unbox |> convert |> box with _ -> DependencyProperty.UnsetValue
                member this.ConvertBack(value, _, _, _) = DependencyProperty.UnsetValue
        }

type IEnumerable<'T> with
    member this.CurrentItem : 'T = undefined

module BindingPatterns = 

    let (|Target|_|) expr = 
        let rec loop = function
            | Some( Value(obj, viewType) ) -> obj
            | Some( FieldGet(tail, field) ) ->  field.GetValue(loop tail)
            | Some( PropertyGet(tail, prop, []) ) -> prop.GetValue(loop tail, [||])
            | _ -> null
        match loop expr with
        | :? DependencyObject as target -> Some target
        | _ -> None

    let rec (|PropertyPath|_|) = function 
        | PropertyGet( Some( Value _), sourceProperty, []) -> Some sourceProperty.Name
        | _ -> None

    let (|StringFormat|_|) = function
        | SpecificCall <@ String.Format : string * obj -> string @> (None, [], [ Value(:? string as format, _); Coerce( propertyPath, _) ]) ->
            Some(format, propertyPath)
        | _ -> None    

    let (|Nullable|_|) = function
        | NewObject( ctorInfo, [ propertyPath ] ) when ctorInfo.DeclaringType.GetGenericTypeDefinition() = typedefof<Nullable<_>> -> 
            Some propertyPath
        | _ -> None    

    let (|Converter|_|) = function
        | Call(None, methodInfo, [ propertyPath ]) -> 
            assert (methodInfo.IsStatic && methodInfo.GetParameters().Length = 1)
            Some((fun(value : obj) -> methodInfo.Invoke(null, [| value |])), propertyPath )
        | _ -> None    
         
    let rec (|BindingExpression|) = function
        | Coerce( BindingExpression binding, _) 
        | SpecificCall <@ string @> (None, _, [ BindingExpression binding ]) 
        | Nullable( BindingExpression binding) -> binding
        | PropertyPath path -> Binding path
        | StringFormat(format, PropertyPath path) -> Binding(path, StringFormat = format)
        | Converter(convert, PropertyPath path) -> Binding(path, Converter = IValueConverter.OneWay convert, Mode = BindingMode.OneWay)
        | expr -> invalidArg "binding property path quotation" (string expr)

type PropertyInfo with
    member this.DependencyProperty : DependencyProperty = 
        this.DeclaringType
            .GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy)
            .GetValue(null, [||]) 
            |> unbox

open BindingPatterns

type Expr with
    member this.ToBindingExpr(?updateSourceTrigger) = 
        match this with
        | PropertySet(Target target, targetProperty, [], BindingExpression binding) ->
            binding.ValidatesOnDataErrors <- true
            binding.ValidatesOnExceptions <- true
            if updateSourceTrigger.IsSome then binding.UpdateSourceTrigger <- updateSourceTrigger.Value
            BindingOperations.SetBinding(target, targetProperty.DependencyProperty, binding)
        | _ -> invalidArg "expr" (string this) 

type Binding with
    static member FromExpression(expr, ?updateSourceTrigger) =
        let rec split = function 
            | Sequential(head, tail) -> head :: split tail
            | tail -> [ tail ]

        for e in split expr do
            let be = e.ToBindingExpr(?updateSourceTrigger = updateSourceTrigger)
            assert not be.HasError
    
    static member UpdateSourceOnChange(expr : Expr) = Binding.FromExpression(expr, updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)

