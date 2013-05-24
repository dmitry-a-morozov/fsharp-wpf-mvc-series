namespace CustomRuntimeClass.INPCTypeProvider

open System
open System.Reflection
open System.IO

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations

open Samples.FSharp.ProvidedTypes

[<TypeProvider>]
type public NotifyPropertyChangedTypeProvider(config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let nameSpace = this.GetType().Namespace
    let assembly = Assembly.GetExecutingAssembly()
    let tempAssembly = ProvidedAssembly(Path.ChangeExtension(Path.GetTempFileName(), ".dll"))
    let providerType = ProvidedTypeDefinition(assembly, nameSpace, "NotifyPropertyChanged", Some typeof<obj>, IsErased = false)

    do 
        tempAssembly.AddTypes <| [ providerType ]

        let parameters = [
            ProvidedStaticParameter("prototypesAssembly", typeof<string>)
        ]

        providerType.DefineStaticParameters(parameters, this.GenerateTypes)
        this.AddNamespace(nameSpace, [ providerType ])

    member internal this.GenerateTypes typeName parameters = 
        let outerType = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some typeof<obj>, IsErased = false)
        tempAssembly.AddTypes <| [ outerType ]

        let fileName = string parameters.[0]
        let assemblyFileName = config.ReferencedAssemblies |> Array.find (fun fullPath -> Path.GetFileNameWithoutExtension fullPath = fileName)
        
        outerType.AddMembersDelayed <| fun() ->
            
            let prototypesAssembly = Assembly.LoadFrom assemblyFileName

            query {
                for prototype in prototypesAssembly.GetTypes() do
                where(FSharpType.IsRecord prototype)
                select(this.MapRecordToModelClass prototype)
            } 
            |> Seq.toList

        outerType

    member internal this.MapRecordToModelClass (prototype : Type) = 

        let result = ProvidedTypeDefinition(prototype.Name, Some typeof<Model>, IsErased = false)
        tempAssembly.AddTypes <| [ result ]

        let prototypeName = prototype.AssemblyQualifiedName
        result.AddMember <| ProvidedConstructor([], IsImplicitCtor = true)

        for field in FSharpType.GetRecordFields prototype do
            if not field.CanWrite then failwith "Record field %s is not marked as mutable." field.Name
            let name = field.Name

            let backingField = ProvidedField("_" + field.Name, field.PropertyType)
            result.AddMember backingField

            let property = ProvidedProperty(field.Name, field.PropertyType)
            property.GetterCode <- fun args -> Expr.FieldGet(args.[0], backingField)
            property.SetterCode <- fun  [this; value] -> 
                <@@ 
                    //got lost here 
//                    let cast = typeof<Expr>.GetMethod("Cast").MakeGenericMethod(  backingField.FieldType )
//                    let oldValue = cast.Invoke(%%Expr.FieldGet(this, backingField), Array.empty)
//                    if not(value.Equals oldValue) 
//                    then
//                        (%%Expr.FieldSet(this, backingField, value) : unit)
//                        let inpc = %%this :> Model
//                        inpc.TriggerPropertyChanged name
                    ()
                @@>

            result.AddMember property

        result

[<assembly:TypeProviderAssembly>] 
do()