[<AutoOpen>]
module FSharp.Windows.Sample.Extensions 

open System.Windows.Data
open System.Windows.Controls

type TextBox with
    member this.ShowErrorInTooltip() = 
        let binding = Binding("(Validation.Errors).CurrentItem.ErrorContent", RelativeSource = RelativeSource.Self)
        this.SetBinding(TextBox.ToolTipProperty, binding) |> ignore
