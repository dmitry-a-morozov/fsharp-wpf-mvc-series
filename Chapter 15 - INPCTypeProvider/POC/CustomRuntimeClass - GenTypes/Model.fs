namespace CustomRuntimeClass.INPCTypeProvider

open System
open System.ComponentModel
open System.Collections.Generic
open System.Windows
open System.Windows.Data

[<AbstractClass>]
type Model(derivedProperties : (DependencyProperty * string list)[]) as this = 
    inherit DependencyObject()
    
    let errors = Dictionary()
    let errorsChangedEvent = Event<_,_>()
    let triggerErrorsChanged propertyName = errorsChangedEvent.Trigger(this, DataErrorsChangedEventArgs propertyName)
    let getErrorsOrEmpty propertyName = match errors.TryGetValue propertyName with | true, errors -> errors | false, _ -> []

    do 
        for dp, dependecies in derivedProperties do
            let binding = MultiBinding()
            let self = Binding(RelativeSource = RelativeSource.Self) 
            binding.Bindings.Add self
            for path in dependecies do
                binding.Bindings.Add <| Binding(path, RelativeSource = RelativeSource.Self)
            let p' = this.GetType().GetProperty(dp.Name)

            binding.Converter <- {
                new IMultiValueConverter with

                    member this.Convert(values, _, _, _) = 
                        if values |> Array.exists (fun x -> x = DependencyProperty.UnsetValue)
                        then 
                            DependencyProperty.UnsetValue
                        else
                            try 
                                p'.GetValue(values.[0])
                            with _ ->
                                DependencyProperty.UnsetValue

                    member this.ConvertBack(_, _, _, _) = raise <| NotImplementedException()
            }
            let bindingExpression = BindingOperations.SetBinding(this, dp, binding)
            assert not bindingExpression.HasError

    interface INotifyDataErrorInfo with
        member this.HasErrors = 
            errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)
        member this.GetErrors propertyName = 
            if String.IsNullOrEmpty propertyName 
            then upcast errors.Values 
            else upcast getErrorsOrEmpty propertyName
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChangedEvent.Publish

    member this.SetErrors(propertyName, messages) = 
        errors.[propertyName] <- messages
        triggerErrorsChanged propertyName

    member this.AddError(propertyName, message) = 
        errors.[propertyName] <- message :: getErrorsOrEmpty propertyName 
        triggerErrorsChanged propertyName


