namespace FSharp.Windows.Sample

open System
open System.Globalization
open System.Windows.Data
open FSharp.Windows
open FSharp.Windows.UIElements
open System.Windows
open System.Windows.Controls
open FSharpx

module HexConverter =  

    type HexConverterWindow = XAML<"View\HexConverterWindow.xaml">

    type Events = ValueChanging of string * (unit -> unit)

    [<AbstractClass>]
    type Model() = 
        inherit FSharp.Windows.Model()

        abstract HexValue : string with get, set
        member this.Value 
            with get() = Int32.Parse(this.HexValue, NumberStyles.HexNumber)
            and set value = this.HexValue <- sprintf "%X" value

    let view() = 
        let window = HexConverterWindow()
        let value = window.Value
        value.ShowErrorInTooltip()
        let result = {
            new View<Events, Model, Window>(window.Root) with 
                member this.EventStreams = 
                    [
                        window.Value.PreviewTextInput |> Observable.map(fun args -> ValueChanging(args.Text, fun() -> args.Handled <- true))
                    ]

                member this.SetBindings model = 
                    Binding.FromExpression 
                        <@ 
                            window.Value.Text <- model.HexValue
                        @>
        }
        result.CancelButton <- window.Cancel
        result.DefaultOKButton <- window.OK
        result

    let controller() = 
        Controller.Create(
            fun(ValueChanging(text, cancel)) (model : Model) ->
                let isValid, _ = Int32.TryParse(text, NumberStyles.HexNumber, null)
                if not isValid then cancel()
            )
