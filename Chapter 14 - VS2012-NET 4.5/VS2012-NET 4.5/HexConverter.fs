namespace Mvc.Wpf.Sample

open System
open System.Globalization
open System.Windows.Data
open Mvc.Wpf
open Mvc.Wpf.UIElements
open System.Windows
open System.Windows.Controls
open FSharpx

module HexConverter =  

    type HexConverterWindow = XAML<"View\HexConverterWindow.xaml">

    [<AbstractClass>]
    type Model() = 
        inherit Mvc.Wpf.Model()

        abstract HexValue : string with get, set
        member this.Value 
            with get() = Int32.Parse(this.HexValue, NumberStyles.HexNumber)
            and set value = this.HexValue <- sprintf "%X" value

    let view() = 
        let window = HexConverterWindow()
        let ok = window.OK
        let value = window.Value
        value.ShowErrorInTooltip()
        let result = {
            new View<unit, Model, Window>(window.Root) with 
                member this.EventStreams = 
                    [
                        ok.Click |> Observable.mapTo()
                    ]

                member this.SetBindings model = 
                    Binding.FromExpression 
                        <@ 
                            value.Text <- model.HexValue
                        @>
        }
        result.CancelButton <- window.Cancel
        result

    let controller view = {
        new SupervisingController<unit, Model>(view) with
            member this.InitModel _ = ()
            member this.Dispatcher = fun() -> 
                Sync <| fun(model : Model) ->
                    try 
                        let _ = model.Value
                        view.OK()
                    with :? FormatException as why ->  
                        let errorMessage = sprintf "Cannot parse hex value %s because %s" model.HexValue why.Message
                        model |> Validation.setError <@ fun m -> m.HexValue @> errorMessage
    }

