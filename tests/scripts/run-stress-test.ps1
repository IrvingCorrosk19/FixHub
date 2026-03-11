<#
.SYNOPSIS
  Simula carga ligera en FixHub: 10 clientes, 5 técnicos, 30 jobs con distribución definida.
.DESCRIPTION
  Requiere API en ejecución (http://localhost:5100). Ejecutar desde raíz del repo.
  Genera docs/QA/STRESS_TEST_REPORT.md con resumen y validaciones.
#>

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5100/api/v1"
$Prefix = "stress_" + [int][double]::Parse((Get-Date -UFormat %s)) + "_"

# Resultados para el reporte
$Script:Report = @{
    StartTime   = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    EndTime     = $null
    Errors      = [System.Collections.ArrayList]::new()
    Customers   = @()
    Technicians = @()
    Admin       = $null
    Jobs        = @()
    JobIdsByGroup = @{
        Completed           = @()   # 1-10
        CancelledBefore     = @()   # 11-15
        Reassigned          = @()   # 16-20
        NoProposalAccepted  = @()   # 21-25
        CancelledAfterAssign = @()  # 26-30
    }
}

function Invoke-Api {
    param(
        [string] $Method,
        [string] $Path,
        [hashtable] $Body = $null,
        [string] $Token = $null
    )
    $uri = "$BaseUrl/$Path"
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{ Uri = $uri; Method = $Method; Headers = $headers }
    if ($Body) { $params["Body"] = ($Body | ConvertTo-Json -Compress) }
    try {
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response }
    } catch {
        $Script:Report.Errors.Add("$Method $Path : $($_.Exception.Message)") | Out-Null
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# --- 1. Admin ---
Write-Host "Registering admin..."
$adminEmail = "${Prefix}admin@test.fixhub"
$r = Invoke-Api -Method POST -Path "auth/register" -Body @{
    fullName = "Admin Stress"
    email    = $adminEmail
    password = "Test123!"
    role     = 3
    phone    = $null
}
if (-not $r.Success) { Write-Host "Admin register failed"; exit 1 }
$adminToken = $r.Data.token
$adminId    = $r.Data.userId
$Script:Report.Admin = @{ Id = $adminId; Token = $adminToken; Email = $adminEmail }
Start-Sleep -Seconds 1

# --- 2. 10 Clientes (pausa entre auth para no superar rate limit 10/min) ---
Write-Host "Registering 10 customers..."
for ($i = 1; $i -le 10; $i++) {
    Start-Sleep -Seconds 7
    $email = "${Prefix}customer${i}@test.fixhub"
    $r = Invoke-Api -Method POST -Path "auth/register" -Body @{
        fullName = "Cliente Stress $i"
        email    = $email
        password = "Test123!"
        role     = 1
        phone    = "+34${i}0000000$i"
    }
    if (-not $r.Success) { Write-Host "Customer $i register failed"; continue }
    Start-Sleep -Seconds 7
    $login = Invoke-Api -Method POST -Path "auth/login" -Body @{ email = $email; password = "Test123!" }
    if (-not $login.Success) { continue }
    $Script:Report.Customers += @{ Id = $r.Data.userId; Token = $login.Data.token; Email = $email; Index = $i }
}

# --- 3. 5 Técnicos (sin aprobar aún para evitar auto-asign al crear jobs) ---
Write-Host "Registering 5 technicians..."
for ($i = 1; $i -le 5; $i++) {
    Start-Sleep -Seconds 7
    $email = "${Prefix}tech${i}@test.fixhub"
    $r = Invoke-Api -Method POST -Path "auth/register" -Body @{
        fullName = "Técnico Stress $i"
        email    = $email
        password = "Test123!"
        role     = 2
        phone    = "+34${i}0000010$i"
    }
    if (-not $r.Success) { Write-Host "Technician $i register failed"; continue }
    Start-Sleep -Seconds 7
    $login = Invoke-Api -Method POST -Path "auth/login" -Body @{ email = $email; password = "Test123!" }
    if (-not $login.Success) { continue }
    $Script:Report.Technicians += @{ Id = $r.Data.userId; Token = $login.Data.token; Email = $email; Index = $i }
}

# --- 4. Crear 30 jobs ANTES de aprobar técnicos (para que todos queden Open, sin auto-asignación) ---
Write-Host "Creating 30 jobs..."
$jobIds = @()
$customerCount = [Math]::Max(1, $Script:Report.Customers.Count)
$technicianCount = [Math]::Max(1, $Script:Report.Technicians.Count)
for ($j = 0; $j -lt 30; $j++) {
    $cust = $Script:Report.Customers[$j % $customerCount]
    if (-not $cust -or -not $cust.Token) { continue }
    $r = Invoke-Api -Method POST -Path "jobs" -Token $cust.Token -Body @{
        categoryId  = 1
        title       = "Stress Job $($j+1)"
        description = "Descripción job $($j+1)"
        addressText = "Calle Stress $($j+1)"
        lat         = $null
        lng         = $null
        budgetMin   = 50
        budgetMax   = 150
    }
    if (-not $r.Success) { Write-Host "Job $($j+1) create failed"; continue }
    $jobIds += $r.Data.id
    Start-Sleep -Milliseconds 1200
}
$Script:Report.Jobs = $jobIds
if ($jobIds.Count -lt 30) { Write-Host "Only $($jobIds.Count) jobs created"; }

# Aprobar técnicos (después de crear jobs para que no haya auto-asignación)
Write-Host "Approving technicians..."
foreach ($t in $Script:Report.Technicians) {
    Invoke-Api -Method PATCH -Path "admin/technicians/$($t.Id)/status" -Body @{ status = 2 } -Token $adminToken | Out-Null
}

# Índices 0-based: 0-9 completed, 10-14 cancelled before, 15-19 reassigned, 20-24 no accept, 25-29 cancelled after
$j1_10  = 0..9
$j11_15 = 10..14
$j16_20 = 15..19
$j21_25 = 20..24
$j26_30 = 25..29

# --- 5. Jobs 11-15: cancelar sin propuesta ---
Write-Host "Cancelling jobs 11-15 (before assignment)..."
foreach ($idx in $j11_15) {
    if ($idx -ge $jobIds.Count) { break }
    $jobId = $jobIds[$idx]
    $cust  = $Script:Report.Customers[$idx % $customerCount]
    $r = Invoke-Api -Method POST -Path "jobs/$jobId/cancel" -Token $cust.Token
    if ($r.Success) { $Script:Report.JobIdsByGroup.CancelledBefore += $jobId }
}

# --- 6. Jobs 21-25: propuestas sin aceptar ---
Write-Host "Adding proposals for jobs 21-25 (no accept)..."
foreach ($idx in $j21_25) {
    if ($idx -ge $jobIds.Count) { break }
    $jobId = $jobIds[$idx]
    $tech  = $Script:Report.Technicians[$idx % $technicianCount]
    $pr = Invoke-Api -Method POST -Path "jobs/$jobId/proposals" -Token $tech.Token -Body @{ price = 80; message = "Propuesta" }
    if ($pr.Success) { $Script:Report.JobIdsByGroup.NoProposalAccepted += $jobId }
    Start-Sleep -Milliseconds 1200
}

# --- 7. Jobs 1-10: completar flujo (propuesta -> aceptar -> start -> complete) ---
Write-Host "Completing jobs 1-10..."
foreach ($idx in $j1_10) {
    if ($idx -ge $jobIds.Count) { break }
    $jobId  = $jobIds[$idx]
    $cust   = $Script:Report.Customers[$idx % $customerCount]
    $tech   = $Script:Report.Technicians[$idx % $technicianCount]
    $propR  = Invoke-Api -Method POST -Path "jobs/$jobId/proposals" -Token $tech.Token -Body @{ price = 100; message = "OK" }
    if (-not $propR.Success) { continue }
    Start-Sleep -Milliseconds 800
    $propId = $propR.Data.id
    $accR   = Invoke-Api -Method POST -Path "proposals/$propId/accept" -Token $adminToken
    if (-not $accR.Success) { continue }
    $startR = Invoke-Api -Method POST -Path "jobs/$jobId/start" -Token $tech.Token
    if (-not $startR.Success) { continue }
    $compR  = Invoke-Api -Method POST -Path "jobs/$jobId/complete" -Token $cust.Token
    if ($compR.Success) { $Script:Report.JobIdsByGroup.Completed += $jobId }
}

# --- 8. Jobs 16-20: reasignar (tech1 propone, accept, reassign to tech2) ---
Write-Host "Reassigning jobs 16-20..."
$tech1 = $Script:Report.Technicians[0]; $tech2 = $Script:Report.Technicians[1]
foreach ($idx in $j16_20) {
    if ($idx -ge $jobIds.Count) { break }
    $jobId  = $jobIds[$idx]
    $propR  = Invoke-Api -Method POST -Path "jobs/$jobId/proposals" -Token $tech1.Token -Body @{ price = 90; message = "Tech1" }
    if (-not $propR.Success) { continue }
    $accR   = Invoke-Api -Method POST -Path "proposals/$($propR.Data.id)/accept" -Token $adminToken
    if (-not $accR.Success) { continue }
    $reassignR = Invoke-Api -Method POST -Path "admin/jobs/$jobId/reassign" -Token $adminToken -Body @{
        toTechnicianId = $tech2.Id
        reason         = "Stress test reassign"
        reasonDetail   = "E2E"
    }
    if ($reassignR.Success) { $Script:Report.JobIdsByGroup.Reassigned += $jobId }
}

# --- 9. Jobs 26-30: cancelar después de asignación ---
Write-Host "Cancelling jobs 26-30 (after assignment)..."
foreach ($idx in $j26_30) {
    if ($idx -ge $jobIds.Count) { break }
    $jobId  = $jobIds[$idx]
    $cust   = $Script:Report.Customers[$idx % $customerCount]
    $tech   = $Script:Report.Technicians[$idx % $technicianCount]
    $propR  = Invoke-Api -Method POST -Path "jobs/$jobId/proposals" -Token $tech.Token -Body @{ price = 70; message = "Cancel after" }
    if (-not $propR.Success) { continue }
    $accR   = Invoke-Api -Method POST -Path "proposals/$($propR.Data.id)/accept" -Token $adminToken
    if (-not $accR.Success) { continue }
    $cancelR = Invoke-Api -Method POST -Path "jobs/$jobId/cancel" -Token $cust.Token
    if ($cancelR.Success) { $Script:Report.JobIdsByGroup.CancelledAfterAssign += $jobId }
}

$Script:Report.EndTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# --- Generar reporte ---
$reportPath = Join-Path $PSScriptRoot "..\..\docs\QA\STRESS_TEST_REPORT.md"
$reportDir = Split-Path $reportPath
if (-not (Test-Path $reportDir)) { New-Item -ItemType Directory -Path $reportDir -Force | Out-Null }

$summary = @"
## Resumen de ejecución

- **Inicio:** $($Script:Report.StartTime)
- **Fin:**   $($Script:Report.EndTime)
- **Errores durante la ejecución:** $($Script:Report.Errors.Count)
- **Jobs creados:** $($Script:Report.Jobs.Count)
- **Completados (1-10):** $($Script:Report.JobIdsByGroup.Completed.Count)
- **Cancelados sin asignación (11-15):** $($Script:Report.JobIdsByGroup.CancelledBefore.Count)
- **Reasignados (16-20):** $($Script:Report.JobIdsByGroup.Reassigned.Count)
- **Sin aceptar propuesta (21-25):** $($Script:Report.JobIdsByGroup.NoProposalAccepted.Count)
- **Cancelados después de asignación (26-30):** $($Script:Report.JobIdsByGroup.CancelledAfterAssign.Count)
"@

if ($Script:Report.Errors.Count -gt 0) {
    $summary += "`n`n### Errores`n`n"
    foreach ($e in $Script:Report.Errors) { $summary += "- $e`n" }
}

$fullReport = @"
# FixHub — Reporte de prueba de carga (stress test ligero)

**Objetivo:** Simular carga ligera: 10 clientes, 5 técnicos, 30 jobs con distribución definida.

**Script:** `tests/scripts/run-stress-test.ps1` (ejecutar con API en marcha en http://localhost:5100).

$summary

## Diseño del test

| Recurso    | Cantidad |
|------------|----------|
| Clientes   | 10       |
| Técnicos   | 5        |
| Admin      | 1        |
| Jobs       | 30       |

### Distribución de acciones (30 jobs)

| Grupo              | Cantidad | Acciones |
|--------------------|----------|----------|
| Completados        | 10       | Crear → Propuesta → Aceptar → Start → Complete |
| Cancelados (antes) | 5        | Crear → Cancelar (sin propuesta) |
| Reasignados        | 5        | Crear → Propuesta (Tech1) → Aceptar → Reasignar a Tech2 |
| Sin aceptar        | 5        | Crear → Propuesta(s) → no Aceptar |
| Cancelados (después)| 5       | Crear → Propuesta → Aceptar → Cliente cancela |

## Validaciones

Comprobar en BD o API tras ejecutar el script.

### 1. No existan duplicaciones

- [ ] **Jobs:** 30 filas únicas por `Id`; ningún `Id` repetido.
- [ ] **JobAssignments:** A lo sumo un assignment activo por job (por job que tuvo propuesta aceptada); ningún job con dos assignments vigentes.
- [ ] **Proposals:** No hay dos propuestas con el mismo `(JobId, TechnicianId)` (constraint único en BD).

### 2. Estados correctos

- [ ] **10 completados:** `Job.Status = 4` (Completed), `CompletedAt` no nulo, un `JobAssignment` con `CompletedAt` no nulo.
- [ ] **5 cancelados antes:** `Job.Status = 5` (Cancelled), `CancelledAt` no nulo, sin `JobAssignment` para ese job.
- [ ] **5 reasignados:** `Job.Status = 2` (Assigned); exactamente un `AssignmentOverride` por cada uno; un solo `JobAssignment` activo (técnico destino).
- [ ] **5 sin aceptar:** `Job.Status = 1` (Open); al menos una propuesta en estado Pending; sin `JobAssignment`.
- [ ] **5 cancelados después:** `Job.Status = 5` (Cancelled), `CancelledAt` no nulo; existe `JobAssignment` (el que se creó al aceptar).

### 3. AuditLog registre todas las acciones

Comprobar que existan entradas de `AuditLog` coherentes con el flujo:

- [ ] **JOB_CREATE:** 30 (una por job creado).
- [ ] **PROPOSAL_SUBMIT:** al menos 25 (10+5+5+5 para completados, reasignados, sin aceptar, cancelados después).
- [ ] **PROPOSAL_ACCEPT:** 20 (10 completados + 5 reasignados + 5 cancelados después).
- [ ] **JOB_START:** 10 (solo los completados).
- [ ] **JOB_COMPLETE:** 10.
- [ ] **JOB_CANCEL:** 10 (5 antes + 5 después).
- [ ] **Job.Reassign:** 5.

### Consultas SQL de ejemplo (PostgreSQL)

Tabla de auditoría: `audit_logs` (columnas: action, entity_type, entity_id, created_at_utc).

```sql
-- Conteo por estado de Job (tabla jobs, columna status)
SELECT status, COUNT(*) FROM jobs GROUP BY status;

-- Jobs con más de un JobAssignment (debe ser 0)
SELECT job_id, COUNT(*) FROM job_assignments GROUP BY job_id HAVING COUNT(*) > 1;

-- Conteo por acción en AuditLog
SELECT action, COUNT(*) FROM audit_logs GROUP BY action ORDER BY action;
```

## Resultado de validación

_(Rellenar tras ejecutar las comprobaciones)_

| Validación           | Resultado | Notas |
|----------------------|-----------|-------|
| Sin duplicaciones    |           |       |
| Estados correctos    |           |       |
| AuditLog completo   |           |       |
"@

Set-Content -Path $reportPath -Value $fullReport -Encoding UTF8
Write-Host "Report written to $reportPath"
Write-Host $summary
