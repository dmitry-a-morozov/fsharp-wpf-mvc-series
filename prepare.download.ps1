param
(
    [Parameter(Mandatory=$true)]
    $folder
)

if(Test-Path '.\temp') 
{ 
    Remove-Item '.\temp' -Recurse -Force
} 
$temp = New-Item -Path . -Name temp -ItemType directory 
Copy-Item $folder -Destination $temp -Recurse 
$source = Join-Path $temp $folder
$source | 
    Get-ChildItem -Recurse -Force | 
    where { $_.Name -in ('.svn', 'bin', 'obj') -or $_.Name -like '*.suo' } | 
    Remove-Item -Recurse -Force 
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = "$pwd\$folder.zip"
Remove-Item $archive -Force -ErrorAction SilentlyContinue
[IO.Compression.ZipFile]::CreateFromDirectory($source, $archive, 'Optimal', $true)
Remove-Item $temp -Recurse -Force
