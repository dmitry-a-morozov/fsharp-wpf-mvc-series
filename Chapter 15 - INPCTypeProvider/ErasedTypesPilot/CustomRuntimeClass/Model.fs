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

    let properties = 
        seq {
            for p in FSharpType.GetRecordFields prototype do
                let desc = { 
                    new PropertyDescriptor(p.Name, [||]) with
                        member __.ComponentType = this.GetType()
                        member __.IsReadOnly = not p.CanWrite
                        member __.PropertyType = p.PropertyType
                        member __.CanResetValue _ = true
                        member __.ResetValue _ = ()
                        member __.ShouldSerializeValue _ = false
                        member __.GetValue _ = p.GetValue prototypeInstance
                        member __.SetValue(_, value) = 
                            p.SetValue(prototypeInstance, value)
                            propertyChangedEvent.Trigger(this, PropertyChangedEventArgs p.Name)
                }
                yield p.Name, desc
        } |> dict

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

    interface ICustomTypeDescriptor with
        member this.GetAttributes() = TypeDescriptor.GetAttributes prototype
        member this.GetClassName() = TypeDescriptor.GetClassName prototype
        member this.GetComponentName() = TypeDescriptor.GetComponentName prototype
        member this.GetConverter() = TypeDescriptor.GetConverter prototype
        member this.GetDefaultEvent() = TypeDescriptor.GetDefaultEvent prototype
        member this.GetDefaultProperty() = TypeDescriptor.GetDefaultProperty prototype
        member this.GetEditor x = TypeDescriptor.GetEditor(prototype, x)
        member this.GetEvents() = TypeDescriptor.GetEvents prototype
        member this.GetEvents attrs = TypeDescriptor.GetEvents(prototype, attrs)
        member this.GetPropertyOwner _ = box this
        member this.GetProperties attrs = TypeDescriptor.GetProperties(this, attrs, true)
        member this.GetProperties() = PropertyDescriptorCollection(Seq.toArray properties.Values)

    interface INotifyDataErrorInfo with
        member this.HasErrors = this.HasErrors
        member this.GetErrors propertyName = upcast getErrorsOrEmpty propertyName
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChangedEvent.Publish

    member this.Item 
        with get propertyName = properties.[propertyName].GetValue this
        and set propertyName value = properties.[propertyName].SetValue(this, value)

    member this.AddErrors(propertyName, [<ParamArray>] messages) = 
        errors.[propertyName] <- getErrorsOrEmpty propertyName @ List.ofArray messages 
        triggerErrorsChanged propertyName
    member this.AddError(propertyName, message : string) = this.AddErrors(propertyName, message)
    member this.ClearErrors propertyName = 
        errors.Remove propertyName |> ignore
        triggerErrorsChanged propertyName
    member this.ClearAllErrors() = errors.Keys |> Seq.toArray |> Array.iter this.ClearErrors
    member this.HasErrors = errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)



