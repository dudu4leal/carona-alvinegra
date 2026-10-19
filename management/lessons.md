# Lessons Learned

Registro de dificuldades reais encontradas durante a implementação e o padrão de solução reutilizável.

---

## L1: NuGet quebra no Windows quando variáveis de ambiente de diretórios estão ausentes

**Date:** 2026-10-19
**Category:** tooling

**Problem:** `dotnet restore`, `dotnet build` e `dotnet nuget list source` falhavam com `error : Value cannot be null. (Parameter 'path1')` no `_GetRestoreSettings` (NuGet.targets), em todos os projetos com `PackageReference`. A causa raiz: o Windows desta máquina não define `PROGRAMFILES(X86)` (nem `PROGRAMDATA`, `APPDATA`, `PROGRAMFILES`). No .NET Core, o NuGet resolve a pasta de configuração machine-wide lendo **`PROGRAMFILES(X86)`** via `Environment.GetEnvironmentVariable`; sem a variável, `Path.Combine(null, "NuGet")` lança `ArgumentNullException`.

**Solution:** Executar os comandos `dotnet` com as variáveis definidas e sem reutilização de nós do MSBuild (nós antigos mantêm o ambiente antigo):

```bash
env 'PROGRAMFILES(X86)=C:\Program Files (x86)' 'PROGRAMFILES=C:\Program Files' \
    'PROGRAMDATA=C:\ProgramData' 'APPDATA=C:\Users\<user>\AppData\Roaming' \
    'LOCALAPPDATA=C:\Users\<user>\AppData\Local' \
    MSBUILDDISABLENODEREUSE=1 \
    dotnet restore backend/CaronaAlvinegra.sln
```

Se o erro persistir após definir as variáveis, rodar `dotnet build-server shutdown` (nós do MSBuild reaproveitados preservam o ambiente de quando foram iniciados).

**Takeaway:** Antes de concluir que um projeto está quebrado, isolar a falha no toolchain: `dotnet nuget list source` falhando indica ambiente/NuGet, não código. Em máquinas Windows com ambiente enxuto, prefixar os comandos `dotnet` com as variáveis de diretórios padrão e `MSBUILDDISABLENODEREUSE=1`. O repositório fixa o SDK em `global.json` (9.0.319), então não é necessário trocar de SDK para diagnosticar.

---

## L2: Templates do SDK .NET 10 geram `.slnx` e não `.sln`

**Date:** 2026-10-19
**Category:** tooling

**Problem:** `dotnet new sln -n CaronaAlvinegra -o backend` gerou `backend/CaronaAlvinegra.slnx` (formato novo), enquanto o Spec 10 exige `backend/CaronaAlvinegra.sln` (formato clássico usado por todas as referências e pelo Spec 100).

**Solution:** Recriar com `dotnet new sln -n CaronaAlvinegra -o backend --format sln` e remover o `.slnx`. O `global.json` com o SDK 9.0.319 também evita o comportamento novo por padrão.

**Takeaway:** Sempre usar `--format sln` ao criar soluções neste projeto (ou garantir o `global.json` com SDK 9), para manter o caminho `backend/CaronaAlvinegra.sln` estável entre máquinas e containers.

---

(Add more lessons as they are discovered during development.)
