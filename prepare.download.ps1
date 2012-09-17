param
(
    [Parameter(Mandatory=$true)]
    $folder
)

if(Test-Path temp) 
{ 
    Remove-Item temp -Recurse -Force
} 
$temp = New-Item -Path . -Name temp -ItemType directory 
Copy-Item $folder -Destination $temp -Recurse 
$source = Join-Path $temp $folder
$source | Get-ChildItem -Recurse | where { ('.svn', 'bin', 'obj') -contains  $_.Name } | Remove-Item -Recurse -Force 
Import-Module Pscx
#$source | gm
Write-Zip $temp
"Done."