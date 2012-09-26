namespace Mvc.Wpf

open System
open System.ComponentModel
open System.Collections.Generic
open System.Reflection
open Castle.DynamicProxy

module MethodInfo = 
    let (|PropertySetter|_|) (m : MethodInfo) =
        match m.Name.Split('_') with
        | [| "set"; propertyName |] -> assert m.IsSpecialName; Some propertyName
        | _ -> None

    let (|PropertyGetter|_|) (m : MethodInfo) =
        match m.Name.Split('_') with
        | [| "get"; propertyName |] -> assert m.IsSpecialName; Some propertyName
        | _ -> None

    let (|Abstract|_|) (m : MethodInfo) = if m.IsAbstract then Some() else None

open MethodInfo

[<AbstractClass>]
type Model() = 

    static let proxyFactory = ProxyGenerator()

    static let notifyPropertyChanged = 
        {
            new StandardInterceptor() with
                member this.PostProceed invocation = 
                    match invocation.Method, invocation.InvocationTarget with 
                        | PropertySetter propertyName, (:? Model as model) -> model.TriggerPropertyChanged propertyName
                        | _ -> ()
        }

    let propertyChangedEvent = Event<_,_>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

    member internal this.TriggerPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs propertyName)

    static member Create<'T when 'T :> Model and 'T : not struct>()  : 'T = 
        let interceptors : IInterceptor[] = [| notifyPropertyChanged; AbstractProperties() |]
        proxyFactory.CreateClassProxy interceptors    

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

