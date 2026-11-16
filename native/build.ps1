$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
New-Item -ItemType Directory -Force -Path out | Out-Null

if (Get-Command gcc -ErrorAction SilentlyContinue) {
    gcc -std=c99 -O2 -Wall -Wextra -shared -o out\ticketcost.dll ticketcost.c
    Write-Host "built out\ticketcost.dll with gcc"
    gcc --version | Select-Object -First 1
}
elseif (Get-Command cl -ErrorAction SilentlyContinue) {
    cl /nologo /O2 /LD ticketcost.c /Fe:out\ticketcost.dll /Fo:out\ticketcost.obj
    Write-Host "built out\ticketcost.dll with cl"
    Remove-Item -ErrorAction SilentlyContinue out\ticketcost.obj, out\ticketcost.lib, out\ticketcost.exp
}
else {
    throw "No C compiler found. Install gcc (MSYS2) or use the x64 Native Tools Command Prompt for VS."
}
