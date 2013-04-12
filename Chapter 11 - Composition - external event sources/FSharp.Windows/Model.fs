namespace FSharp.Windows

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
                        | PropertySetter propertyName, (:? Model as model) -> model.ClearError propertyName 
                        | _ -> ()
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
        proxyFactory.CreateClassProxy interceptors    

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
    member this.HasErrors = errors.Values |> Seq.exists (not << String.IsNullOrEmpty)

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

[<RequireQualifiedAccess>]
module Mvc = 

    let inline startDialog(view, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        if Mvc<'Events, ^Model>(model, view, controller).StartDialog() then Some model else None

    let inline startWindow(view, controller) = 
        async {
            let model = (^Model : (static member Create : unit -> ^Model) ())
            let! isOk = Mvc<'Events, ^Model>(model, view, controller).StartWindow()
            return if isOk then Some model else None
        }