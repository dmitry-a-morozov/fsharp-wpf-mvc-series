namespace Mvc.Wpf

open System
open System.ComponentModel
open System.Collections.Generic
open System.Reflection
open System.Linq
open System.Windows
open System.Windows.Data
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns 
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.ExprShape

open Castle.DynamicProxy

open Binding.Patterns

type NotifyDependencyChangedAttribute = ReflectedDefinitionAttribute

module ModelExtensions = 

    let (|PropertySetter|_|) (m : MethodInfo) =
        match m.Name.Split('_') with
        | [| "set"; propertyName |] -> assert m.IsSpecialName; Some propertyName
        | _ -> None

    let (|PropertyGetter|_|) (m : MethodInfo) =
        match m.Name.Split('_') with
        | [| "get"; propertyName |] -> assert m.IsSpecialName; Some propertyName
        | _ -> None

    let (|Abstract|_|) (m : MethodInfo) = if m.IsAbstract then Some() else None

    type Expr with

        member this.ExpandLetBindings() = 
            match this with 
            | Let(binding, expandTo, tail) -> 
                tail.Substitute(fun var -> if var = binding then Some expandTo else None).ExpandLetBindings() 
            | ShapeVar var -> Expr.Var(var)
            | ShapeLambda(var, body) -> Expr.Lambda(var, body.ExpandLetBindings())  
            | ShapeCombination(shape, exprs) -> ExprShape.RebuildShapeCombination(shape, exprs |> List.map(fun e -> e.ExpandLetBindings()))

        member this.Dependencies model = 
            seq {
                match this with 
                | SourceAndPropertyPath(path, Some(source)) when source = model -> yield path
                | ShapeVar _ -> ()
                | ShapeLambda(_, body) -> yield! body.Dependencies model   
                | ShapeCombination(_, exprs) -> for subExpr in exprs do yield! subExpr.Dependencies model
            }

    let (|DependentProperty|_|) (memberInfo : MemberInfo) = 
        match memberInfo with
        | :? MethodInfo as getter ->
            match getter with
            | PropertyGetter propertyName & MethodWithReflectedDefinition (Lambda (model, propertyBody)) -> 
                let binding = MultiBinding()
                let self = Binding(path = "", RelativeSource = RelativeSource.Self) 
                binding.Bindings.Add self

                for dependency in propertyBody.ExpandLetBindings().Dependencies(model).Distinct() do
                    binding.Bindings.Add <| Binding(path = dependency, RelativeSource = RelativeSource.Self)

                binding.Converter <- {
                    new IMultiValueConverter with

                        member this.Convert(values, _, _, _) = 
                            if values.Contains DependencyProperty.UnsetValue
                            then 
                                DependencyProperty.UnsetValue
                            else
                                try 
                                    let model = values.[0] 
                                    getter.Invoke(model, [||])
                                with _ ->
                                    DependencyProperty.UnsetValue

                        member this.ConvertBack(_, _, _, _) = undefined
                }
                Some(propertyName, getter.ReturnType, binding)
            | _ -> None
        | _ -> None


open ModelExtensions

type IInterceptorFilter = 
    abstract Applicable : (MethodInfo -> bool) with get 

[<AbstractClass>]
type Model() = 
    inherit DependencyObject()

    static let dependentProperties = Dictionary()
    static let proxyFactory = ProxyGenerator()

    static let options = 
        ProxyGenerationOptions(
            Hook = { 
                new IProxyGenerationHook with
                    member this.NonProxyableMemberNotification(_, member') = 
                        match member' with
                        | DependentProperty(propertyName, propertyType, binding) ->
                            let perTypeDependentProperties = 
                                match dependentProperties.TryGetValue member'.DeclaringType with 
                                | true, xs -> xs
                                | false, _ -> 
                                    let xs = List()
                                    dependentProperties.Add(member'.DeclaringType, xs)
                                    xs
                            let dp = DependencyProperty.Register(propertyName, propertyType, member'.DeclaringType)
                            perTypeDependentProperties.Add(dp, binding)
                        | _ -> ()
                    member this.ShouldInterceptMethod(_, method') = 
                        match method' with 
                        | PropertyGetter _ | PropertySetter _ -> method'.IsVirtual 
                        | _ -> false
                    member this.MethodsInspected() = ()
            },
            Selector = {
                new IInterceptorSelector with
                    member this.SelectInterceptors(_, method', interceptors) = 
                        interceptors |> Array.filter(function 
                            | :? IInterceptorFilter as filter -> filter.Applicable method'
                            | _ -> true
                        )
            } 
        )

    static let notifyPropertyChanged = {
        new StandardInterceptor() with
            member this.PostProceed invocation = 
                match invocation.Method, invocation.InvocationTarget with 
                    | PropertySetter propertyName, (:? Model as model) -> model.ClearError propertyName 
                    | _ -> ()
        interface IInterceptorFilter with 
            member this.Applicable = function | PropertySetter _ -> true | _ -> false
    }

    let errors = Dictionary()
    let propertyChangedEvent = Event<_,_>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

    member internal this.TriggerPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs propertyName)

    static member Create<'T when 'T :> Model and 'T : not struct>()  : 'T = 
        let interceptors : IInterceptor[] = [| notifyPropertyChanged; AbstractProperties() |]
        let model = proxyFactory.CreateClassProxy(options, interceptors)
        match dependentProperties.TryGetValue typeof<'T> with
        | true, xs ->
            for dp, binding in xs do 
                let bindingExpression = BindingOperations.SetBinding(model, dp, binding)
                assert not bindingExpression.HasError
        | false, _ -> ()
        model

    interface IDataErrorInfo with
        member this.Error = undefined
        member this.Item 
            with get propertyName = 
                match errors.TryGetValue propertyName with
                | true, message -> message
                | false, _ -> null

    member this.SetError(propertyName, message) = 
        errors.[propertyName] <- message
        this.TriggerPropertyChanged propertyName
    member this.ClearError propertyName = this.SetError(propertyName, null)
    member this.ClearAllErrors() = errors.Keys |> Seq.toArray |> Array.iter this.ClearError
    abstract HasErrors : bool
    default this.HasErrors = errors.Values |> Seq.exists (not << String.IsNullOrEmpty)
    member this.IsValid = not this.HasErrors

and AbstractProperties() =
    let data = Dictionary()

    interface IInterceptor with
        member this.Intercept invocation = 
            match invocation.Method with 
                | Abstract & PropertySetter propertyName -> 
                    data.[propertyName] <- invocation.Arguments.[0]

                | Abstract & PropertyGetter propertyName ->
                    match data.TryGetValue propertyName with 
                    | true, value -> invocation.ReturnValue <- value 
                    | false, _ -> 
                        let returnType = invocation.Method.ReturnType
                        if returnType.IsValueType then 
                            invocation.ReturnValue <- Activator.CreateInstance returnType

                | _ -> invocation.Proceed()

    interface IInterceptorFilter with 
        member this.Applicable = function | Abstract & (PropertySetter _ | PropertyGetter _) -> true | _ -> false

[<RequireQualifiedAccess>]
module Controller = 

    let inline start(controller : SupervisingController<_, ^Model>) = 
        let model = (^Model : (static member Create : unit -> ^Model) ())
        if controller.Start model then Some model else None

    let inline asyncStart(controller : SupervisingController<_, ^Model>) = 
        async {
            let model = (^Model : (static member Create : unit -> ^Model) ())
            let! isOk = controller.AsyncStart model
            return if isOk then Some model else None
        }
