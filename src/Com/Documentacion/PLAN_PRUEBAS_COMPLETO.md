# 📋 Plan de Pruebas Completo - CarnetQR Platform

## 🎯 Objetivo General
Validar el correcto funcionamiento de todas las funcionalidades del sistema según los roles de usuario, incluyendo CRUD operations, multi-tenancy, autorización y flujos de negocio.

---

## 👥 Roles del Sistema

1. **SuperAdmin** - Acceso completo a todas las instituciones
2. **InstitutionAdmin** - Administrador de una institución específica
3. **Staff** - Personal operativo de una institución
4. **AdministrativeOperator** - Operador administrativo

---

## 🔐 FASE 1: AUTENTICACIÓN Y AUTORIZACIÓN

### PRUEBA 1.1: Login con SuperAdmin
**Objetivo:** Verificar que el SuperAdmin puede iniciar sesión correctamente.

**Pasos:**
1. Acceder a `http://164.68.99.83/Account/Login`
2. Ingresar credenciales:
   - Email: `admin@qlservices.com`
   - Password: `Admin@123456`
3. Click en "Iniciar Sesión"

**Resultado Esperado:**
- ✅ Login exitoso
- ✅ Redirección al Dashboard/Home
- ✅ Menú muestra opciones de SuperAdmin
- ✅ Se puede ver información de todas las instituciones

---

### PRUEBA 1.2: Login con InstitutionAdmin
**Objetivo:** Verificar que el InstitutionAdmin puede iniciar sesión y ver solo su institución.

**Pasos:**
1. Acceder a `http://164.68.99.83/Account/Login`
2. Ingresar credenciales:
   - Email: `admin@demo.com`
   - Password: `Admin@123456`
3. Click en "Iniciar Sesión"

**Resultado Esperado:**
- ✅ Login exitoso
- ✅ Redirección al Dashboard/Home
- ✅ Menú muestra opciones de InstitutionAdmin
- ✅ Solo se ve información de su institución asignada

---

### PRUEBA 1.3: Logout
**Objetivo:** Verificar que el logout funciona correctamente.

**Pasos:**
1. Estar autenticado como cualquier usuario
2. Click en "Cerrar Sesión" o acceder a `/Account/Logout`

**Resultado Esperado:**
- ✅ Sesión cerrada correctamente
- ✅ Redirección a página de login
- ✅ No se puede acceder a páginas protegidas sin re-autenticarse

---

### PRUEBA 1.4: Acceso No Autorizado
**Objetivo:** Verificar que usuarios sin permisos no pueden acceder a recursos restringidos.

**Pasos:**
1. Iniciar sesión como Staff (rol con menos permisos)
2. Intentar acceder directamente a:
   - `/Users` (requiere InstitutionAdminOrAbove)
   - `/Institutions` (requiere SuperAdminOnly)
   - `/InstitutionTypes` (requiere SuperAdminOnly)

**Resultado Esperado:**
- ✅ Redirección a `/Account/AccessDenied` o página de error
- ✅ Mensaje indicando falta de permisos
- ✅ No se muestra información restringida

---

## 👤 FASE 2: GESTIÓN DE USUARIOS (Users)

**Nota:** Solo accesible para `InstitutionAdminOrAbove` (InstitutionAdmin y SuperAdmin)

### PRUEBA 2.1: Listar Usuarios (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede ver todos los usuarios de todas las instituciones.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/Users`
3. Revisar la lista de usuarios

**Resultado Esperado:**
- ✅ Se muestra lista de usuarios
- ✅ Se ven usuarios de todas las instituciones
- ✅ Se muestra el nombre de la institución de cada usuario
- ✅ Botones de acción disponibles: Edit, Delete, ToggleActive

---

### PRUEBA 2.2: Listar Usuarios (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin solo ve usuarios de su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/Users`
3. Revisar la lista de usuarios

**Resultado Esperado:**
- ✅ Se muestra lista de usuarios
- ✅ Solo se ven usuarios de su institución
- ✅ No se ven usuarios de otras instituciones
- ✅ Botones de acción disponibles

---

### PRUEBA 2.3: Crear Usuario (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede crear usuarios y seleccionar la institución.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/Users/Create`
3. Completar el formulario:
   - Email: `test@example.com`
   - Nombre: `Test User`
   - Contraseña: `Test@123456`
   - Rol: Seleccionar cualquier rol
   - **Institución: Seleccionar de dropdown** (debe aparecer)
4. Click en "Crear"

**Resultado Esperado:**
- ✅ Dropdown de instituciones visible y funcional
- ✅ Usuario creado exitosamente
- ✅ Mensaje de confirmación
- ✅ Redirección a lista de usuarios
- ✅ Usuario aparece en la lista con la institución seleccionada

---

### PRUEBA 2.4: Crear Usuario (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin crea usuarios automáticamente en su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/Users/Create`
3. Completar el formulario:
   - Email: `staff@demo.com`
   - Nombre: `Staff User`
   - Contraseña: `Staff@123456`
   - Rol: Seleccionar Staff
   - **Institución: NO debe aparecer dropdown** (asignación automática)
4. Click en "Crear"

**Resultado Esperado:**
- ✅ Dropdown de instituciones NO visible
- ✅ Usuario creado exitosamente
- ✅ Usuario asignado automáticamente a la institución del InstitutionAdmin
- ✅ Mensaje de confirmación
- ✅ Usuario aparece en la lista

---

### PRUEBA 2.5: Editar Usuario
**Objetivo:** Verificar que se puede editar información de usuarios.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/Users`
3. Click en "Edit" de un usuario
4. Modificar campos (nombre, email, rol)
5. Click en "Guardar"

**Resultado Esperado:**
- ✅ Formulario de edición carga con datos actuales
- ✅ SuperAdmin puede cambiar institución (si aplica)
- ✅ InstitutionAdmin NO puede cambiar institución
- ✅ Cambios guardados exitosamente
- ✅ Mensaje de confirmación
- ✅ Cambios reflejados en la lista

---

### PRUEBA 2.6: Eliminar Usuario
**Objetivo:** Verificar que se puede eliminar usuarios con confirmación.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/Users`
3. Click en "Delete" de un usuario
4. Confirmar eliminación en el diálogo

**Resultado Esperado:**
- ✅ Diálogo de confirmación aparece (SweetAlert)
- ✅ Usuario eliminado después de confirmar
- ✅ Mensaje de éxito
- ✅ Usuario desaparece de la lista
- ✅ Registro de auditoría creado

---

### PRUEBA 2.7: ToggleActive Usuario
**Objetivo:** Verificar que se puede activar/desactivar usuarios.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/Users`
3. Click en "ToggleActive" de un usuario activo
4. Verificar cambio de estado
5. Click nuevamente para reactivar

**Resultado Esperado:**
- ✅ Estado cambia de Activo a Inactivo (o viceversa)
- ✅ Mensaje de confirmación
- ✅ Usuario inactivo no puede iniciar sesión
- ✅ Registro de auditoría creado

---

## 🏥 FASE 3: GESTIÓN DE ENTIDADES (EntityProfiles)

### PRUEBA 3.1: Listar Entidades (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede ver todas las entidades de todas las instituciones.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/EntityProfiles`
3. Revisar la lista

**Resultado Esperado:**
- ✅ Se muestra lista de entidades
- ✅ Se ven entidades de todas las instituciones
- ✅ Se muestra nombre de institución
- ✅ Botones: View, Edit, Delete, ToggleActive

---

### PRUEBA 3.2: Listar Entidades (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin solo ve entidades de su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/EntityProfiles`
3. Revisar la lista

**Resultado Esperado:**
- ✅ Se muestra lista de entidades
- ✅ Solo se ven entidades de su institución
- ✅ No se ven entidades de otras instituciones

---

### PRUEBA 3.3: Crear Entidad (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede crear entidades y seleccionar institución.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/EntityProfiles/Create`
3. Completar formulario:
   - **Institución: Seleccionar de dropdown** (debe aparecer)
   - Número de identificación
   - Nombre, Apellido
   - Email, Teléfono
   - Fecha de nacimiento
   - Foto (opcional, si está habilitada)
4. Click en "Crear"

**Resultado Esperado:**
- ✅ Dropdown de instituciones visible
- ✅ Entidad creada exitosamente
- ✅ Foto subida correctamente (si se proporciona)
- ✅ Mensaje de confirmación
- ✅ Redirección a lista o detalles
- ✅ Entidad aparece con la institución seleccionada

---

### PRUEBA 3.4: Crear Entidad (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin crea entidades automáticamente en su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/EntityProfiles/Create`
3. Completar formulario:
   - **Institución: NO debe aparecer dropdown**
   - Datos de la entidad
4. Click en "Crear"

**Resultado Esperado:**
- ✅ Dropdown de instituciones NO visible
- ✅ Entidad creada exitosamente
- ✅ Entidad asignada automáticamente a la institución del InstitutionAdmin
- ✅ Mensaje de confirmación

---

### PRUEBA 3.5: Editar Entidad (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede editar entidades y cambiar institución.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/EntityProfiles`
3. Click en "Edit" de una entidad
4. Modificar campos
5. **Cambiar institución en dropdown** (si aplica)
6. Click en "Guardar"

**Resultado Esperado:**
- ✅ Formulario carga con datos actuales
- ✅ Dropdown de instituciones visible y funcional
- ✅ Cambios guardados exitosamente
- ✅ Institución actualizada si se cambió
- ✅ Mensaje de confirmación

---

### PRUEBA 3.6: Editar Entidad (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin puede editar pero NO cambiar institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/EntityProfiles`
3. Click en "Edit" de una entidad de su institución
4. Modificar campos
5. Verificar que NO hay dropdown de instituciones
6. Click en "Guardar"

**Resultado Esperado:**
- ✅ Formulario carga con datos actuales
- ✅ Dropdown de instituciones NO visible
- ✅ Cambios guardados exitosamente
- ✅ Institución NO cambia (permanece la misma)
- ✅ Mensaje de confirmación

---

### PRUEBA 3.7: Ver Detalles de Entidad
**Objetivo:** Verificar que se pueden ver todos los detalles de una entidad.

**Pasos:**
1. Iniciar sesión como cualquier usuario autorizado
2. Navegar a `/EntityProfiles`
3. Click en "View" o "Details" de una entidad

**Resultado Esperado:**
- ✅ Se muestra información completa de la entidad
- ✅ Se muestra nombre de la institución
- ✅ Se muestra foto (si existe)
- ✅ Se muestran tarjetas asociadas
- ✅ Se muestran eventos asociados

---

### PRUEBA 3.8: Eliminar Entidad
**Objetivo:** Verificar que se puede eliminar entidades con confirmación.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/EntityProfiles`
3. Click en "Delete" de una entidad
4. Confirmar eliminación

**Resultado Esperado:**
- ✅ Diálogo de confirmación aparece
- ✅ Entidad eliminada después de confirmar
- ✅ Mensaje de éxito
- ✅ Entidad desaparece de la lista
- ✅ Registro de auditoría creado

---

### PRUEBA 3.9: ToggleActive Entidad
**Objetivo:** Verificar que se puede activar/desactivar entidades.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/EntityProfiles`
3. Click en "ToggleActive" de una entidad activa
4. Verificar cambio de estado

**Resultado Esperado:**
- ✅ Estado cambia de Activo a Inactivo (o viceversa)
- ✅ Mensaje de confirmación
- ✅ Entidad inactiva no aparece en ciertos listados
- ✅ Registro de auditoría creado

---

## 🎴 FASE 4: GESTIÓN DE TARJETAS (Cards)

**Nota:** Las tarjetas son inmutables, NO hay funcionalidad de Edit.

### PRUEBA 4.1: Listar Tarjetas (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede ver todas las tarjetas.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/Cards`
3. Revisar la lista

**Resultado Esperado:**
- ✅ Se muestra lista de tarjetas
- ✅ Se ven tarjetas de todas las instituciones
- ✅ Se muestra información de la entidad asociada
- ✅ Botones: View, ToggleActive, Delete

---

### PRUEBA 4.2: Listar Tarjetas (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin solo ve tarjetas de su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/Cards`
3. Revisar la lista

**Resultado Esperado:**
- ✅ Se muestra lista de tarjetas
- ✅ Solo se ven tarjetas de su institución
- ✅ No se ven tarjetas de otras instituciones

---

### PRUEBA 4.3: Crear Tarjeta (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede crear tarjetas para cualquier entidad.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/Cards/Create`
3. Verificar que se muestran todas las entidades (de todas las instituciones)
4. **Filtro opcional por institución** (si está implementado)
5. Seleccionar una entidad
6. Click en "Crear"

**Resultado Esperado:**
- ✅ Se listan todas las entidades activas sin tarjeta activa
- ✅ Filtro por institución funciona (si está implementado)
- ✅ Tarjeta creada exitosamente
- ✅ QR code generado
- ✅ Mensaje de confirmación
- ✅ Redirección a lista o detalles

---

### PRUEBA 4.4: Crear Tarjeta (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin solo puede crear tarjetas para entidades de su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/Cards/Create`
3. Verificar lista de entidades disponibles

**Resultado Esperado:**
- ✅ Solo se listan entidades activas de su institución
- ✅ No se ven entidades de otras instituciones
- ✅ Tarjeta creada exitosamente
- ✅ QR code generado

---

### PRUEBA 4.5: Ver Detalles de Tarjeta
**Objetivo:** Verificar que se pueden ver detalles completos de una tarjeta.

**Pasos:**
1. Iniciar sesión como cualquier usuario autorizado
2. Navegar a `/Cards`
3. Click en "View" o "Details" de una tarjeta

**Resultado Esperado:**
- ✅ Se muestra información completa de la tarjeta
- ✅ Se muestra QR code
- ✅ Se muestra información de la entidad asociada
- ✅ Botón para imprimir/vista previa disponible

---

### PRUEBA 4.6: Vista de Impresión de Tarjeta
**Objetivo:** Verificar que la vista de impresión muestra correctamente la tarjeta.

**Pasos:**
1. Iniciar sesión como cualquier usuario autorizado
2. Navegar a `/Cards/Details/{id}` o `/Carnet/Print/{id}`
3. Revisar la vista de impresión

**Resultado Esperado:**
- ✅ Vista frontal muestra:
  - Logo/nombre de institución
  - Número de tarjeta
  - Nombre de entidad
  - **Foto de entidad (o placeholder si no hay foto)**
- ✅ Vista trasera muestra:
  - **QR code (o placeholder si no hay QR)**
  - Información de contacto
- ✅ Dimensiones correctas para impresión (CR80: 85.6mm x 54mm)
- ✅ Estilos de impresión correctos (@media print)

---

### PRUEBA 4.7: Eliminar Tarjeta
**Objetivo:** Verificar que se puede eliminar tarjetas con confirmación.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/Cards`
3. Click en "Delete" de una tarjeta
4. Confirmar eliminación

**Resultado Esperado:**
- ✅ Diálogo de confirmación aparece
- ✅ Tarjeta eliminada después de confirmar
- ✅ Mensaje de éxito
- ✅ Tarjeta desaparece de la lista
- ✅ Registro de auditoría creado

---

### PRUEBA 4.8: ToggleActive Tarjeta
**Objetivo:** Verificar que se puede activar/desactivar tarjetas.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/Cards`
3. Click en "ToggleActive" de una tarjeta activa
4. Verificar cambio de estado

**Resultado Esperado:**
- ✅ Estado cambia de Activo a Inactivo (o viceversa)
- ✅ Mensaje de confirmación
- ✅ Tarjeta inactiva no se puede usar
- ✅ Registro de auditoría creado

---

### PRUEBA 4.9: Validación - Una Tarjeta Activa por Entidad
**Objetivo:** Verificar que una entidad no puede tener múltiples tarjetas activas.

**Pasos:**
1. Iniciar sesión como cualquier usuario autorizado
2. Navegar a `/Cards/Create`
3. Intentar crear una tarjeta para una entidad que ya tiene una tarjeta activa

**Resultado Esperado:**
- ✅ Entidad con tarjeta activa NO aparece en la lista de entidades disponibles
- ✅ O muestra mensaje de error si se intenta crear
- ✅ Solo se pueden crear tarjetas para entidades sin tarjeta activa

---

## 📅 FASE 5: GESTIÓN DE EVENTOS (Events)

### PRUEBA 5.1: Listar Eventos (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede ver todos los eventos.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/Events`
3. Revisar la lista

**Resultado Esperado:**
- ✅ Se muestra lista de eventos
- ✅ Se ven eventos de todas las instituciones
- ✅ Se muestra información de la entidad asociada
- ✅ Botones: View, Edit, Delete, ToggleActive

---

### PRUEBA 5.2: Listar Eventos (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin solo ve eventos de su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/Events`
3. Revisar la lista

**Resultado Esperado:**
- ✅ Se muestra lista de eventos
- ✅ Solo se ven eventos de su institución
- ✅ No se ven eventos de otras instituciones

---

### PRUEBA 5.3: Crear Evento (SuperAdmin)
**Objetivo:** Verificar que SuperAdmin puede crear eventos para cualquier entidad.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/Events/Create`
3. Completar formulario:
   - **Filtro por institución** (si está implementado)
   - Seleccionar entidad (de cualquier institución)
   - Tipo de evento
   - Fecha y hora
   - Descripción
4. Click en "Crear"

**Resultado Esperado:**
- ✅ Filtro por institución funciona (si está implementado)
- ✅ Se listan entidades de todas las instituciones
- ✅ Evento creado exitosamente
- ✅ InstitutionId se asigna automáticamente desde la entidad seleccionada
- ✅ Mensaje de confirmación
- ✅ Evento aparece en la lista

---

### PRUEBA 5.4: Crear Evento (InstitutionAdmin)
**Objetivo:** Verificar que InstitutionAdmin solo puede crear eventos para entidades de su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/Events/Create`
3. Verificar lista de entidades disponibles

**Resultado Esperado:**
- ✅ Solo se listan entidades de su institución
- ✅ No se ven entidades de otras instituciones
- ✅ Evento creado exitosamente
- ✅ InstitutionId asignado automáticamente

---

### PRUEBA 5.5: Editar Evento
**Objetivo:** Verificar que se pueden editar eventos (solo eventos programados).

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/Events`
3. Click en "Edit" de un evento programado
4. Modificar campos
5. Click en "Guardar"

**Resultado Esperado:**
- ✅ Formulario carga con datos actuales
- ✅ Cambios guardados exitosamente
- ✅ Mensaje de confirmación
- ✅ Cambios reflejados en la lista

---

### PRUEBA 5.6: Eliminar Evento
**Objetivo:** Verificar que se puede eliminar eventos con confirmación.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/Events`
3. Click en "Delete" de un evento
4. Confirmar eliminación

**Resultado Esperado:**
- ✅ Diálogo de confirmación aparece
- ✅ Evento eliminado después de confirmar
- ✅ Mensaje de éxito
- ✅ Evento desaparece de la lista
- ✅ Registro de auditoría creado

---

### PRUEBA 5.7: ToggleActive Evento
**Objetivo:** Verificar que se puede cambiar el estado de eventos.

**Pasos:**
1. Iniciar sesión como SuperAdmin o InstitutionAdmin
2. Navegar a `/Events`
3. Click en "ToggleActive" de un evento
4. Verificar cambio de estado

**Resultado Esperado:**
- ✅ Estado cambia (Scheduled ↔ NotCompleted)
- ✅ Mensaje de confirmación
- ✅ Cambio reflejado en la lista
- ✅ Registro de auditoría creado

---

## 🏢 FASE 6: GESTIÓN DE INSTITUCIONES (Solo SuperAdmin)

### PRUEBA 6.1: Listar Instituciones
**Objetivo:** Verificar que solo SuperAdmin puede ver la lista de instituciones.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a `/Institutions`
3. Revisar la lista

**Resultado Esperado:**
- ✅ Se muestra lista de todas las instituciones
- ✅ Se muestra tipo de institución
- ✅ Botones de acción disponibles

---

### PRUEBA 6.2: Acceso Restringido a Instituciones
**Objetivo:** Verificar que InstitutionAdmin NO puede acceder a gestión de instituciones.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Intentar acceder directamente a `/Institutions`

**Resultado Esperado:**
- ✅ Redirección a `/Account/AccessDenied`
- ✅ Mensaje de acceso denegado
- ✅ No se muestra información de instituciones

---

## 🔍 FASE 7: MULTI-TENANCY Y FILTROS

### PRUEBA 7.1: Filtro de Institución - SuperAdmin
**Objetivo:** Verificar que SuperAdmin puede ver y gestionar datos de todas las instituciones.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Navegar a diferentes secciones:
   - `/EntityProfiles`
   - `/Cards`
   - `/Events`
   - `/Users`
3. Verificar que se muestran datos de todas las instituciones

**Resultado Esperado:**
- ✅ En todas las secciones se muestran datos de todas las instituciones
- ✅ Se puede identificar a qué institución pertenece cada registro
- ✅ Dropdowns de institución disponibles en formularios de creación

---

### PRUEBA 7.2: Filtro de Institución - InstitutionAdmin
**Objetivo:** Verificar que InstitutionAdmin solo ve datos de su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a diferentes secciones:
   - `/EntityProfiles`
   - `/Cards`
   - `/Events`
   - `/Users`
3. Verificar que solo se muestran datos de su institución

**Resultado Esperado:**
- ✅ En todas las secciones solo se muestran datos de su institución
- ✅ No se ven datos de otras instituciones
- ✅ No hay dropdowns de institución en formularios (asignación automática)

---

### PRUEBA 7.3: Intentar Acceder a Datos de Otra Institución
**Objetivo:** Verificar que InstitutionAdmin no puede acceder a datos de otras instituciones.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Obtener un ID de entidad/tarjeta/evento de otra institución (desde SuperAdmin)
3. Intentar acceder directamente a:
   - `/EntityProfiles/Details/{id-de-otra-institucion}`
   - `/Cards/Details/{id-de-otra-institucion}`
   - `/Events/Details/{id-de-otra-institucion}`

**Resultado Esperado:**
- ✅ Redirección a error 404 o AccessDenied
- ✅ No se muestra información de otras instituciones
- ✅ Mensaje de error apropiado

---

## 📊 FASE 8: FUNCIONALIDADES ESPECÍFICAS POR ROL

### PRUEBA 8.1: Dashboard/Home - SuperAdmin
**Objetivo:** Verificar que el dashboard muestra información agregada de todas las instituciones.

**Pasos:**
1. Iniciar sesión como SuperAdmin
2. Acceder a `/` o `/Home`
3. Revisar estadísticas y resúmenes

**Resultado Esperado:**
- ✅ Dashboard muestra estadísticas globales
- ✅ Información de todas las instituciones
- ✅ Gráficos/resúmenes agregados

---

### PRUEBA 8.2: Dashboard/Home - InstitutionAdmin
**Objetivo:** Verificar que el dashboard muestra información solo de su institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Acceder a `/` o `/Home`
3. Revisar estadísticas y resúmenes

**Resultado Esperado:**
- ✅ Dashboard muestra estadísticas de su institución
- ✅ No se muestra información de otras instituciones
- ✅ Gráficos/resúmenes específicos de su institución

---

### PRUEBA 8.3: Estadísticas (Statistics)
**Objetivo:** Verificar que las estadísticas respetan el filtro de institución.

**Pasos:**
1. Iniciar sesión como InstitutionAdmin
2. Navegar a `/Statistics` (si existe)
3. Revisar estadísticas mostradas

**Resultado Esperado:**
- ✅ Solo se muestran estadísticas de su institución
- ✅ No se ven datos de otras instituciones

---

## 🖨️ FASE 9: IMPRESIÓN Y QR CODES

### PRUEBA 9.1: Generación de QR Code
**Objetivo:** Verificar que los QR codes se generan correctamente.

**Pasos:**
1. Crear una tarjeta nueva
2. Ver detalles de la tarjeta
3. Verificar que se muestra el QR code

**Resultado Esperado:**
- ✅ QR code generado y visible
- ✅ QR code es una imagen Base64 válida
- ✅ QR code es escaneable

---

### PRUEBA 9.2: Vista de Impresión - Con Foto
**Objetivo:** Verificar que la vista de impresión muestra la foto cuando existe.

**Pasos:**
1. Crear una entidad con foto
2. Crear una tarjeta para esa entidad
3. Acceder a vista de impresión
4. Revisar vista frontal y trasera

**Resultado Esperado:**
- ✅ Vista frontal muestra la foto de la entidad
- ✅ Vista trasera muestra el QR code
- ✅ Layout correcto para impresión
- ✅ Dimensiones CR80 (85.6mm x 54mm)

---

### PRUEBA 9.3: Vista de Impresión - Sin Foto
**Objetivo:** Verificar que la vista de impresión muestra placeholder cuando no hay foto.

**Pasos:**
1. Crear una entidad sin foto
2. Crear una tarjeta para esa entidad
3. Acceder a vista de impresión
4. Revisar vista frontal

**Resultado Esperado:**
- ✅ Vista frontal muestra placeholder agradable
- ✅ Placeholder es visualmente atractivo
- ✅ Layout mantiene proporciones correctas

---

### PRUEBA 9.4: Escaneo de QR Code
**Objetivo:** Verificar que el QR code redirige correctamente.

**Pasos:**
1. Escanear el QR code de una tarjeta con dispositivo móvil
2. Verificar la URL a la que redirige

**Resultado Esperado:**
- ✅ QR code es escaneable
- ✅ Redirige a la URL correcta
- ✅ Muestra información de la tarjeta/entidad

---

## 🔒 FASE 10: AUDITORÍA Y SEGURIDAD

### PRUEBA 10.1: Registro de Auditoría - Crear
**Objetivo:** Verificar que las acciones se registran en auditoría.

**Pasos:**
1. Realizar acciones (crear, editar, eliminar) en diferentes módulos
2. Verificar registros de auditoría (si hay interfaz)

**Resultado Esperado:**
- ✅ Cada acción crea un registro de auditoría
- ✅ Registro incluye: usuario, acción, fecha, entidad afectada
- ✅ Registros son inmutables

---

### PRUEBA 10.2: Validación de Campos Requeridos
**Objetivo:** Verificar que los campos requeridos se validan correctamente.

**Pasos:**
1. Intentar crear/editar registros sin completar campos requeridos
2. Verificar mensajes de validación

**Resultado Esperado:**
- ✅ Mensajes de validación claros
- ✅ Formularios no se envían con datos inválidos
- ✅ Campos requeridos marcados visualmente

---

### PRUEBA 10.3: Validación de Archivos (Fotos)
**Objetivo:** Verificar que las fotos se validan correctamente.

**Pasos:**
1. Intentar subir archivos no válidos:
   - Archivo que no es imagen
   - Imagen muy grande
   - Formato no soportado
2. Verificar mensajes de error

**Resultado Esperado:**
- ✅ Validación de tipo de archivo (solo imágenes)
- ✅ Validación de tamaño máximo
- ✅ Validación de formato (JPG, PNG, etc.)
- ✅ Mensajes de error claros

---

## 📱 FASE 11: INTERFAZ Y USABILIDAD

### PRUEBA 11.1: Navegación del Menú
**Objetivo:** Verificar que el menú muestra opciones según el rol.

**Pasos:**
1. Iniciar sesión como diferentes roles
2. Revisar el menú de navegación

**Resultado Esperado:**
- ✅ SuperAdmin ve todas las opciones
- ✅ InstitutionAdmin ve opciones apropiadas
- ✅ Staff ve opciones limitadas
- ✅ Menú es responsive

---

### PRUEBA 11.2: Mensajes de Confirmación
**Objetivo:** Verificar que los mensajes de confirmación funcionan correctamente.

**Pasos:**
1. Realizar acciones que requieren confirmación (eliminar, toggle)
2. Verificar diálogos de confirmación

**Resultado Esperado:**
- ✅ SweetAlert o diálogos nativos funcionan
- ✅ Mensajes son claros y descriptivos
- ✅ Confirmación y cancelación funcionan correctamente

---

### PRUEBA 11.3: Paginación y Búsqueda
**Objetivo:** Verificar que las listas grandes se manejan correctamente.

**Pasos:**
1. Crear múltiples registros (entidades, tarjetas, eventos)
2. Navegar por las listas
3. Usar búsqueda/filtros si están disponibles

**Resultado Esperado:**
- ✅ Listas se cargan correctamente
- ✅ Paginación funciona (si está implementada)
- ✅ Búsqueda/filtros funcionan (si están implementados)
- ✅ Performance aceptable con muchos registros

---

## ✅ CHECKLIST DE VALIDACIÓN FINAL

### Por Rol:

#### SuperAdmin
- [ ] Puede ver todas las instituciones
- [ ] Puede crear usuarios en cualquier institución
- [ ] Puede crear entidades en cualquier institución
- [ ] Puede crear tarjetas para cualquier entidad
- [ ] Puede crear eventos para cualquier entidad
- [ ] Puede gestionar instituciones
- [ ] Puede gestionar tipos de institución

#### InstitutionAdmin
- [ ] Solo ve su institución
- [ ] Puede crear usuarios en su institución
- [ ] Puede crear entidades en su institución
- [ ] Puede crear tarjetas para entidades de su institución
- [ ] Puede crear eventos para entidades de su institución
- [ ] NO puede acceder a gestión de instituciones
- [ ] NO puede ver datos de otras instituciones

#### Staff
- [ ] Acceso limitado según permisos
- [ ] Puede ver entidades/tarjetas/eventos de su institución
- [ ] Permisos de edición según configuración

#### AdministrativeOperator
- [ ] Acceso según permisos configurados
- [ ] Funcionalidades operativas disponibles

---

## 📝 NOTAS DE PRUEBA

### Credenciales de Prueba:
- **SuperAdmin:** `admin@qlservices.com` / `Admin@123456`
- **InstitutionAdmin:** `admin@demo.com` / `Admin@123456`

### URLs Base:
- **Producción:** `http://164.68.99.83`

### Consideraciones:
- Limpiar cookies del navegador entre cambios de rol
- Usar modo incógnito para pruebas limpias
- Verificar logs del servidor si hay errores
- Documentar cualquier comportamiento inesperado

---

## 🎯 RESULTADO ESPERADO GENERAL

Al completar todas las pruebas, se debe verificar que:

1. ✅ Todos los roles funcionan correctamente
2. ✅ Multi-tenancy está implementado correctamente
3. ✅ CRUD operations funcionan según permisos
4. ✅ Validaciones funcionan correctamente
5. ✅ Auditoría registra todas las acciones
6. ✅ Interfaz es intuitiva y responsive
7. ✅ Impresión de tarjetas funciona correctamente
8. ✅ QR codes se generan y escanean correctamente
9. ✅ Seguridad y autorización funcionan correctamente
10. ✅ Performance es aceptable

---

**Fecha de Creación:** 17 de Enero, 2026  
**Versión:** 1.0  
**Estado:** Listo para Ejecución
