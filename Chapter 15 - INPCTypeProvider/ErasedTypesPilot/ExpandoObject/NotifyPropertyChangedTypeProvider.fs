namespace ExpandoObject.INPCTypeProvider

open System
open System.Reflection
open System.IO
open System.Dynamic
open System.Collections.Generic

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations

open Samples.FSharp.ProvidedTypes

[<TypeProvider>]
type public NotifyPropertyChangedTypeProvider(config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let nameSpace = this.GetType().Namespace
    let assembly = Assembly.GetExecutingAssembly()
    let providerType = ProvidedTypeDefinition(assembly, nameSpace, "NotifyPropertyChanged", Some typeof<obj>)

    do 
        providerType.DefineStaticParameters(
            [ ProvidedStaticParameter("prototypesAssembly", typeof<string>) ], 
            this.GenerateTypes)
        this.AddNamespace(nameSpace, [ providerType ])

    member internal this.GenerateTypes typeName parameters = 
        let outerType = ProvidedTypeDefinition(assembly, nameSpace, typeName, Some typeof<obj>)

        let fileName = string parameters.[0]
        let assemblyName = config.ReferencedAssemblies |> Array.find(fun fullPath -> Path.GetFileNameWithoutExtension fullPath = fileName)
        let prototypesAssembly = Assembly.LoadFrom assemblyName

        query {
            for prototype in prototypesAssembly.GetTypes() do
            where(FSharpType.IsRecord prototype)
            select(this.MapRecordToModelClass prototype)
        } 
        |> Seq.iter outerType.AddMember

        outerType

    member internal this.MapRecordToModelClass (prototype : Type) = 

        let result = ProvidedTypeDefinition(prototype.Name, Some typeof<ExpandoObject>)

        let prototypeName = prototype.AssemblyQualifiedName
        ProvidedConstructor([], InvokeCode = fun _ -> <@@ ExpandoObject() @@>) |> result.AddMember 

        for field in FSharpType.GetRecordFields prototype do
            let propertyName = field.Name
            let property = ProvidedProperty(field.Name, field.PropertyType)
            property.GetterCode <- fun args -> 
                <@@ 
                    let dict : IDictionary<_, _> = upcast (%%args.[0] : ExpandoObject)
                    dict.[propertyName] 
                @@>
            if field.CanWrite 
            then 
                property.SetterCode <- fun args -> 
                    <@@ 
                        let dict : IDictionary<_, _> = upcast (%%args.[0] : ExpandoObject)
                        dict.[propertyName] <- %%(Expr.Coerce(args.[1], typeof<obj>))
                    @@>
            result.AddMember property

        result

[<assembly:TypeProviderAssembly>] 
do()