namespace CustomRuntimeClass.INPCTypeProvider

open System
open System.ComponentModel
open System.Reflection
open System.Collections.Generic

open Microsoft.FSharp.Reflection

type Model(prototype) as this = 

    do assert(FSharpType.IsRecord prototype)
    let prototypeInstance = 
        let defaultValues = Array.zeroCreate <| FSharpType.GetRecordFields(prototype).Length
        FSharpValue.MakeRecord(prototype, defaultValues)

    let propertyChangedEvent = Event<_, _>()

    let errors = Dictionary()
    let errorsChangedEvent = Event<_,_>()
    let getErrorsOrEmpty propertyName = match errors.TryGetValue propertyName with | true, errors -> errors | false, _ -> []
    let triggerErrorsChanged propertyName = errorsChangedEvent.Trigger(this, DataErrorsChangedEventArgs propertyName)

    let properties = dict <| seq { for p in FSharpType.GetRecordFields prototype -> p.Name, p } 

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

    interface INotifyDataErrorInfo with
        member this.HasErrors = this.HasErrors
        member this.GetErrors propertyName = upcast getErrorsOrEmpty propertyName
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChangedEvent.Publish

    member this.Item 
        with get propertyName = 
            properties.[propertyName].GetValue prototypeInstance
        and set propertyName value = 
            if value <> this.[propertyName]
            then 
                properties.[propertyName].SetValue(prototypeInstance, value)
                propertyChangedEvent.Trigger(this, PropertyChangedEventArgs propertyName)

    member this.AddErrors(propertyName, [<ParamArray>] messages) = 
        errors.[propertyName] <- getErrorsOrEmpty propertyName @ List.ofArray messages 
        triggerErrorsChanged propertyName
    member this.AddError(propertyName, message : string) = this.AddErrors(propertyName, message)
    member this.ClearErrors propertyName = 
        errors.Remove propertyName |> ignore
        triggerErrorsChanged propertyName
    member this.ClearAllErrors() = errors.Keys |> Seq.toArray |> Array.iter this.ClearErrors
    member this.HasErrors = errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)



